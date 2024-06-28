using System.Collections.Generic;
using Godot;

public class Globals : Node 
{
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    public static RandomNumberGenerator Rng => _instance._rng;

    public static IReadOnlyList<string> CatNames = new string[]
    {
        // generic
        "Blackfur",
        "Catbeard",
        "Fishbone",

        // asi who else
        "Yarrfield",

        // holmes
        "Kocl",

        // nix0nax
        "Rino",
        "Brie",
        "Sunny",

        // shiloh
        "Selina",

        // hermi
        "Feliks",
    };

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

    public override void _Ready()
    {
        _rng.Randomize();
    }

    public override void _EnterTree()
    {
        _instance = this;
    }

    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
