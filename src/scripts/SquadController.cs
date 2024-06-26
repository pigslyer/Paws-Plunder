using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Another one of those wonderful "controller" hacks because thisified systems require it.
/// </summary> 
public class SquadController : Node
{
    private readonly List<RatGrunt> _activeRatGrunts = new List<RatGrunt>();

    private RandomNumberGenerator _rng;

    private Timer _attackTimer;
    private readonly (float Mean, float Deviation) _attackTimerInterval = (5f, 0.5f);

    public override void _Ready()
    {
        base._Ready();

        _rng = new RandomNumberGenerator();
        _rng.Randomize();

        _attackTimer = new Timer()
        {
            OneShot = false,
        };
        AddChild(_attackTimer);
        _attackTimer.Connect("timeout", this, nameof(CombatRonud));

        _attackTimer.Start(_rng.Randfn(_attackTimerInterval.Mean, _attackTimerInterval.Deviation));
    }
    private void CombatRonud()
    {
        GD.Print("started combat round");

        //if (_activeCombatants.Count > 0)
        {
            ProcessCombatTurn();
        } 

        _attackTimer.Start(_rng.Randfn(_attackTimerInterval.Mean, _attackTimerInterval.Deviation));
    }

    private void ProcessCombatTurn()
    {
    }
}
