

using System;

namespace EyeOfRubiss
{
    public class BlueprintFileDQB2 : SaveData
    {
        public const int HEADER_LENGTH = 0;

        public static bool TryLoad(string path, out BlueprintFileDQB2 result)
        {
            result = null;
            BlueprintFileDQB2 blueprintFile = new();
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

        public static BlueprintFileDQB2 CreateNew()
        {
            BlueprintFileDQB2 blueprintFile = new()
            {
                _Header = [],
                _Buffer = new byte[Blueprint.LENGTH]
            };

           return blueprintFile;
        }

        public Blueprint Blueprint => new(this, 0);
    }
}