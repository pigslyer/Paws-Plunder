using Godot;

public class RatGrunt : KinematicBody, IMeleeTargettable, IBulletHittable
{
    [Export] private PackedScene _bulletScene;

    private float _speed = 1.0f;
    private bool _isInCombat = false;

    private NavigationAgent _agent;
    private RayCast _playerSeekingRay;
    private Timer _outOfCombatTimer;
    private Timer _refreshPathTimer;

    public override void _Ready()
    {
        _agent = GetNode<NavigationAgent>("NavigationAgent");
        _playerSeekingRay = GetNode<RayCast>("RayCast");
        _outOfCombatTimer = GetNode<Timer>("OutOfCombatTimer");
        _refreshPathTimer = GetNode<Timer>("RefreshPathTimer");

        _outOfCombatTimer.Connect("timeout", this, nameof(OnCombatTimerEnded));
        _refreshPathTimer.Connect("timeout", this, nameof(OnRefreshPath));
    }

    public override void _PhysicsProcess(float delta)
    {
        UpdateInCombat();
        FollowPath(delta);

        _playerSeekingRay.CastTo = _playerSeekingRay.ToLocal(Globals.GetPlayer().GetGlobalPosition());
    }

    private void UpdateInCombat()
    {
        bool canSeePlayer = !_playerSeekingRay.IsColliding();

        if (_isInCombat != canSeePlayer)
        {
            if (_isInCombat)
            {
                if (!_outOfCombatTimer.IsStopped())
                {
                    _outOfCombatTimer.Start();
                }
            }
            else
            {
                //Globals.GetSquadController().EnterCombat(this);
                _isInCombat = true;

                _outOfCombatTimer.Stop();
            }
        }
    }

    private void FollowPath(float delta)
    {
        if (_isInCombat && _refreshPathTimer.IsStopped())
        {
            _refreshPathTimer.Start();
            _agent.SetTargetLocation(Globals.GetPlayer().GetGlobalPosition());
        }
        else if (!_isInCombat && !_refreshPathTimer.IsStopped())
        {
            _refreshPathTimer.Stop();
        }

        if (!_isInCombat)
        {
            return;
        }        

        Vector3 nextPosition = _agent.GetNextLocation();

        GlobalTranslation = GlobalTranslation.MoveToward(nextPosition, _speed * delta);
    }

    private void OnCombatTimerEnded()
    {
//        Globals.GetSquadController().LeaveCombat(this);
        _isInCombat = false;
    }

    private void OnRefreshPath()
    {
        _agent.SetTargetLocation(Globals.GetPlayer().GetGlobalPosition());
    }


    public void Target(MeleeTargetInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void AttackTarget()
    {
        Spatial spatial = (Spatial)null;

        Bullet bullet = _bulletScene.Instance<Bullet>();        
        GetTree().Root.AddChild(bullet);
        bullet.SetGlobalPosition(this.GetGlobalPosition());
        bullet.Initialize((spatial.GetGlobalPosition() - this.GetGlobalPosition()).Normalized() * 20f, PhysicsLayers3D.World | PhysicsLayers3D.Player);
    }

    void IBulletHittable.Hit()
    {
        DestroyModel();
    }

    private void DestroyModel()
    {
        //Globals.GetSquadController().LeaveCombat(this);
        QueueFree();
    }
}
