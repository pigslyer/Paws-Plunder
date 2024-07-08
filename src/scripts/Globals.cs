using System;
using System.Collections.Generic;
using Godot;

namespace PawsPlunder;

public partial class Globals : Node 
{
    private static Globals _instance = null!;

    private RandomNumberGenerator _rng = new();
    public static RandomNumberGenerator Rng => _instance._rng;
    
    public static float MouseSensitivity = 1.0F;

    public static readonly IReadOnlyList<string> CatNames = [
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
    ];

    private readonly RandomOrderQueue<string> _randomOrderedCatNames;
    private string _protagonistName;
    public static string ProtagonistName => _instance._protagonistName;
    

    private Player? _player;
    /// <summary>
    /// Should be avoided like the devil!
    /// </summary>
    /// <returns>The player, if they exist.</returns>
    public static Player GetPlayer()
    {
        // IsInstanceValid should be properly annotated with nullability hints, whatever tho
        if (_instance._player != null && IsInstanceValid(_instance._player))
        {
            return _instance._player;
        }

        _instance._player = (Player)_instance.GetTree().GetNodesInGroup("PLAYER")[0];

        return _instance._player;
    }

    public static void RandomizeProtag()
    {
        _instance._protagonistName = _instance._randomOrderedCatNames.NextElement() ?? "NULL";
    }

    public Globals()
    {
        _randomOrderedCatNames = new RandomOrderQueue<string>(CatNames, _rng);
        
        _rng.Randomize();

        _protagonistName = _randomOrderedCatNames.NextElement() ?? "NULL";
    }

    public override void _EnterTree()
    {
        _instance = this;
    }

    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null!;
        }
    }

    // TODO: move this math malarky to its own utility class?    
    public static void CalculateShotgunDirections(Vector3 baseDirection, float spread, float speed, Span<Vector3> shotsStorage)
    {
        float baseAddend = -spread / 2;
        float perElementFactor = spread / (shotsStorage.Length - 1);

        for (int i = 0; i < shotsStorage.Length; i++)
        {
            float currentAngleOffset = baseAddend + i * perElementFactor;
            Vector3 currentVelocity = baseDirection.Rotated(Vector3.Up, currentAngleOffset) * speed;

            shotsStorage[i] = currentVelocity;
        }
    }

    public static Vector3 GetProjectileVelocity(Vector3 targetPosition, Vector3 targetVelocity, Vector3 startingPoint, float projectileSpeed, float delay = 0f)
    {
        float distanceToTarget = (targetPosition - startingPoint).Length();
        float timeToHit = distanceToTarget / projectileSpeed + delay;

        Vector3 finalPosition = targetPosition + targetVelocity * timeToHit;

        Vector3 direction = startingPoint.DirectionTo(finalPosition); 

        return direction * projectileSpeed;
    }

}
