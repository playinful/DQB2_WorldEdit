using EyeOfRubiss.Info;
using EyeOfRubiss.Scenes;
using Godot;
using System;

namespace EyeOfRubiss.Nodes
{
	public partial class CameraController : Camera3D
	{
		[Export] float Speed = 1;
		[Export] float MaxSpeedMultiplier = 2;
		[Export] float MinSpeedMultiplier = 0.25f;
		[Export] float SpeedMultiplierStep = 0.25f;
		[Export] float MouseSensitivity = 0.4f;
		[Export] float MaxAngle = 90;
		[Export] float MinAngle = -90;
		[Export] float SizeStep = 1.0f;

		private float SpeedMultiplier = 1;

		[Export] public bool Enabled { get; set; }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
		{
			if (!Enabled)
				return;

			if (GetTree().Root.GuiGetFocusOwner() is LineEdit)
				return;

			Vector3 positionChangeVector = Vector3.Zero;

			if (Input.IsActionPressed(Constants.Controls.CAMERA_LEFT))
				positionChangeVector += Vector3.Left;
			if (Input.IsActionPressed(Constants.Controls.CAMERA_RIGHT))
				positionChangeVector += Vector3.Right;
			if (Input.IsActionPressed(Constants.Controls.CAMERA_FORWARD))
				positionChangeVector += Vector3.Forward;
			if (Input.IsActionPressed(Constants.Controls.CAMERA_BACK))
				positionChangeVector += Vector3.Back;
			if (Input.IsActionPressed(Constants.Controls.CAMERA_UP))
				positionChangeVector += Vector3.Up;
			if (Input.IsActionPressed(Constants.Controls.CAMERA_DOWN))
				positionChangeVector += Vector3.Down;

			positionChangeVector = positionChangeVector.Normalized().Rotated(Vector3.Up, Rotation.Y);
			Position += positionChangeVector * (float)delta * Speed * SpeedMultiplier;

			if (Input.IsActionPressed(Constants.Controls.CAMERA_FOV_UP))
            {
				if (Size + SizeStep > 100)
                {
                    Size = 100.0f;
                }
				else
                {
                    Size += SizeStep;
                }
				if (Size * 1.8f > 179)
                {
                    Fov = 179.0f;
                }
				else
                {
                    Fov = Size * 1.8f;
                }
            }
			if (Input.IsActionPressed(Constants.Controls.CAMERA_FOV_DOWN))
            {
				if (Size - SizeStep < 1)
                {
                    Size = 1.0f;
                }
				else
                {
                    Size -= SizeStep;
                }
				if (Size * 1.8f < 1)
                {
                    Fov = 1.0f;
                }
				else
                {
                    Fov = Size * 1.8f;
                }
            }
		}

        public override void _UnhandledInput(InputEvent @event)
        {
			if (!Enabled)
				return;

			if (@event.IsActionPressed(Constants.Controls.CAMERA_HOLD_TO_MOVE) || @event.IsActionPressed(Constants.Controls.CURSOR_CAPTURE))
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
			if (@event.IsActionReleased(Constants.Controls.CAMERA_HOLD_TO_MOVE) || @event.IsActionPressed(Constants.Controls.CURSOR_RELEASE))
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}

            if (@event is InputEventMouseMotion mouseMotion && (Input.IsActionPressed(Constants.Controls.CAMERA_HOLD_TO_MOVE) || Input.MouseMode == Input.MouseModeEnum.Captured))
			{
				var motion = mouseMotion.Relative;

				float x = Rotation.Y - Mathf.DegToRad(motion.X) * MouseSensitivity;
				float y = Rotation.X - Mathf.DegToRad(motion.Y) * MouseSensitivity;

				y = (float)Mathf.Clamp(y, Mathf.DegToRad(MinAngle + 0.001), Mathf.DegToRad(MaxAngle - 0.001));

				Rotation = new Vector3(y, x, Rotation.Z);
			}
			
			if (@event.IsActionPressed(Constants.Controls.CAMERA_SPEED_UP))
			{
				SpeedMultiplier += SpeedMultiplierStep;
				if (SpeedMultiplier > MaxSpeedMultiplier)
					SpeedMultiplier = MaxSpeedMultiplier;
			}
			if (@event.IsActionPressed(Constants.Controls.CAMERA_SPEED_DOWN))
			{
				SpeedMultiplier -= SpeedMultiplierStep;
				if (SpeedMultiplier < MinSpeedMultiplier)
					SpeedMultiplier = MinSpeedMultiplier;
			}

			if (@event.IsActionPressed(Constants.Controls.CAMERA_ISOMETRIC))
            {
                Projection = (Projection == ProjectionType.Perspective) ? ProjectionType.Orthogonal : ProjectionType.Perspective;
            }
        }

		public void Enable()
		{
			Enabled = true;
		}
		public void Disable()
		{
			Enabled = false;
		}
    }
}
