using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Another one of those wonderful "controller" hacks because thisified systems require it.
/// </summary> 
public class SquadController : Node
{
    private readonly DetectionTracker<RatGrunt> _gruntTracker;
    private readonly DetectionTracker<Sniper> _sniperTracker;
    private readonly DetectionTracker<Gunner> _gunnerTracker;
    private readonly DetectionTracker<TraderMouse> _traderTracker;

    private readonly RandomOrderQueue<RatGrunt> _gruntMovementQueue;
    private readonly RandomOrderQueue<Gunner> _gunnerMovementQueue;
    private readonly RandomOrderQueue<TraderMouse> _traderMovementQueue;

    private readonly Distro _updatePositionsInterval = (2.0f, 1.0f);
    private readonly Distro _distanceToPlayerRatGrunt = (8.0f, 3.0f);

    private readonly Distro _attackInterval = (4.0f, 2.0f);

    // chance that, during any given attack cycle, a sniper fires
    private const float ChanceOfSniperShot = 0.8f;
    private const float ChanceOfBlunderbussShot = 0.8f;

    private RandomNumberGenerator _rng;
    private Player _player;
    private RayCast _wallDetection;

    private CustomTimer _updatePositionsTimer = null;
    private CustomTimer _runAttackRoutine = null;

    public SquadController()
    {
        _rng = new RandomNumberGenerator();

        _gruntTracker = new DetectionTracker<RatGrunt>(
            canSeeTarget: grunt => 
                _player != null && 
                CanPointBeSeen(grunt.CenterOfMass, _player.GlobalTranslation)
        );
        
        _sniperTracker = new DetectionTracker<Sniper>(
            canSeeTarget: sniper => 
                _player != null && 
                CanPointBeSeen(sniper.CenterOfMass, _player.GlobalTranslation)
        );

        _gunnerTracker = new DetectionTracker<Gunner>(
            canSeeTarget: gunner =>
                _player != null &&
                CanPointBeSeen(gunner.CenterOfMass, _player.GlobalTranslation)
        );

        _traderTracker = new DetectionTracker<TraderMouse>(
            canSeeTarget: trader =>
                _player != null &&
                CanPointBeSeen(trader.CenterOfMass, _player.GlobalTranslation)
        );

        _gruntMovementQueue = new RandomOrderQueue<RatGrunt>(_rng);
        _gunnerMovementQueue = new RandomOrderQueue<Gunner>(_rng);
        _traderMovementQueue = new RandomOrderQueue<TraderMouse>(_rng);
    }

    public override void _Ready()
    {
        _rng.Randomize();

        _wallDetection = new RayCast()
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
    }

    private void OnSniperDied(Sniper sniper)
    {
        _sniperTracker.RemoveEnemy(sniper);
    }

    private void OnGunnerDied(Gunner gunner)
    {
        _gunnerTracker.RemoveEnemy(gunner);
        _gunnerMovementQueue.RemoveElement(gunner);
    }

    private void OnTraderDied(TraderMouse trader)
    {
        _traderTracker.RemoveEnemy(trader);
        _traderMovementQueue.RemoveElement(trader);
    }

    public override void _PhysicsProcess(float delta)
    {
        if (_player != null)
        {
            PerformCombatTasks();
        }
    }

    // all of these presuppose player exists
    private void PerformCombatTasks()
    {
        UpdateInCombatEnemies();

        UpdateEnemyPositions();

        UpdateEnemyAttacking();
    }

    private void UpdateInCombatEnemies()
    {
        _gruntTracker.UpdateVisibility();
        _sniperTracker.UpdateVisibility();
        _gunnerTracker.UpdateVisibility();
        _traderTracker.UpdateVisibility();

        // can't wait for when this has to be updated for more than just combat people
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
        if (_gruntTracker.ActiveEnemies.Count == 0)
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

        Vector3 playerPosition = _player.GlobalTranslation;

        // maybe make them prioritize moving towards the player if they're close, maybe make them prioritize moving into the player's view
        // can't determine without testing
        int movedGrunts = (int)Math.Ceiling(_gruntTracker.ActiveEnemies.Count / 3.0f) + _rng.RandiRange(-1, 1);
        movedGrunts = Math.Max(movedGrunts, _gruntTracker.ActiveEnemies.Count);

        for (int i = 0; i < movedGrunts; i++)
        {
            RatGrunt grunt = _gruntMovementQueue.NextElement();
            float angle = _rng.RandfRange(0, Mathf.Tau);
            float distance = _rng.Randfn(_distanceToPlayerRatGrunt);

            Vector3 newPosition = playerPosition + new Vector3(distance, 0, 0).Rotated(Vector3.Up, angle);
            grunt.GoTo(newPosition);
        }

        if (_gunnerTracker.ActiveEnemies.Count > 0)
        {
            Gunner gunner = _gunnerMovementQueue.NextElement();

            float angle = _rng.RandfRange(0, Mathf.Tau);
            float distance = _rng.Randfn(_distanceToPlayerRatGrunt);

            Vector3 newPosition = playerPosition + new Vector3(distance, 0, 0).Rotated(Vector3.Up, angle);
            gunner.GoTo(newPosition);
        }

        int movedTraders = _traderTracker.ActiveEnemies.Count * 2 / 3;
        for (int i = 0; i < movedTraders; i++)
        {
            TraderMouse trader = _traderMovementQueue.NextElement();

            Vector3 currentPosition = trader.GlobalTranslation;
            Distro traderMoveDistro = (5.0f, 2.0f);

            float angle = _rng.RandfRange(0, Mathf.Tau);
            float distance = _rng.Randfn(traderMoveDistro);

            Vector3 newPosition = currentPosition + new Vector3(distance, 0, 0).Rotated(Vector3.Up, angle);

            trader.GoTo(newPosition);
        }
    }

