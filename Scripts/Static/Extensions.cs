using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;

namespace EyeOfRubiss
{
    /// <summary> Static class holding extension methods. </summary>
    public static class Extensions
    {
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
    
        public static int GetItemIndexById(this PopupMenu @this, int id)
        {
            for (int i = 0; i < @this.ItemCount; i++)
            {
                if (@this.GetItemId(i) == id)
                    return i;
            }

            return -1;
        }
    }
}
