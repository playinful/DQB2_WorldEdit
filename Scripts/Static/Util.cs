using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using EyeOfRubiss.Nodes;
using EyeOfRubiss.Scenes;
using Godot;

namespace EyeOfRubiss
{
	public static class Util
	{
        public static byte[] Decompress(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);

            output.Flush();
            return output.ToArray();
        }
        public static byte[] Compress(byte[] data, System.IO.Compression.CompressionLevel compressionLevel)
        {
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();
            using (var zlib = new System.IO.Compression.ZLibStream(output, compressionLevel))
            {
                input.CopyTo(zlib);
                zlib.Flush();
            }
            return output.ToArray();
        }
		
		public static FileType DetermineFileType(string path)
        {
            if (!Godot.FileAccess.FileExists(path))
				return FileType.Unknown;
			
			using Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            {
				if (path.ToLower().EndsWith(".unknown") || path.ToLower().EndsWith(".dat") || path.ToLower().EndsWith(".vap") || path.ToLower().EndsWith(".sdb") || path.ToLower().EndsWith(".dfont") || path.ToLower().EndsWith(".nfc") || path.ToLower().EndsWith(".ttf") || path.ToLower().EndsWith(".gt1") || path.ToLower().EndsWith(".unpack") || path.ToLower().EndsWith(".frm"))
				{
					file.Seek(0);
					byte[] header = file.GetBuffer(4);
					if (header[1] != 0 && header[2] != 0 && header[3] != 0 && ((ulong)header[1] * header[2] * header[3] * 6 + 4 + header[0] * (ulong)8 == file.GetLength()))
					{
						return FileType.DQB2_Blueprint;
					}
					else
					{
						// todo make more elegant and robust
						return FileType.DQB2_DioramaData;	
					}
				}

				
				// todo make more elegant and robust
                if (file.GetLength() == 0x30008)
					return FileType.DQB2_PencilSketch;
				
				file.Seek(0);
				if (file.GetLength() == 0x48010)
				{
					byte[] header = file.GetBuffer(8);

					if (header.SequenceEqual<byte>([0x00, 0x02, 0x01, 0x00, 0x00, 0x04, 0x80, 0x10]))
						return FileType.DQB1_ParamData_PS3;
					if (header.SequenceEqual<byte>([0x01, 0x02, 0x00, 0x01, 0x10, 0x80, 0x04, 0x00]))
						return FileType.DQB1_ParamData_PS4;
					if (header.SequenceEqual<byte>([0x02, 0x02, 0x00, 0x01, 0x10, 0x80, 0x04, 0x00]))
						return FileType.DQB1_ParamData_Vita;
				}
				if (file.GetLength() == 0x1A00000 || file.GetLength() == 0x194CCFC)
				{
					file.Seek(0x1200);
					byte[] test1 = file.GetBuffer(7);
					file.Seek(0x1208);
					byte[] test2 = file.GetBuffer(4);

					if (test1.SequenceEqual<byte>([0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x02]) &&
						test2.SequenceEqual<byte>([0x06, 0x00, 0x06, 0x00]))
						return FileType.DQB1_WorldData_PS3;

					file.Seek(0x1200);
					byte[] test3 = file.GetBuffer(4);
					file.Seek(0x1205);
					byte[] test4 = file.GetBuffer(7);

					if (test3.SequenceEqual<byte>([0x00, 0x09, 0x00, 0x00]) &&
						test4.SequenceEqual<byte>([0x02, 0x00, 0x00, 0x00, 0x06, 0x00, 0x06]))
						return FileType.DQB1_WorldData_PS4;
						
				}

				file.Seek(0);
				if (file.GetLength() >= StageData.HEADER_LENGTH)
                {
                    byte[] header = file.GetBuffer(6);

					if (header.SequenceEqual<byte>([0x61, 0x65, 0x72, 0x43, 0xDD, 0x00]))
						return FileType.DQB2_StageData;
                }
				file.Seek(0);
				if (file.GetLength() >= CommonData.HEADER_LENGTH)
                {
                    byte[] header = file.GetBuffer(6);

					if (header.SequenceEqual<byte>([0x61, 0x65, 0x72, 0x43, 0x02, 0x01]))
						return FileType.DQB2_CommonData;
                }
				file.Seek(0);
				if (file.GetLength() >= ScreenshotData.HEADER_LENGTH)
                {
                    byte[] header = file.GetBuffer(6);

					if (header.SequenceEqual<byte>([0x61, 0x65, 0x72, 0x43, 0x10, 0x00]))
						return FileType.DQB2_ScreenshotData;
                }

				file.Seek(0);
				if (file.GetLength() >= 2)
                {
                    byte[] header = file.GetBuffer(2);
					
					if (header[0] == 0x78 && (header[1] == 0x01 || header[1] == 0x5E || header[1] == 0x9C || header[1] == 0xDA))
                    {
						file.Seek(0);
						byte[] compressed = file.GetBuffer((long)file.GetLength());
						byte[] data = Decompress(compressed);

						if (data[0] == 0x04 && data[1] == 0x02 && data[2] == 0x00 && data[3] == 0x01)
						{
							return FileType.DQB1_ParamData_Switch;
						}
						else
						{
							return FileType.DQB1_WorldData_Switch;
						}
                    }
                }
				
				try
                {
                    JsonDocument json = JsonDocument.Parse(file.GetAsText());

					if (json.RootElement.TryGetProperty("m_Script", out JsonElement script) && script.TryGetProperty("m_PathID", out JsonElement m_pathId) && m_pathId.TryGetInt64(out long pathId))
                    {
						if (pathId == 9047693110664622774)
							return FileType.DQB1_BlueprintAsset;
						if (pathId == 3054667693991686249)
							return FileType.DQB1_DioramaHeaderAsset;
						if (pathId == 1781583903654961094)
							return FileType.DQB1_DioramaDataAsset;
                    }
					if (json.RootElement.TryGetProperty("SourceGame", out JsonElement sourceGameElement) && sourceGameElement.TryGetByte(out byte sourceGame) && (sourceGame == 1 || sourceGame == 2))
					{
						return FileType.EyeOfRubissStructure;
					}
                }
				catch {}
            }

			return FileType.Unknown;
        }
	
