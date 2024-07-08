using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PawsPlunder;

public interface IMoveable
{
    void GoTo(Vector3 point);
}

public interface IPlayerAttacker
{
    void AttackTarget(Player player);
}

public partial class SquadController : Node
{
    private readonly DetectionTracker<RatGrunt> _gruntTracker;
    private readonly DetectionTracker<Sniper> _sniperTracker;
    private readonly DetectionTracker<Gunner> _gunnerTracker;
    private readonly DetectionTracker<TraderMouse> _traderTracker;

    private readonly RandomOrderQueue<RatGrunt> _gruntMovementQueue;
    private readonly RandomOrderQueue<Gunner> _gunnerMovementQueue;
    private readonly RandomOrderQueue<TraderMouse> _traderMovementQueue;

    private readonly DelayedQueue<(IMoveable, Vector3)> _queuedMovers;
    private readonly DelayedQueue<IPlayerAttacker> _queuedAttackers;

    private readonly Distro _updatePositionsInterval = (2.0f, 1.0f);
    private readonly float _totalMovementTime = 1.0f;

    private readonly Distro _percentageOfMovingRatGrunts = (0.6f, 0.3f);
    private readonly Distro _distanceToPlayerRatGrunt = (8.0f, 3.0f);

    private readonly Distro _percentageOfMovingGunners = (-0.1f, 0.3f);
    private readonly Distro _distanceToPlayerGunner = (8.0f, 3.0f);

    private readonly Distro _percentageOfMovingTraders = (0.7f, 0.2f);
    private readonly Distro _distanceTraderMovement = (10.0f, 4.0f);

    private readonly Distro _attackInterval = (4.0f, 2.0f);
    private readonly float _totalAttackTime = 0.5f;

    private readonly Distro _percentageAttackingGrunts = (0.3333f, 0.1f);
    private readonly Distro _percentageAttackingSnipers = (0.5f, 0.2f);
    private readonly Distro _percentageAttackingGunners = (0.3f, 0.1f);

    private RandomNumberGenerator _rng;
    private Player? _player;

    private CustomTimer? _updatePositionsTimer = null;
    private CustomTimer? _runAttackRoutine = null;

    [Export] private RayCast3D _wallDetection = null!;

    public SquadController()
    {
        _rng = new RandomNumberGenerator();

        // TODO: make detection trackers properly track player
        _gruntTracker = new DetectionTracker<RatGrunt>(
            canSeeTarget: grunt => 
                _player != null && 
                CanPointBeSeen(grunt.CenterOfMass, _player.CenterOfMass),
            cantSeeTarget: grunt => 
                _player == null
        );

        _sniperTracker = new DetectionTracker<Sniper>(
            canSeeTarget: sniper => 
                _player != null && 
                CanPointBeSeen(sniper.CenterOfMass, _player.CenterOfMass),
            cantSeeTarget: sniper => 
                _player == null || !CanPointBeSeen(sniper.CenterOfMass, _player.CenterOfMass) 
        );

        _gunnerTracker = new DetectionTracker<Gunner>(
            canSeeTarget: gunner =>
                _player != null &&
                CanPointBeSeen(gunner.CenterOfMass, _player.CenterOfMass),
            cantSeeTarget: gunner => 
                _player == null
        );

        _traderTracker = new DetectionTracker<TraderMouse>(
            canSeeTarget: trader =>
                _player != null &&
                CanPointBeSeen(trader.CenterOfMass, _player.CenterOfMass),
            cantSeeTarget: trader => 
                _player == null
        );

        _gruntMovementQueue = new RandomOrderQueue<RatGrunt>(_rng);
        _gunnerMovementQueue = new RandomOrderQueue<Gunner>(_rng);
        _traderMovementQueue = new RandomOrderQueue<TraderMouse>(_rng);

        // possible use after free avoided by clearing both of these when anyone dies
        // this causes a break in the fighting, which might be a cool effect
        _queuedMovers = new DelayedQueue<(IMoveable, Vector3)>();
        _queuedAttackers = new DelayedQueue<IPlayerAttacker>();
    }

