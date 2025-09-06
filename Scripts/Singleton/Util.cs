using System;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;

namespace EyeOfRubiss
{
    /// <summary> Static helper class which holds generic methods that are used in multiple places. </summary>  
	public static class Util
	{
		/// <summary> Creates a new AtlasTexture from res://Graphics/Items.png bearing the icon at the specified index. </summary>
		/// <param name="idx"> The index of the requested icon. </param>
		/// <returns> A new AtlasTexture bearing the icon at the specified index. </returns>
	    public static AtlasTexture GetItemIcon(int idx)
		{
			Texture2D atlas = ResourceLoader.Load<Texture2D>("res://Graphics/Items.png");

			if (idx < 0)
			{
				return new AtlasTexture
				{
					Atlas = atlas,
					Region = new Rect2(0, 112, 0, 112)
				};
			}

			int iconX = idx % (int)Math.Floor((double)(atlas.GetWidth() / 112));
			int iconY = idx / (int)Math.Floor((double)(atlas.GetWidth() / 112));

			return new AtlasTexture
			{
				Atlas = atlas,
				Region = new Rect2(112 * iconX, 112 * iconY, 112, 112)
			};
		}
		
        /// <summary>  
		/// Formats strings (usually item or block names) to decorative versions more similar to the base game.
		/// </summary>  
		/// <param name="input">The string to be formatted.</param>  
		/// <returns>A string with proper decoration to be applied to a RichTextLabel.</returns> 
		public static string ToRichText(string input)
		{
			string output = input;
			output = output.Replace("{White}", $"[color={Constants.Colors.WHITE}]■[/color]");
			output = output.Replace("{Black}", $"[color={Constants.Colors.BLACK}]■[/color]");
			output = output.Replace("{Purple}", $"[color={Constants.Colors.PURPLE}]■[/color]");
			output = output.Replace("{Pink}", $"[color={Constants.Colors.PINK}]■[/color]");
			output = output.Replace("{Red}", $"[color={Constants.Colors.RED}]■[/color]");
			output = output.Replace("{Green}", $"[color={Constants.Colors.GREEN}]■[/color]");
			output = output.Replace("{Yellow}", $"[color={Constants.Colors.YELLOW}]■[/color]");
			output = output.Replace("{Blue}", $"[color={Constants.Colors.BLUE}]■[/color]");
			return output;
		}
	}
}