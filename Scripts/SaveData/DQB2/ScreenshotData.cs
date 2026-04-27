using System;
using System.Linq;
using System.IO.Enumeration;
using System.Collections.Generic;
using System.Text;
using Godot;
using System.Runtime.CompilerServices;
using System.IO;

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

            Span<byte> bytes = GetBytes(IMAGE_ADDRESS + IMAGE_SIZE * index, IMAGE_SIZE);
            if (!(bytes[0] == 0xFF && bytes[1] == 0xD8))
                return null;

            image.LoadJpgFromBuffer(bytes.ToArray());
            return image;
        }
        public bool TrySetImage(int index, string filename)
        {
            byte[] imageData = Godot.FileAccess.GetFileAsBytes(filename);

            if (!(imageData[0] == 0xFF && imageData[1] == 0xD8))
            {
                GD.Print("JPG header does not match.");
                return false;
            }
            if (imageData.Length > IMAGE_SIZE)
            {
                GD.Print($"Image too large to fit. Expected size: < {IMAGE_SIZE}, provided image size: {imageData.Length}");
                return false;
            }

            Fill(0, IMAGE_ADDRESS + IMAGE_SIZE * index, IMAGE_SIZE);

            SetBytes(IMAGE_ADDRESS + IMAGE_SIZE * index, imageData);

            return true;
        }
    }
}