    private void UpdateEnemyAttacking()
    {
        if (_gruntTracker.ActiveEnemies.Count == 0)
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

        // safety
        if (_player == null)
        {
            return;
        }

        if (_gruntTracker.ActiveEnemies.Count > 0)
        {
            int gruntAttackers = (int)Math.Ceiling(_gruntTracker.ActiveEnemies.Count / 3.0);

            foreach (RatGrunt grunt in _rng.RandEls(_gruntTracker.ActiveEnemies, gruntAttackers))
            {
                grunt.AttackTarget(_player);
            }
        }

        float sniperFireShot = _rng.Randf();

        if (_sniperTracker.ActiveEnemies.Count > 0 && sniperFireShot < ChanceOfSniperShot)
        {
            Sniper sniper = _rng.RandEl(_sniperTracker.ActiveEnemies);  
            
            sniper.AttackTarget(_player);
        }

        float gunnerFireShot = _rng.Randf();

        if (_gunnerTracker.ActiveEnemies.Count > 0 && gunnerFireShot > -1)
        {
            Gunner gunner = _rng.RandEl(_gunnerTracker.ActiveEnemies);

            gunner.AttackTarget(_player);
        }
    }

    public static Vector3 GetProjectileVelocity(Vector3 targetPosition, Vector3 targetVelocity, Vector3 startingPoint, float projectileSpeed, float delay = 0f)
    {
        float distanceToTarget = (targetPosition - startingPoint).Length();
        float timeToHit = distanceToTarget / projectileSpeed + delay;

        Vector3 finalPosition = targetPosition + targetVelocity * timeToHit;

        Vector3 direction = (finalPosition - startingPoint).Normalized();

        return direction * projectileSpeed;
    }

    private bool CanPointBeSeen(Vector3 from, Vector3 to)
    {
        _wallDetection.GlobalTranslation = from;
        _wallDetection.CastTo = _wallDetection.ToLocal(to);
        _wallDetection.ForceRaycastUpdate();

        return !_wallDetection.IsColliding();
    }

    private void PlayerEnteredArea(Spatial spatial)
    {
        if (spatial is Player player)
        {
            _player = player;
        }
    }

    private void PlayerExitedArea(Spatial spatial)
    {
        if (_player == spatial)
        {
            _player = null;

            _updatePositionsTimer?.Stop();
            _updatePositionsTimer = null;
            _runAttackRoutine?.Stop();
            _runAttackRoutine = null;
        }
    }

    private class RandomOrderQueue<T>
    {
        private readonly RandomNumberGenerator _rng;
        private List<T> _unretrievedElements = new List<T>();
        private List<T> _retrievedElements = new List<T>();

        public RandomOrderQueue(RandomNumberGenerator rng)
        {
            _rng = rng;
        }

        public void AddElement(T element)
        {
            int index = _rng.RandiRange(0, Math.Max(_unretrievedElements.Count - 1, 0));
            _unretrievedElements.Insert(index, element);
        }   

        public void RemoveElement(T element)
        {
            if (!_unretrievedElements.Remove(element))
            {
                _retrievedElements.Remove(element);
            }

            if (_unretrievedElements.Count == 0 && _retrievedElements.Count > 0)
            {
                (_unretrievedElements, _retrievedElements) = (_retrievedElements, _unretrievedElements);
                _rng.Shuffle(_unretrievedElements);
            }
        }   

        public T NextElement()
        {
            if (_unretrievedElements.Count == 0)
            {
                return default;
            }
            
            T returnedElement = _unretrievedElements.Last();
            _unretrievedElements.RemoveAt(_unretrievedElements.Count - 1);
            _retrievedElements.Add(returnedElement);

            if (_unretrievedElements.Count == 0)
            {
                (_unretrievedElements, _retrievedElements) = (_retrievedElements, _unretrievedElements);
                _rng.Shuffle(_unretrievedElements);
            }   

            return returnedElement;
        }  
    }

    private class DetectionTracker<T>
    {
        private readonly Predicate<T> _canSeeTarget;
        private readonly List<T> _inactiveEnemies = new List<T>();
        private readonly List<T> _activeEnemies = new List<T>();
        private readonly List<T> _justLeftCombat = new List<T>();
        private readonly List<T> _justEnteredCombat = new List<T>();

        public IReadOnlyList<T> InactiveEnemies => _inactiveEnemies;
        public IReadOnlyList<T> ActiveEnemies => _activeEnemies;

        public IReadOnlyList<T> JustLeftCombat => _justLeftCombat;
        public IReadOnlyList<T> JustEnteredCombat => _justEnteredCombat;

        public DetectionTracker(Predicate<T> canSeeTarget)
        {
            _canSeeTarget = canSeeTarget;
        }
        
        public void UpdateVisibility()
        {
            _justEnteredCombat.Clear();
            _justLeftCombat.Clear();

            List<(bool isEntering, T enemy)> enteringCombat = new List<(bool, T)>();

            foreach (T enemy in _inactiveEnemies)
            {
                if (_canSeeTarget.Invoke(enemy))
                {
                    enteringCombat.Add((true, enemy));
                }
            }

            foreach (T enemy in _activeEnemies)
            {
                if (!_canSeeTarget.Invoke(enemy))
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
