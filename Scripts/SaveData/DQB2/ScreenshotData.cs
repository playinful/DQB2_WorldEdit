using System;
using System.Linq;
using System.IO.Enumeration;
using System.Collections.Generic;
using System.Text;
using Godot;
using System.Runtime.CompilerServices;

namespace EyeOfRubiss
{
    public class ScreenshotData : SaveData
    {
        public const int HEADER_LENGTH = 0x40;

        private const int IMAGE_ADDRESS = 0x69E90;
        private const int IMAGE_SIZE = 0x64000;

        public static bool TryLoad(string path, out ScreenshotData result)
        {
            result = null;
            ScreenshotData screenshotData = new();
            if (screenshotData._TryLoad(path, HEADER_LENGTH))
            {
                result = screenshotData;
                return true;
            }
            else return false;
        }

        public Image GetImage(int index)
        {
            Image image = new();
            image.LoadJpgFromBuffer(GetBytes(IMAGE_ADDRESS + IMAGE_SIZE * index, IMAGE_SIZE).ToArray());
            return image;
        }
        public void SetImage(int index, string filename)
        {
            Image image = new();
            image.Load(filename);

            byte[] imageData = image.GetData();

            if (!(imageData[0] == 0xFF && imageData[1] == 0xD8))
                return;
            if (imageData.Length > IMAGE_SIZE)
                return;

            Fill(0, IMAGE_ADDRESS + IMAGE_SIZE * index, IMAGE_SIZE);

            SetBytes(IMAGE_ADDRESS + IMAGE_SIZE * index, imageData);
        }
    }
}