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
        private const int HeaderLength = 0x40;

        private const int ImageAddress = 0x69E90;
        private const int ImageSize = 0x64000;

        public static ScreenshotData Instance { get; private set; }
        public static bool HasInstance() => Instance is not null && Instance.IsLoaded;

        public static bool IsScreenshotDataFile(string path)
        {
            if (!Godot.FileAccess.FileExists(path))
                return false;

            using Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            {
                if (file.GetLength() < 6)
                    return false;

                byte[] header = file.GetBuffer(6);
                return
                    header[0] == 0x61 &&
                    header[1] == 0x65 &&
                    header[2] == 0x72 &&
                    header[3] == 0x43 &&
                    header[4] == 0x10 &&
                    header[5] == 0x00;
            }
        }

        public static ScreenshotData TryLoadAndSet(string path)
        {
            if (TryLoad(path) is ScreenshotData screenshotData)
            {
                return Instance = screenshotData;
            }
            else return null;
        }
        public static ScreenshotData TryLoad(string path)
        {
            ScreenshotData screenshotData = new();
            if (screenshotData._TryLoad(path, HeaderLength))
                return screenshotData;
            else return null;
        }
        public static void SetInstance(ScreenshotData screenshotData)
        {
            Instance = screenshotData;
        }

        public static void Close()
        {
            Instance.IsLoaded = false;
            Instance.UnsavedChanges = false;
            Instance = null;
        }

        public Image GetImage(int index)
        {
            Image image = new();
            image.LoadJpgFromBuffer(GetBytes(ImageAddress + ImageSize * index, ImageSize).ToArray());
            return image;
        }
        public void SetImage(int index, string filename)
        {
            Image image = new();
            image.Load(filename);

            byte[] imageData = image.GetData();

            if (!(imageData[0] == 0xFF && imageData[1] == 0xD8))
                return;
            if (imageData.Length > ImageSize)
                return;

            Fill(0, ImageAddress + ImageSize * index, ImageSize);

            SetBytes(ImageAddress + ImageSize * index, imageData);
        }
    }
}