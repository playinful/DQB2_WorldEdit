using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Godot;

namespace EyeOfRubiss
{
    /// <summary> Static class holding extension methods. </summary>
    public static class Extensions
    {
        public static ushort GetBlockID(this ushort value)
        {
            return (ushort)(value & 0b0000_0111_1111_1111);
        }
        public static ushort SetBlockID(this ushort value, ushort blockId)
        {
           blockId = blockId.GetBlockID();
           value = (ushort)(value & 0b1111_1000_0000_0000);
           return (ushort)(value | blockId);
        }
        public static ChiselShape GetChiselShape(this ushort value)
        {
            return (ChiselShape)(value >> 12);
        }
        public static ushort SetChiselShape(this ushort value, ChiselShape shape)
        {
            ushort shapeValue = (ushort)((ushort)shape << 12);
            value = (ushort)(value & 0b0000_1111_1111_1111);
            return (ushort)(value | shapeValue);
        }
        public static bool GetPlayerPlaced(this ushort value)
        {
            return (value & 0b0000_1000_0000_0000) != 0;
        }
        public static ushort SetPlayerPlaced(this ushort value, bool playerPlaced)
        {
            ushort playerPlacedValue = (ushort)((playerPlaced ? 1 : 0) << 11);
            value = (ushort)(value & 0b1111_0111_1111_1111);
            return (ushort)(value | playerPlacedValue);
        }

        public static void SetCurrentDirRecursive(this FileDialog dialog, string path)
        {
            string[] directories = path.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
            while (directories.Length > 0 && !Directory.Exists(Path.Join(directories)))
            {
                directories = directories[..(directories.Length - 1)];
            }

            if (directories.Length > 0)
                dialog.CurrentDir = Path.Join(directories);
        }
        public static void SetFilter(this FileDialog dialog, string filter, string description = "")
        {
            dialog.ClearFilters();
            dialog.AddFilter(filter, description);
        }
        public static void SetFilter(this FileDialog dialog, string[] filter, string[] description = null)
        {
            dialog.ClearFilters();
            for (int i = 0; i < filter.Length; i++)
            {
                string desc = "";
                if (description is not null && description.Length > i)
                    desc = description[i];
                dialog.AddFilter(filter[i], desc);
            }
        }

        public static bool ToggleVisible(this CanvasItem node)
        {
            return node.Visible = !node.Visible;
        }

        public static void QueueFreeAllChildren(this Node node)
        {
            foreach (Node child in node.GetChildren())
            {
                child.QueueFree();
            }
        }
        public static void Unparent(this Node node)
        {
            if (node.GetParent() is Node parent)
            {
                parent.RemoveChild(node);
            }
        }
        
        public static byte GetFacingDirection(this Node3D node, bool global = false)
        {
            float rot = global ? node.GlobalRotation.Y : node.Rotation.Y;

            int rotation = (int)Math.Round(rot / (Math.PI / 2)) % 4;
			if (rotation < 0)
				rotation += 4;
			return (byte)rotation;
        }

        // This code doesn't work for binds with extra arguments, But I don't think I need to use any of those so it's probably fine
        public static void DisconnectAll(this GodotObject @object, StringName signal)
        {
            foreach (Godot.Collections.Dictionary dic in @object.GetSignalConnectionList(signal))
			{
                if (dic["callable"].As<Callable>() is Callable callable)
                {
                    if (@object.IsConnected(signal, callable))
                        @object.Disconnect(signal, callable);
                }
			}
        }
    }
}
