using System.Collections.Generic;
using Godot;

public class Globals : Node 
{
    private static Globals _instance;

    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    public static RandomNumberGenerator Rng => _instance._rng;
    
    public static float MouseSensitivity = 1.0F;

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

        // matej
        "Kiki",

        // nudl
        "Mini",
    };

    private RandomOrderQueue<string> _randomOrderedCatNames;
    private string _protagonistName;
    public static string ProtagonistName => _instance._protagonistName;
    

    private Player _player;
    /// <summary>
    /// Should be avoided like the devil!
    /// </summary>
    /// <returns>The player, if they exist.</returns>
    public static Player GetPlayer()
    {
        if (IsInstanceValid(_instance._player))
        {
            return _instance._player;
        }

        _instance._player = (Player)_instance.GetTree().GetNodesInGroup("PLAYER")[0];

        return _instance._player;
    }

    public static void RandomizeProtag()
    {
        _instance._protagonistName = _instance._randomOrderedCatNames.NextElement();
    }

    public Globals()
    {
        _randomOrderedCatNames = new RandomOrderQueue<string>(CatNames, _rng);
        
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
    
    public static IEnumerable<Vector3> CalculateShotgunDirections(Vector3 baseDirection, float spread, int shotCount, float speed)
    {
        for (int i = 0; i < shotCount; i++)
        {
            float currentAngleOffset = -spread / 2 + i * (spread / (shotCount - 1));
            Vector3 currentVelocity = baseDirection.Rotated(Vector3.Up, currentAngleOffset) * speed;

            yield return currentVelocity;
        }
    }
}
