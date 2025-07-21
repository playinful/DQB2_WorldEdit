using EyeOfRubiss.Scenes;
using Godot;
using System;

namespace EyeOfRubiss.Nodes
{
    public partial class CommandParser : LineEdit
    {
        [Export] private WorldEditorScene _WorldEditorScene;
        [Export] private StatusLabel _StatusLabel;

        public override void _Input(InputEvent @event)
        {
			if (@event is InputEventKey inputEventKey && inputEventKey.Keycode == Key.Slash)
			{
				//lineEdit.Text = "/";
				Show();
				GrabFocus();
				//lineEdit.CaretColumn = 1;
			}
        }

        public void _On_TextSubmitted(string new_text)
        {
            if (_WorldEditorScene is null)
                return;

            Text = "";
            if (!new_text.StartsWith('/'))
            {
                _StatusLabel?.PrintMessage(new_text);
                return;
            }

            string[] command_parts = new_text[1..].Split(' ');
            try
            {
                switch (command_parts[0])
                {
                    case "countblocks":
                        _WorldEditorScene.CountBlocks(command_parts[1]);
                        break;
                    case "countprops":
                        _WorldEditorScene.CountProps(command_parts[1]);
                        break;
                    case "findprop":
                        _WorldEditorScene.FindProp((ushort)command_parts[1].ToInt());
                        break;
                    case "tp":
                        _WorldEditorScene.MoveCamera(new Vector3(command_parts[1].ToFloat(), command_parts[2].ToFloat(), command_parts[3].ToFloat()) - new Vector3(1024, 0, 1024));
                        break;
                    case "fill":
                        _WorldEditorScene.FillCube(new Vector3I(command_parts[1].ToInt(), command_parts[2].ToInt(), command_parts[3].ToInt()),
                            new Vector3I(command_parts[4].ToInt(), command_parts[5].ToInt(), command_parts[6].ToInt()), (ushort)command_parts[7].ToInt());
                        break;
                    
                    case "moodtest":
                        _WorldEditorScene.TEST_Mood();
                        break;
                    case "moodtest2":
                        _WorldEditorScene.TEST_Mood2();
                        break;
                    case "moodtest3":
                        _WorldEditorScene.TEST_Mood3();
                        break;
                    case "moodtest4":
                        _WorldEditorScene.TEST_Mood4();
                        break;

                    case "setbrushblock":
                        _WorldEditorScene.SetBrushBlock((ushort)command_parts[1].ToInt());
                        break;
                    case "setbrushprop":
                        _WorldEditorScene.SetBrushProp((ushort)command_parts[1].ToInt());
                        break;
                    case "reload":
                        _WorldEditorScene.Reload();
                        break;
                    default:
                        _StatusLabel.PrintMessage($"Unknown command: {new_text}");
                        break;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr(ex);
            }

            Hide();
        }
    }
}
