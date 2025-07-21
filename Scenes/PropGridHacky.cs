using Godot;
using System;

public partial class PropGridHacky : GridMap
{
    [Export] private GridMap SubGrid;

    public void SetCellItemDelegated(Vector3I position, int item, int orientation = 0)
    {
        if (item <= 1269)
            SetCellItem(position, item, orientation);
        else
        {
            SubGrid.SetCellItem(position, item, orientation);
        }
    }

    public void ClearCellItem(Vector3I position)
    {
        SetCellItem(position, -1);
        SubGrid.SetCellItem(position, -1);
    }
    public void ClearSubGrid()
    {
        SubGrid.Clear();
    }
}
