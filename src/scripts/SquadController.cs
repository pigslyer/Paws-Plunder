using System;
using System.Collections.Generic;
using Godot;

public interface ISquadTarget
{
    bool CanSeeCombatant(ISquadCombatant combatant);
}

public interface ISquadCombatant
{
    void AttackTarget(ISquadTarget target);
}

/// <summary>
/// Another one of those wonderful "controller" hacks because thisified systems require it.
/// </summary> 
public class SquadController : Node
{    
    // implementing global target only since i'm not planning on including infighting
    private ISquadTarget _globalTarget = null;
    private List<ISquadCombatant> _activeCombatants = new List<ISquadCombatant>();

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

    public void SetGlobalSquadTarget(ISquadTarget target)
    {
        _globalTarget = target;
    }

    public void EnterCombat(ISquadCombatant combatant)
    {
        _activeCombatants.Add(combatant);
    }

    public void LeaveCombat(ISquadCombatant combatant)
    {
        _activeCombatants.Remove(combatant);
    }

    private void CombatRonud()
    {
        GD.Print("started combat round");

        if (_activeCombatants.Count > 0)
        {
            ProcessCombatTurn();
        } 

        _attackTimer.Start(_rng.Randfn(_attackTimerInterval.Mean, _attackTimerInterval.Deviation));
    }

    private void ProcessCombatTurn()
    {
        int index = (int)(_rng.Randi() % (uint)_activeCombatants.Count);

        ISquadCombatant combatant = _activeCombatants[index];

        combatant.AttackTarget(_globalTarget);
    }
}
