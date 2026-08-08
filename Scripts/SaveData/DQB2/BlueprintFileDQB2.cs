

using System;

namespace EyeOfRubiss
{
    public class PencilSketchFile : SaveData
    {
        public const int HEADER_LENGTH = 0;

        public static bool TryLoad(string path, out PencilSketchFile result)
        {
            result = null;
            PencilSketchFile blueprintFile = new();
            if (blueprintFile._TryLoad(path, HEADER_LENGTH, decompress: false))
            {
                result = blueprintFile;
                return true;
            }
            else return false;
        }
        public override void Save(string path = null)
        {
            path ??= Path;
            Path = path;

            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            file.StoreBuffer(_Buffer);

            UnsavedChanges = false;
        }

        public static PencilSketchFile CreateNew()
        {
            PencilSketchFile blueprintFile = new()
            {
                _Header = [],
                _Buffer = new byte[PencilSketch.LENGTH]
            };

           return blueprintFile;
        }

        public PencilSketch PencilSketch => new(this, 0);
    }
}