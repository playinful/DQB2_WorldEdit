using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EyeOfRubiss.Integration.FSWatcher;

/// <summary>
/// Immutable.
/// </summary>
sealed class World : IWorld
{
	private readonly IReadOnlyList<IReadOnlyList<Chunk>> chunkGrid;
	public int ChunkCount { get; }

	public static World Empty() => new World(new List<List<Chunk>>())
	{
		InitialCameraPosition = new Vector3I(0, 96, 0),
	};

	private World(IReadOnlyList<IReadOnlyList<Chunk>> chunkGrid)
	{
		this.chunkGrid = chunkGrid;
		ChunkCount = chunkGrid.SelectMany(x => x).Where((Chunk c) => c != null).Count();
	}

	public required Vector3I? InitialCameraPosition { get; init; }

	private Chunk GetChunkOrNull(ChunkLocation loc)
	{
		if (loc.X32 < 0 || loc.X32 >= chunkGrid.Count)
		{
			return null;
		}
		var column = chunkGrid[loc.X32];
		if (loc.Z32 < 0 || loc.Z32 >= column.Count)
		{
			return null;
		}
		return column[loc.Z32];
	}

	public Block GetBlockAtPosition(Vector3I position)
	{
		var loc = ChunkLocation.FromPosition(position);
		var chunk = GetChunkOrNull(loc);
		return chunk == null ? new Block(0) : chunk.GetBlock(position);
	}

	public bool HasData(Box box)
	{
		// Lucky that Zylann's boxes line up with DQB2 chunks so we only have to look at box.Start here:
		var loc = ChunkLocation.FromPosition(box.Start);
		return GetChunkOrNull(loc) != null;
	}

	abstract class Chunk
	{
		public DriverFileContent.FileChunkInfo FileChunkInfo { get; init; }
		public DateTime LastWriteTimeUtc { get; init; }

		protected abstract ReadOnlySpan<ushort> Blockdata { get; }

		/// <summary>
		/// Operates modulo 32 (assumes the caller determined this is the correct chunk given the global position)
		/// </summary>
		public Block GetBlock(Vector3I position)
		{
			int y = position.Y % 96;
			int z = position.Z % 32;
			int x = position.X % 32;
			int index = y * (32 * 32) + z * 32 + x;
			return new Block(Blockdata[index]);
		}
	}

	sealed class Chunk_LittleEndian : Chunk
	{
		private readonly byte[] bytes;
		public Chunk_LittleEndian(byte[] bytes)
		{
			this.bytes = bytes;
		}

		protected override ReadOnlySpan<ushort> Blockdata => MemoryMarshal.Cast<byte, ushort>(bytes);
	}

	sealed class Chunk_BigEndian : Chunk
	{
		private readonly ushort[] blockdata;
		public Chunk_BigEndian(ushort[] blockdata)
		{
			this.blockdata = blockdata;
		}

		protected override ReadOnlySpan<ushort> Blockdata => blockdata;
	}

	/// <summary>
	/// Reuses data that has not changed when possible
	/// </summary>
	public World Reload(DriverFileContent content, FileInfo driverFile)
	{
		List<Chunk> newChunks = new();
		string directory = driverFile.Directory.FullName;
		int reusedChunkCount = 0;
		int freshChunkCount = 0;

		foreach (var chunkInfo in content.ChunkInfos)
		{
			if (chunkInfo.OffsetX < 0 || chunkInfo.OffsetZ < 0)
			{
				throw new BadIntegrationException($"Cannot have chunk offsets < 0, but got {chunkInfo}");
			}
			if (chunkInfo.OffsetX % 32 != 0 || chunkInfo.OffsetZ % 32 != 0)
			{
				throw new BadIntegrationException($"Chunk offsets must be a multiple of 32, but got {chunkInfo}");
			}

			if (CanReuseExistingChunk(chunkInfo, directory, out var existingChunk))
			{
				reusedChunkCount++;
				newChunks.Add(existingChunk);
			}
			else
			{
				freshChunkCount++;
				newChunks.Add(LoadChunk(chunkInfo, directory));
			}
		}

		if (freshChunkCount == 0 && reusedChunkCount == this.ChunkCount)
		{
			return this;
		}

		GD.Print($"Reused chunks: {reusedChunkCount}, Fresh chunks: {freshChunkCount}");

		List<List<Chunk>> newGrid = new();
		foreach (var chunk in newChunks)
		{
			var loc = chunk.FileChunkInfo.ChunkLocation;
			while (newGrid.Count <= loc.X32)
			{
				newGrid.Add(new List<Chunk>());
			}
			var column = newGrid[loc.X32];
			while (column.Count <= loc.Z32)
			{
				column.Add(null);
			}
			column[loc.Z32] = chunk;
		}

		Vector3I? initialCameraPosition = null;
		if (content.SetCameraX.HasValue && content.SetCameraZ.HasValue)
		{
			initialCameraPosition = new Vector3I(content.SetCameraX.Value, 96, content.SetCameraZ.Value);
		}

		return new World(newGrid) { InitialCameraPosition = initialCameraPosition };
	}

	private bool CanReuseExistingChunk(DriverFileContent.FileChunkInfo chunkInfo, string directory, out Chunk existingChunk)
	{
		existingChunk = GetChunkOrNull(chunkInfo.ChunkLocation);
		if (existingChunk == null || chunkInfo != existingChunk.FileChunkInfo)
		{
			return false;
		}

		string fullPath = Path.Combine(directory, chunkInfo.RelativePath);
		var lastWriteTimeUtc = File.GetLastWriteTimeUtc(fullPath);
		return lastWriteTimeUtc == existingChunk.LastWriteTimeUtc;
	}

	private static Chunk LoadChunk(DriverFileContent.FileChunkInfo chunkInfo, string directory)
	{
		string fullPath = Path.Combine(directory, chunkInfo.RelativePath);
		var lastWriteTimeUtc = File.GetLastWriteTimeUtc(fullPath);
		var bytes = File.ReadAllBytes(fullPath);

		const int expectedLength = 2 * 96 * 32 * 32;
		if (bytes.Length != expectedLength)
		{
			throw new BadIntegrationException($"Chunk file must be exactly {expectedLength} bytes, but got {bytes.Length} from {fullPath}");
		}

		if (BitConverter.IsLittleEndian)
		{
			return new Chunk_LittleEndian(bytes)
			{
				FileChunkInfo = chunkInfo,
				LastWriteTimeUtc = lastWriteTimeUtc,
			};
		}
		else
		{
			var blockdata = new ushort[expectedLength / 2];
			for (int i = 0; i < blockdata.Length; i++)
			{
				byte lo = bytes[i * 2];
				byte hi = bytes[i * 2 + 1];
				blockdata[i] = (ushort)(lo | (hi << 8));
			}
			return new Chunk_BigEndian(blockdata)
			{
				FileChunkInfo = chunkInfo,
				LastWriteTimeUtc = lastWriteTimeUtc,
			};
		}
	}
}
