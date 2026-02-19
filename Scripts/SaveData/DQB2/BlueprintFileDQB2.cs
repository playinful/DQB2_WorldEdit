

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

        public Blueprint Blueprint => new Blueprint(this, 0);
    }
}