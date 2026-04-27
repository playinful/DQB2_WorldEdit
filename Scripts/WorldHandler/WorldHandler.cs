using Godot;
using EyeOfRubiss.Scenes;
using System;
using System.ComponentModel;

namespace EyeOfRubiss
{
    public class WorldHandler(WorldEditorScene worldEditorScene)
    {
        public WorldEditorScene _WorldEditorScene = worldEditorScene;

        public virtual string GetDebugInfo(Vector3I position) => "";

        public virtual void DoPointer() {}
        public virtual void DoSetBlock(Vector3I position, int block) {}
        public virtual void DoSetBGParts(Vector3I position, int bgParts, PartsType? partsBlock = null, bool collision = true, bool effects = true) {}
        public virtual void DoSetFluid(Vector3I position, int fluidLevel, int fluidType) {}
        public virtual void DoEraser(Vector3I position) {}
        public virtual void DoFill() {}
        public virtual void DoPaste(Vector3I position, EyeOfRubissStructure clipboard, bool pasteAir) {}
        public virtual void DoEyedropper(Vector3I position) {}

        public virtual bool CanCopy() => false;
        public virtual EyeOfRubissStructure DoCopy(Vector3I start, Vector3I end) => null;

        public virtual void ReplaceBlock(int replace, int with, Vector3I? from = null, Vector3I? to = null) {}

        public virtual void Reload() {}

        public virtual void OnTerrainDisplayChanged(bool show) {}
        public virtual void OnPartsBlockDisplayChanged(bool show) {}
        public virtual void OnFluidsDisplayChanged(bool show) {}
        public virtual void OnBGPartsDisplayChanged(bool show) {}
        public virtual void OnNPCDisplayChanged(bool show) {}
        public virtual void OnPlayerDisplayChanged(bool show) {}

        public virtual void OnGizmo3DTransformEnd() {}
    }
}