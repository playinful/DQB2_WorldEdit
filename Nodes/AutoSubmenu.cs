using Godot;
using System;

namespace EyeOfRubiss.Nodes
{
	/// <summary> A PopupMenu that automatically links to its parent PopupMenu or MenuButton as a Submenu at the specified index upon entering the SceneTree. </summary>
	public partial class AutoSubmenu : PopupMenu
	{
		/// <summary> The index of the parent node's item to set to this submenu. Does nothing if negative. </summary>
		[Export] public int SubmenuIndex { get; set; } = -1;

		public override void _Ready()
		{
			if (SubmenuIndex < 0)
				return;
			
			if (GetParent() is MenuButton parentMenuButton)
			{
				Reparent(parentMenuButton.GetPopup());
			}
			if (GetParent() is PopupMenu parentPopupMenu)
			{
				parentPopupMenu.SetItemSubmenuNode(SubmenuIndex, this);
			}
		}
	}
}
