using Godot;

public class GlobalSignals : Node
{
	[Signal]
	public delegate void AddToPlayerScore(int score);
	
	private static GlobalSignals _instance;
	
	public override void _EnterTree()
	{
		_instance = this;
	}

	public override void _ExitTree()
	{
		if (_instance == this) _instance = null;
	}

	public static GlobalSignals GetInstance()
	{
		return _instance;
	}

	public static void AddScore(int score)
	{
		GetInstance().EmitSignal("AddToPlayerScore", score);
	}
}
