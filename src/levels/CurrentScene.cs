using Godot;

namespace PawsPlunder;

public partial class CurrentScene : Node3D
{
	[Export] private Level _level = null!;
	[Export] private Control _mainMenu = null!;
	[Signal]
	public delegate void GameStartEventHandler();
	public override void _Ready()
	{
		_mainMenu.Connect(
			"StartGame",
			Callable.From(OnGameStart),
			(uint)ConnectFlags.OneShot
		);
		Connect(
			nameof(GameStart),
			Callable.From(_level.OnStart),
			(uint)ConnectFlags.OneShot
		);
	}
	private void OnGameStart()
	{
		_mainMenu.QueueFree();
		EmitSignal(nameof(GameStart));
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("ui_quit"))
		{
//			GetTree().Quit();
		}
	}
}
