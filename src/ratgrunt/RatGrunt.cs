using Godot;

public class RatGrunt : KinematicBody, ISquadCombatant, IMeleeTargettable, IBulletHittable
{
    [Export] private PackedScene _bulletScene;

    private bool _isInCombat = false;

    private NavigationAgent _agent;
    private RayCast _playerSeekingRay;
    private Timer _outOfCombatTimer;

    public override void _Ready()
    {
        _agent = GetNode<NavigationAgent>("NavigationAgent");
        _playerSeekingRay = GetNode<RayCast>("RayCast");
        _outOfCombatTimer = GetNode<Timer>("Timer");

        _outOfCombatTimer.Connect("timeout", this, nameof(OnCombatTimerEnded));
    }

    public override void _PhysicsProcess(float delta)
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
                Globals.GetSquadController().EnterCombat(this);
                _isInCombat = true;

                _outOfCombatTimer.Stop();
            }
        }

        _playerSeekingRay.CastTo = _playerSeekingRay.ToLocal(Globals.GetPlayer().GetGlobalPosition());
    }

    private void OnCombatTimerEnded()
    {
        Globals.GetSquadController().LeaveCombat(this);
        _isInCombat = false;
    }


    public void Target(MeleeTargetInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void AttackTarget(ISquadTarget target)
    {
        Spatial spatial = (Spatial)target;

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
        Globals.GetSquadController().LeaveCombat(this);
        QueueFree();
    }
}