		public static AtlasTexture GetItemIcon(int idx)
		{
			Texture2D atlas = ResourceLoader.Load<Texture2D>("res://Resources/Graphics/Items.png");

			if (idx < 0)
			{
				return new AtlasTexture
				{
					Atlas = atlas,
					Region = new Rect2(0, 112, 0, 112)
				};
			}

			int iconX = idx % (int)Math.Floor((double)(atlas.GetWidth() / 112));
			int iconY = idx / (int)Math.Floor((double)(atlas.GetWidth() / 112));

			return new AtlasTexture
			{
				Atlas = atlas,
				Region = new Rect2(112 * iconX, 112 * iconY, 112, 112)
			};
		}
		
        public static string ToRichText(string input)
		{
			string output = input;
			output = output.Replace("{white}", $"[color={Constants.Colors.WHITE}]■[/color]");
			output = output.Replace("{black}", $"[color={Constants.Colors.BLACK}]■[/color]");
			output = output.Replace("{purple}", $"[color={Constants.Colors.PURPLE}]■[/color]");
			output = output.Replace("{pink}", $"[color={Constants.Colors.PINK}]■[/color]");
			output = output.Replace("{red}", $"[color={Constants.Colors.RED}]■[/color]");
			output = output.Replace("{green}", $"[color={Constants.Colors.GREEN}]■[/color]");
			output = output.Replace("{yellow}", $"[color={Constants.Colors.YELLOW}]■[/color]");
			output = output.Replace("{blue}", $"[color={Constants.Colors.BLUE}]■[/color]");
			output = output.Replace("{pname}", Main.GetPlayerName());
			return output;
		}
		public static string DirectionToString(int direction)
		{
			return direction switch
			{
				0 => "North",
				1 => "West",
				2 => "South",
				3 => "East",
				_ => "UNKNOWN"
			};
		}

		public static int GridMapRotationFromDirection(byte direction, byte secondaryRotation = 0)
        {
            return secondaryRotation switch
			{
				1 => direction switch
				{
					1 => 5,
					2 => 9,
					3 => 13,
					_ => 1
				},
				2 => direction switch
				{
					1 => 20,
					2 => 8,
					3 => 18,
					_ => 2
				},
				3 => direction switch
				{
					1 => 15,
					2 => 11,
					3 => 7,
					_ => 3
				},
				_ => direction switch
            	{
                	1 => 16,
                	2 => 10,
	                3 => 22,
	                _ => 0,
            	}
			};
        }
	}
}