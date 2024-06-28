using Godot;
using System;

public class TwoShips : Spatial
{
	private Label _FPSLabel;
	public override void _Ready()
	{
		_FPSLabel = GetNode<Label>("CanvasLayer/DebugContainer/FPSLabel");
	}

	public override void _Process(float delta)
	{
		float fps = Engine.GetFramesPerSecond();
		int ms = (int)(1 / fps * 1000);
		_FPSLabel.Text = $"{fps} FPS ({ms} ms)";
	}
}
