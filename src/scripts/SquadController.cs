using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public interface IMoveable
{
    void GoTo(Vector3 point);
}

public interface IPlayerAttacker
{
    void AttackTarget(Player player);
}

public class SquadController : Node
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

        // possible use after free avoided by clearing both of these when anyone dies
        // this causes a break in the fighting, which might be a cool effect
        _queuedMovers = new DelayedQueue<(IMoveable, Vector3)>();
        _queuedAttackers = new DelayedQueue<IPlayerAttacker>();
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
        
        _queuedAttackers.Clear();
        _queuedMovers.Clear();
    }

    private void OnSniperDied(Sniper sniper)
    {
        _sniperTracker.RemoveEnemy(sniper);

        _queuedAttackers.Clear();
    }

    private void OnGunnerDied(Gunner gunner)
    {
        _gunnerTracker.RemoveEnemy(gunner);
        _gunnerMovementQueue.RemoveElement(gunner);

        _queuedAttackers.Clear();
        _queuedMovers.Clear();
    }

    private void OnTraderDied(TraderMouse trader)
    {
        _traderTracker.RemoveEnemy(trader);
        _traderMovementQueue.RemoveElement(trader);

        _queuedMovers.Clear();
    }

    public override void _PhysicsProcess(float delta)
    {
        UpdateInCombatEnemies();
        UpdateEnemyPositions();

        UpdateEnemyAttacking();
        
        RunQueuedMovers(delta);
        RunQueuedAttackers(delta);
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
        int movedGrunts = GetNormClamped(_percentageOfMovingRatGrunts, _gruntTracker.ActiveEnemies.Count);
        for (int i = 0; i < movedGrunts; i++)
        {
            RatGrunt grunt = _gruntMovementQueue.NextElement();
            float angle = _rng.RandfRange(Range.Circle);
            float distance = _rng.Randfn(_distanceToPlayerRatGrunt);

            Vector3 newPosition = playerPosition + new Vector3(distance, 0, 0).Rotated(Vector3.Up, angle);
            _queuedMovers.AddElement((grunt, newPosition));
        }

        int movedGunners = GetNormClamped(_percentageOfMovingGunners, _gunnerTracker.ActiveEnemies.Count);
        for (int i = 0; i < movedGunners; i++)
        {
            Gunner gunner = _gunnerMovementQueue.NextElement();

            float angle = _rng.RandfRange(Range.Circle);
            float distance = _rng.Randfn(_distanceToPlayerGunner);

            Vector3 newPosition = playerPosition + new Vector3(distance, 0, 0).Rotated(Vector3.Up, angle);
            _queuedMovers.AddElement((gunner, newPosition));
        }

        int movedTraders = GetNormClamped(_percentageOfMovingTraders, _traderTracker.ActiveEnemies.Count);
        for (int i = 0; i < movedTraders; i++)
        {
            TraderMouse trader = _traderMovementQueue.NextElement();

            Vector3 currentPosition = trader.GlobalTranslation;
            
            float angle = _rng.RandfRange(Range.Circle);
            float distance = _rng.Randfn(_distanceTraderMovement);

            Vector3 newPosition = currentPosition + new Vector3(distance, 0, 0).Rotated(Vector3.Up, angle);
            _queuedMovers.AddElement((trader, newPosition));
        }

        _queuedMovers.RedistributeOver(_rng, _totalMovementTime);
    }

    private void UpdateEnemyAttacking()
    {
        if (_gruntTracker.ActiveEnemies.Count + _gunnerTracker.ActiveEnemies.Count + _sniperTracker.ActiveEnemies.Count == 0)
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

        int gruntAttackers = GetNormClamped(_percentageAttackingGrunts, _gruntTracker.ActiveEnemies.Count);
        foreach (RatGrunt grunt in _rng.RandEls(_gruntTracker.ActiveEnemies, gruntAttackers))
        {
            _queuedAttackers.AddElement(grunt);
        }

        int sniperAttackers = GetNormClamped(_percentageAttackingSnipers, _sniperTracker.ActiveEnemies.Count);
        foreach (Sniper sniper in _rng.RandEls(_sniperTracker.ActiveEnemies, sniperAttackers))
        {
            _queuedAttackers.AddElement(sniper);
        }

        int gunnerAttackers = GetNormClamped(_percentageAttackingGunners, _gunnerTracker.ActiveEnemies.Count);
        foreach (Gunner gunner in _rng.RandEls(_gunnerTracker.ActiveEnemies, gunnerAttackers))
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

    private int GetNormClamped(Distro distro, int max)
    {
        float percentage = _rng.Randfn(distro);
        return Mathf.Clamp(Mathf.CeilToInt(percentage * max), 0, max);
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

    private class DelayedQueue<T>
    {
        private readonly List<T> _elements = new List<T>();
        private readonly List<float> _times = new List<float>();
        private float _currentTime;

        public void AddElement(T element)
        {
            _elements.Add(element);
        }

        public IReadOnlyList<T> PopElements(float delta)
        {
            _currentTime += delta;

            if (_times.Count == 0 || _times[0] > _currentTime)
            {
                return Array.Empty<T>();
            }

            List<T> ret = new List<T>();
            int poppedCount = 0;
            while (poppedCount < _times.Count && _times[poppedCount] < _currentTime)            
            {
                ret.Add(_elements[poppedCount]);
                poppedCount += 1;
            }

            _elements.RemoveRange(0, poppedCount);
            _times.RemoveRange(0, poppedCount);

            return ret;
        }
        
        public void RedistributeOver(RandomNumberGenerator rng, float time)
        {
            _currentTime = 0.0f;
            _times.Clear();

            for (int i = 0; i < _elements.Count; i++)
            {
                _times.Add(rng.Randf() * time);
            }

            _times.Sort();
        }

        public void Clear()
        {
            _elements.Clear();
            _times.Clear();   
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