    public override void _Ready()
    {
        _rng.Randomize();

        _wallDetection = new RayCast3D()
        {
            Enabled = false,
            CollisionMask = (int)PhysicsLayers3D.World,
        };
        AddChild(_wallDetection);

        foreach (Node child in GetChildren())
        {
            if (child is RatGrunt grunt)
            {
                _gruntTracker.AddEnemy(grunt);
                
                grunt.Died += () => OnRatGruntDied(grunt);
            }
            else if (child is Sniper sniper)
            {
                _sniperTracker.AddEnemy(sniper);

                sniper.Died += () => OnSniperDied(sniper);
            }
            else if (child is Gunner gunner)
            {
                _gunnerTracker.AddEnemy(gunner);

                gunner.Died += () => OnGunnerDied(gunner);
            }
            else if (child is TraderMouse trader)
            {
                _traderTracker.AddEnemy(trader);

                trader.Died += () => OnTraderDied(trader);
            }
        }
    }

    private void OnRatGruntDied(RatGrunt grunt)
    {
        _gruntTracker.RemoveEnemy(grunt);
        _gruntMovementQueue.RemoveElement(grunt);
        
        _queuedAttackers.Clear();
        _queuedMovers.Clear();

        GlobalSignals.AddScore(100);
    }

    private void OnSniperDied(Sniper sniper)
    {
        _sniperTracker.RemoveEnemy(sniper);

        _queuedAttackers.Clear();

        GlobalSignals.AddScore(500);
    }

    private void OnGunnerDied(Gunner gunner)
    {
        _gunnerTracker.RemoveEnemy(gunner);
        _gunnerMovementQueue.RemoveElement(gunner);

        _queuedAttackers.Clear();
        _queuedMovers.Clear();

        GlobalSignals.AddScore(500);
    }

    private void OnTraderDied(TraderMouse trader)
    {
        _traderTracker.RemoveEnemy(trader);
        _traderMovementQueue.RemoveElement(trader);

        _queuedMovers.Clear();
    }

    public override void _PhysicsProcess(double delta)
    {
        float fDelta = (float)delta;

        if (_player != null && _player.Health <= 0)
        {
            _player = null;
        }

        UpdateInCombatEnemies();
        UpdateEnemyPositions();

        UpdateEnemyAttacking();
        
        RunQueuedMovers(fDelta);
        RunQueuedAttackers(fDelta);
    }

    private void UpdateInCombatEnemies()
    {
        _gruntTracker.UpdateVisibility();
        _sniperTracker.UpdateVisibility();
        _gunnerTracker.UpdateVisibility();
        _traderTracker.UpdateVisibility();

        // TODO: rework just entered/left combat to be more automated? 
        foreach (RatGrunt justEntered in _gruntTracker.JustEnteredCombat)
        {
            _gruntMovementQueue.AddElement(justEntered);
        }

        foreach (RatGrunt justLeft in _gruntTracker.JustLeftCombat)
        {
            _gruntMovementQueue.RemoveElement(justLeft);
        }

        foreach (Gunner justEntered in _gunnerTracker.JustEnteredCombat)
        {
            _gunnerMovementQueue.AddElement(justEntered);
        }
        
        foreach (Gunner justLeft in _gunnerTracker.JustLeftCombat)
        {
            _gunnerMovementQueue.RemoveElement(justLeft);
        }

        foreach (TraderMouse justEntered in _traderTracker.JustEnteredCombat)
        {
            _traderMovementQueue.AddElement(justEntered);
        }

        foreach (TraderMouse justLeft in _traderTracker.JustLeftCombat)
        {
            _traderMovementQueue.RemoveElement(justLeft);
        }
    }

    private void UpdateEnemyPositions()
    {
        if (GetActiveMoversCount() == 0)
        {
            _updatePositionsTimer?.Stop();
            _updatePositionsTimer = null;
            return;
        }

        if (_updatePositionsTimer == null)
        {
            _updatePositionsTimer = CustomTimer.Start(this, _rng.Randfn(_updatePositionsInterval));
            _updatePositionsTimer.Timeout += OnUpdatePositionsTimeout;
        }
    }

