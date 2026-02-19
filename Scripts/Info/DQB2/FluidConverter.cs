using System.Text.Json;
using Godot;

namespace EyeOfRubiss.Info.DQB2
{
    public static class FluidConverter
    {
		private const string DATABASE_PATH = "res://Info/DQB2/Fluid.json";
        private static ushort[][][] _Database;
        
        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
            {
                _Database = JsonSerializer.Deserialize<ushort[][][]>(FileAccess.GetFileAsString(DATABASE_PATH));;
            }
        }

        public static ushort Convert(FluidType fluidType, FluidLevel fluidLevel, PartsType propShell)
        {
            LoadDatabase();
            if (fluidType == FluidType.Air)
            {
                ushort result = _Database[(int)propShell + 1][(int)FluidType.MAXIMUM][0];
                //GD.Print($"In: {fluidType} ({(int)fluidType}) - {fluidLevel} ({(int)fluidLevel}) | Converting to: {propShell} ({(int)propShell}) | Out: {result} [{BlockInfo.Get(result).Name}]");
                return result;
            }
            else
            {
                ushort result = _Database[(int)propShell + 1][(int)fluidType][(int)fluidLevel];
                //GD.Print($"In: {fluidType} ({(int)fluidType}) - {fluidLevel} ({(int)fluidLevel}) | Converting to: {propShell} ({(int)propShell}) | Out: {result} [{BlockInfo.Get(result).Name}]");
                return result;
            }
        }
    }
}