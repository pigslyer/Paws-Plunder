using Godot;

namespace PawsPlunder;

public partial class CurrentScene : Node3D
{
	[Export] private Level _level = null!;
	[Export] private MainMenu _mainMenu = null!;
	[Export] private Pause _pause = null!;
	[Signal]
	public delegate void GameStartEventHandler();
	public override void _Ready()
	{
		_mainMenu.StartGame += OnGameStart;
		GameStart += _level.OnStart;
	}
	public override void _ExitTree()
	{
		_mainMenu.StartGame -= OnGameStart;
		GameStart -= _level.OnStart;
	}
	private void OnGameStart()
	{
		EmitSignal(SignalName.GameStart);
		_mainMenu.Visible = false;
		//_mainMenu.QueueFree(); // TODO: check how much memory credits are consuming
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("ui_pause"))
		{
			if (_mainMenu.Visible) return;
			_pause.TogglePause(!_pause.Visible);
		}
	}
}


// TODO: add debug menu?
/*
_debugLabel.Text = $"Velocity: {newVelocity}\nSpeed: {newVelocity.Length()}\nSpeed xz: {newVelocity.X0Z().Length()}";
*/
