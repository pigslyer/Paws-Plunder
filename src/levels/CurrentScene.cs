using Godot;

namespace PawsPlunder;

public partial class CurrentScene : Node3D
{
	[Export] private Level _level = null!;
	[Export] private MainMenu _mainMenu = null!;
	[Signal]
	public delegate void GameStartEventHandler();
	public override void _Ready()
	{
		_mainMenu.StartGame += OnGameStart;
		GameStart += _level.OnStart;
	}
	private void OnGameStart()
	{
		EmitSignal(SignalName.GameStart);
		_mainMenu.QueueFree();
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("ui_quit"))
		{
//			GetTree().Quit();
		}
	}


}
