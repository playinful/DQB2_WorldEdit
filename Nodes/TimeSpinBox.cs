using Godot;
using System;

namespace EyeOfRubiss.Nodes
{
	public partial class TimeSpinBox : SpinBox
	{
		private LineEdit _DummyLineEdit;

		public override void _Ready()
		{
	        _DummyLineEdit = new LineEdit
	        {
	            Size = GetLineEdit().Size,
	            Scale = GetLineEdit().Scale,
	            Position = GetLineEdit().Position
	        };
	        AddChild(_DummyLineEdit);

			GetLineEdit().Hide();

			ValueChanged += _On_ValueChanged;
			_DummyLineEdit.TextSubmitted += _On_LineEdit_TextSubmitted;
			_DummyLineEdit.TextChangeRejected += _On_LineEdit_TextChangeRejected;
			_DummyLineEdit.EditingToggled += _On_LineEdit_EditingToggled;

			_DummyLineEdit.Text = _GetFormattedText(Value);
		}

		private static bool _TryParseValue(string text, out double value)
		{
			value = 0;

			string[] split = text.Split(':');

			if (split.Length <= 0)
			{
				return false;
			}
			else if (split.Length == 1)
			{
				if (double.TryParse(split[0], out value))
				{
					return true;
				}
				else return false;
			}
			else if (split.Length == 2)
			{
				if (double.TryParse(split[0], out double hour) && double.TryParse(split[1], out double minute))
				{
					value = hour * 60 + minute;

					return true;
				}
				else return false;
			}
			else
			{
				return false;
			}
		}
		private static string _GetFormattedText(double value)
		{
			int hour = (int)(value / 60);
			double minute = value % 60;

			if (minute < 10)
				return $"{hour}:0{minute}";
			else
				return $"{hour}:{minute}";
		}

		public void UpdateLineEdit()
		{
			_DummyLineEdit.Text = _GetFormattedText(Value);
		}

		public void _On_ValueChanged(double _)
		{
			UpdateLineEdit();
		}
		public void _On_LineEdit_TextSubmitted(string newText)
		{
			if (_TryParseValue(newText, out double value))
				Value = value;
		}
		public void _On_LineEdit_TextChangeRejected(string _)
		{
			_DummyLineEdit.Text = _GetFormattedText(Value);
		}
		public void _On_LineEdit_EditingToggled(bool toggledOn)
		{
			if (!toggledOn)
				_DummyLineEdit.Text = _GetFormattedText(Value);
		}
	}
}
