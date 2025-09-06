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
        /// <summary> Sets a FileDialog's current directory to the deepest existing part of the specified path. </summary>
        /// <param name="dialog"> The FileDialog whose directory to set. </param>
        /// <param name="path"> The deepest desired path to set. </param>
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
        /// <summary> Clears all of a FileDialog's current filters and adds a new filter. </summary>
        /// <param name="dialog"> The FileDialog whose filter to set. </param>
        /// <param name="filter"> The filter to set. </param>
        /// <param name="description"> Description of the filter. An empty string by default. </param>
        public static void SetFilter(this FileDialog dialog, string filter, string description = "")
        {
            dialog.ClearFilters();
            dialog.AddFilter(filter, description);
        }

        /// <summary> Toggles a CanvasItem's visibility. </summary>
        /// <param name="node"> The CanvasItem whose visibility to toggle. </param>
        /// <returns> The updated visibility of the CanvasItem. </returns>
        public static bool ToggleVisible(this CanvasItem node)
        {
            return node.Visible = !node.Visible;
        }

        /// <summary> Calls QueueFree on all of this Node's children. </summary>
        /// <param name="node"> The Node whose children upon which to call QueueFree. </param>
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
        public static void DisconnectAll(this GodotObject @object, StringName signal)
        {
            foreach (Godot.Collections.Dictionary dic in @object.GetSignalConnectionList(signal))
			{
				@object.Disconnect(signal, (Callable)dic["callable"]);
			}
        }
    }
}
