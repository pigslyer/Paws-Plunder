

using Godot;

public class Globals : Node 
{
    private static Globals _instance;

    private Player _player;
    public static Player GetPlayer()
    {
        if (IsInstanceValid(_instance._player))
        {
            return _instance._player;
        }

        _instance._player = (Player)_instance.GetTree().GetNodesInGroup("PLAYER")[0];

        return _instance._player;
    } 

    private SquadController _squad;
    public static SquadController GetSquadController()
    {
        if (IsInstanceValid(_instance._squad))
        {
            return _instance._squad;
        }

        _instance._squad = (SquadController)_instance.GetTree().GetNodesInGroup("SQUADCONTROLLER")[0];

        return _instance._squad;
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        _instance = this;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (_instance == this)
        {
            _instance = null;
        }
    }
}
