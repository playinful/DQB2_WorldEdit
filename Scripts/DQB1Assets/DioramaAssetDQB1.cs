using EyeOfRubiss.Scenes;
using Godot;
using System;
using EyeOfRubiss.Info;
using System.Collections.Generic;

namespace EyeOfRubiss
{
    public class DioramaAssetDQB1
    {
        public DioramaHeaderAssetDQB1 Header;
        public DioramaDataAssetDQB1 Data;

        public byte[] Blocks;

        public void CreateBlockList()
        {
            if (Data is null)
                return;
            
            List<byte> blocks = [];
            for (int i = 0; i + 1 < Data.Blocks.Length; i += 2)
            {
                if (Data.Blocks[i] == 0 && Data.Blocks[i + 1] == 0)
                {
                    blocks = [];
                    continue;
                }
                for (int j = 1; j <= Data.Blocks[i]; j++)
                {
                    blocks.Add((byte)Data.Blocks[i + 1]);
                }
            }

            Blocks = [.. blocks];
        }

        public byte GetBlock(Vector3I position)
        {
            if (Header is null || Data is null || Blocks is null)
            {
                return 0;
            }

            if (position.X < 0 || position.X >= Header.SizeX || position.Y < 0 || position.Y >= Header.SizeY || position.Z < 0 || position.Z >= Header.SizeZ)
                return 0;

            int index = position.Z * Header.SizeX * Header.SizeY + position.X * Header.SizeY + position.Y;
            if (index >= Blocks.Length)
                return 0;
            else return Blocks[index];
        }
    }
}