    private void OnUpdatePositionsTimeout()
    {
        _updatePositionsTimer = null;

        if (_player == null)
        {
            return;
        }

        Vector3 playerPosition = _player.GlobalPosition;

        // maybe make them prioritize moving towards the player if they're close, maybe make them prioritize moving into the player's view
        // can't determine without testing
        int movedGrunts = GetNormClamped(_percentageOfMovingRatGrunts, _gruntTracker.ActiveEnemies.Length);
        for (int i = 0; i < movedGrunts; i++)
        {
            RatGrunt grunt = _gruntMovementQueue.NextElement() ?? throw new System.Diagnostics.UnreachableException();

            float angle = _rng.RandfRange(Range.Circle);
            float distance = _rng.Randfn(_distanceToPlayerRatGrunt);

            Vector3 newPosition = playerPosition + new Vector3(distance, 0, 0).Rotated(Vector3.Up, angle);
            _queuedMovers.AddElement((grunt, newPosition));
        }

        int movedGunners = GetNormClamped(_percentageOfMovingGunners, _gunnerTracker.ActiveEnemies.Length);
        for (int i = 0; i < movedGunners; i++)
        {
            Gunner gunner = _gunnerMovementQueue.NextElement() ?? throw new System.Diagnostics.UnreachableException();

            float angle = _rng.RandfRange(Range.Circle);
            float distance = _rng.Randfn(_distanceToPlayerGunner);

            Vector3 newPosition = playerPosition + new Vector3(distance, 0, 0).Rotated(Vector3.Up, angle);
            _queuedMovers.AddElement((gunner, newPosition));
        }

        int movedTraders = GetNormClamped(_percentageOfMovingTraders, _traderTracker.ActiveEnemies.Length);
        for (int i = 0; i < movedTraders; i++)
        {
            TraderMouse trader = _traderMovementQueue.NextElement() ?? throw new System.Diagnostics.UnreachableException();

            Vector3 currentPosition = trader.GlobalPosition;
            
            float angle = _rng.RandfRange(Range.Circle);
            float distance = _rng.Randfn(_distanceTraderMovement);

            Vector3 newPosition = currentPosition + new Vector3(distance, 0, 0).Rotated(Vector3.Up, angle);
            _queuedMovers.AddElement((trader, newPosition));
        }

        _queuedMovers.RedistributeOver(_rng, _totalMovementTime);
    }

    private void UpdateEnemyAttacking()
    {
        if (GetActiveAttackersCount() == 0)
        {
            _runAttackRoutine?.Stop();
            _runAttackRoutine = null;
            return;
        }            

        if (_runAttackRoutine == null)
        {
            _runAttackRoutine = CustomTimer.Start(this, _rng.Randfn(_attackInterval));
            _runAttackRoutine.Timeout += OnAttackRoutineTimeout;
        }
    }

    private void OnAttackRoutineTimeout()
    {
        _runAttackRoutine = null;

        if (_player == null)
        {
            return;
        }

        // TODO: avoid array allocations
        int gruntAttackers = GetNormClamped(_percentageAttackingGrunts, _gruntTracker.ActiveEnemies.Length);
        Span<RatGrunt> attackingGruntsStorage = new RatGrunt[gruntAttackers];
        gruntAttackers = _rng.RandEls(_gruntTracker.ActiveEnemies, attackingGruntsStorage, grunt => CanPointBeSeen(grunt.CenterOfMass, _player.CenterOfMass));

        foreach (RatGrunt grunt in attackingGruntsStorage[..gruntAttackers])
        {
            _queuedAttackers.AddElement(grunt);
        }

        int sniperAttackers = GetNormClamped(_percentageAttackingSnipers, _sniperTracker.ActiveEnemies.Length);
        Span<Sniper> attackingSniperStorage = new Sniper[sniperAttackers];
        sniperAttackers = _rng.RandEls(_sniperTracker.ActiveEnemies, attackingSniperStorage, sniper => CanPointBeSeen(sniper.CenterOfMass, _player.CenterOfMass));

        foreach (Sniper sniper in attackingSniperStorage[..sniperAttackers])
        {
            _queuedAttackers.AddElement(sniper);
        }

        int gunnerAttackers = GetNormClamped(_percentageAttackingGunners, _gunnerTracker.ActiveEnemies.Length);
        Span<Gunner> attackingGunnerStorage = new Gunner[gunnerAttackers];
        gunnerAttackers = _rng.RandEls(_gunnerTracker.ActiveEnemies, attackingGunnerStorage, gunner => CanPointBeSeen(gunner.CenterOfMass, _player.CenterOfMass)); 

        foreach (Gunner gunner in attackingGunnerStorage[..gunnerAttackers]) 
        {
            _queuedAttackers.AddElement(gunner);
        }

        _queuedAttackers.RedistributeOver(_rng, _totalAttackTime);
    }

