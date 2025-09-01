using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeOfRubiss.Integration.FSWatcher;

/// <summary>
/// Maps to JSON content of the main driver file
/// </summary>
sealed class DriverFileContent : DriverFileContentBase
{
	public IReadOnlyList<FileChunkInfo> ChunkInfos { get; init; }

	public sealed record FileChunkInfo
	{
		/// <summary>
		/// Path is relative to the driver file.
		/// The chunk file must contain exactly 2*96*32*32 bytes.
		/// Interpretation of the bytes is the same as a STGDAT file.
		/// </summary>
		public string RelativePath { get; init; }

		/// <summary>
		/// Must be a multiple of 32
		/// </summary>
		public int OffsetX { get; init; }

		/// <summary>
		/// Must be a multiple of 32
		/// </summary>
		public int OffsetZ { get; init; }

		public int ChunkId { get; init; }

		public ChunkLocation ChunkLocation => new ChunkLocation(OffsetX / 32, OffsetZ / 32);
	}
}
