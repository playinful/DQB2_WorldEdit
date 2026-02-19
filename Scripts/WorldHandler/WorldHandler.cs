using Godot;
using EyeOfRubiss.Scenes;
using System;
using System.ComponentModel;

namespace EyeOfRubiss
{
    public class WorldHandler(WorldEditorScene worldEditorScene)
    {
        public WorldEditorScene _WorldEditorScene = worldEditorScene;

        public virtual void DoPointer() {}
        public virtual void DoSetBlock(Vector3I position, int block) {}
        public virtual void DoSetBGParts(Vector3I position, int bgParts) {}
        public virtual void DoSetFluid(Vector3I position, int fluid) {}
        public virtual void DoEraser(Vector3I position) {}
        public virtual void DoFill() {}
        public virtual void DoEyedropper(Vector3I position) {}

        public virtual string GetDebugInfo(Vector3I position) => "";

        public virtual void Reload() {}

        public virtual void OnTerrainDisplayChanged(bool show) {}
        public virtual void OnPropShellsDisplayChanged(bool show) {}
        public virtual void OnFluidsDisplayChanged(bool show) {}
        public virtual void OnPropsDisplayChanged(bool show) {}
        public virtual void OnNPCDisplayChanged(bool show) {}
        public virtual void OnPlayerDisplayChanged(bool show) {}

        public virtual void OnGizmo3DTransformEnd() {}
    }
}