using Godot;
using System;
using System.IO;

public partial class PropGridHacky : GridMap
{
    [Export] private GridMap SubGrid;

    [Export(PropertyHint.File, "*.tres,")] private string _MeshLibraryA;
    [Export(PropertyHint.File, "*.tres,")] private string _MeshLibraryB;

    public void SetupLibraries()
    {
        MeshLibrary = ResourceLoader.Load<MeshLibrary>(_MeshLibraryA);
        SubGrid.MeshLibrary = ResourceLoader.Load<MeshLibrary>(_MeshLibraryB);
    }

    public void SetCellItemDelegated(Vector3I position, int item, int orientation = 0)
    {
        if (MeshLibrary is null)
            SetupLibraries();

        if (item <= 1269)
            CallDeferred(GridMap.MethodName.SetCellItem, position, item, orientation);
        else
            SubGrid.CallDeferred(GridMap.MethodName.SetCellItem, position, item, orientation);
    }

    public void ClearCellItem(Vector3I position)
    {
        CallDeferred(GridMap.MethodName.SetCellItem, position, -1);
        SubGrid.CallDeferred(GridMap.MethodName.SetCellItem, position, -1);
    }
    public void ClearSubGrid()
    {
        SubGrid.CallDeferred(GridMap.MethodName.Clear);
    }
}
