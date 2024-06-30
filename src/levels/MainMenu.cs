using Godot;
using System;

public class MainMenu : CanvasLayer
{
	[Signal]
	public delegate void StartGame();
	private void _on_PlayButton_pressed()
	{
		EmitSignal("StartGame");
	}
	private void _on_ExitButton_pressed()
	{
		GetTree().Quit();
	}
}





