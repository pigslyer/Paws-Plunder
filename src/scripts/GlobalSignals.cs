using Godot;

namespace PawsPlunder;

// TODO: maybe rename this?
public partial class GlobalSignals : Node
{
	[Signal] public delegate void AddToPlayerScoreEventHandler(int score);
	
	private static GlobalSignals _instance = null!;
	
	public override void _EnterTree()
	{
		_instance = this;
	}

	public override void _ExitTree()
	{
		if (_instance == this) _instance = null!;
	}

	public static GlobalSignals GetInstance()
	{
		return _instance;
	}

	public static void AddScore(int score)
	{
		Logger.Debug($"Adding {score} to player score.");
		GetInstance().EmitSignal(SignalName.AddToPlayerScore, score);
	}
}
