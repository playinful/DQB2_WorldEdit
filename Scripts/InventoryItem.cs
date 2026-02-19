using Godot;
using System;
using EyeOfRubiss.Info;

namespace EyeOfRubiss
{
    /// <summary> Represents an Item held in an inventory within a SaveData instance. </summary>
    /// <param name="saveData"> The SaveData instance wherein this InventoryItem resides. </param>
    /// <param name="address"> The starting address of the InventoryItem instance's data within the SaveData's Buffer. </param>
    public class InventoryItem(SaveData saveData, int address)
    {
        /// <summary> The SaveData instance wherein this InventoryItem resides. </summary>
        private SaveData _SaveData = saveData;
        /// <summary> The starting address of the InventoryItem instance's data within the SaveData's Buffer. </summary>
        public int Address = address;

        /// <summary> The ushort ID of the item. </summary>
        public ushort ItemID { get => _SaveData.GetUInt16(Address); set => _SaveData.SetUInt16(Address, value); }
        /// <summary> The amount of items stacked in this inventory slot. </summary>
        public short Count { get => _SaveData.GetInt16(Address + 2); set => _SaveData.SetInt16(Address + 2, value); } // TODO check signed
    }
}