    private void RunQueuedMovers(float delta)
    {
        foreach ((IMoveable movable, Vector3 targetPosition) in _queuedMovers.PopElements(delta))
        {
            movable.GoTo(targetPosition);
        }        
    }

    private void RunQueuedAttackers(float delta)
    {
        if (_player == null)
        {
            return;
        }

        foreach (IPlayerAttacker attacker in _queuedAttackers.PopElements(delta))
        {
            attacker.AttackTarget(_player);
        }
    }


    private int GetActiveMoversCount()
    {
        return 
            _gruntTracker.ActiveEnemies.Length + 
            _gunnerTracker.ActiveEnemies.Length + 
            _traderTracker.ActiveEnemies.Length;
    }
    

    private int GetActiveAttackersCount()
    {
        return 
            _gruntTracker.ActiveEnemies.Length + 
            _gunnerTracker.ActiveEnemies.Length + 
            _sniperTracker.ActiveEnemies.Length;
    }

    private int GetNormClamped(Distro distro, int max)
    {
        float percentage = _rng.Randfn(distro);
        return int.Clamp(Mathf.CeilToInt(percentage * max), 0, max);
    }

    private bool CanPointBeSeen(Vector3 from, Vector3 to)
    {
        _wallDetection.GlobalPosition = from;
        _wallDetection.TargetPosition = _wallDetection.ToLocal(to);
        _wallDetection.ForceRaycastUpdate();

        return !_wallDetection.IsColliding();
    }

    private void PlayerEnteredArea(Node3D node3d)
    {
        if (node3d is Player player)
        {
            _player = player;
        }
    }

    private void PlayerExitedArea(Node3D node3d)
    {
        if (_player == node3d)
        {
            _player = null;

            _updatePositionsTimer?.Stop();
            _updatePositionsTimer = null;
            _runAttackRoutine?.Stop();
            _runAttackRoutine = null;
        }
    }

    private class DetectionTracker<T>
    {
        private readonly Predicate<T> _canSeeTarget;
        private readonly Predicate<T> _cantSeeTarget;

        private readonly List<T> _inactiveEnemies = [];
        private readonly List<T> _activeEnemies = [];
        private readonly List<T> _justLeftCombat = [];
        private readonly List<T> _justEnteredCombat = [];

        // these lists are guarnateed to never be recreated and will only shift their contents with UpdateVisibility and Add/RemoveEnemy
        public ReadOnlySpan<T> InactiveEnemies => _inactiveEnemies.AsSpan();
        public ReadOnlySpan<T> ActiveEnemies => _activeEnemies.AsSpan();

        public ReadOnlySpan<T> JustLeftCombat => _justLeftCombat.AsSpan();
        public ReadOnlySpan<T> JustEnteredCombat => _justEnteredCombat.AsSpan();

        public DetectionTracker(Predicate<T> canSeeTarget, Predicate<T> cantSeeTarget)
        {
            (_canSeeTarget, _cantSeeTarget) = (canSeeTarget, cantSeeTarget);
        }

        public void UpdateVisibility()
        {
            _justEnteredCombat.Clear();
            _justLeftCombat.Clear();

            // stackalloc/rent array here
            List<(bool isEntering, T enemy)> enteringCombat = [];

            foreach (T enemy in _inactiveEnemies)
            {
                if (_canSeeTarget.Invoke(enemy))
                {
                    enteringCombat.Add((true, enemy));
                }
            }

            foreach (T enemy in _activeEnemies)
            {
                if (_cantSeeTarget.Invoke(enemy))
                {
                    enteringCombat.Add((false, enemy));
                }
            }

            foreach ((bool isEntering, T enemy) in enteringCombat)
            {
                if (isEntering)
                {
                    _inactiveEnemies.Remove(enemy);
                    _activeEnemies.Add(enemy);

                    _justEnteredCombat.Add(enemy);
                }
                else
                {
                    _inactiveEnemies.Add(enemy);
                    _activeEnemies.Remove(enemy);

                    _justLeftCombat.Add(enemy);
                }
            }
        }

        public void AddEnemy(T enemy)
        {
            _inactiveEnemies.Add(enemy);
        }

        public void RemoveEnemy(T enemy)
        {
            if (!_inactiveEnemies.Remove(enemy))
            {
                _activeEnemies.Remove(enemy);
            }
        }
    }
}
