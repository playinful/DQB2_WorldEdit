using Godot;
using EyeOfRubiss.Scenes;
using System;
using System.ComponentModel;
using EyeOfRubiss.Nodes;
using System.Collections.Generic;

namespace EyeOfRubiss
{
    public class WorldHandler(WorldEditorScene worldEditorScene)
    {
        public WorldEditorScene _WorldEditorScene = worldEditorScene;

        public virtual string GetDebugInfo(Vector3I position) => "";

        public virtual void DoPointer(Vector3I position) {}
        public virtual void DoSetBlock(Vector3I position, int block) {}
        public virtual void DoSetBGParts(Vector3I position, int bgParts, PartsType? partsBlock = null, bool collision = true, bool effects = true, bool unbreakable = false, byte size = 0) {}
        public virtual void DoSetFluid(Vector3I position, int fluidLevel, int fluidType) {}
        public virtual void DoEraser(Vector3I position) {}
        public virtual void DoFill() {}
        public virtual void DoChisel(Vector3I position, ChiselShape shape) {}
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

        public virtual void OnGizmo3DTransformEnd(NPCSprite npcSprite) {}

        public virtual void MakeSuperflat(List<Tuple<int, int>> layers) {}
        public virtual void RaiseLowerIsland(int amount, int fillerBlock) {}
        public virtual void DeleteAllBGParts() {}
        public virtual void FillInChunks() {}
        public virtual void FixPropShells() {}
        public virtual void FixFakeBlocks() {}
        public virtual void ClearOrphanedBlockEntities() {}
        public virtual void CreateWaterCeiling() {}
    }
}