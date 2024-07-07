using Godot;

namespace PawsPlunder;
public partial class RatGruntSounds : Node
{
    public AudioStreamPlayer3D Fire;
    public AudioStreamPlayer3D Death;
    public AudioStreamPlayer3D Footsteps;
    public AudioStreamPlayer3D AttackBark;

    public override void _Ready()
    {
        Fire = GetNode<AudioStreamPlayer3D>("Fire");
        Death = GetNode<AudioStreamPlayer3D>("Death");
        Footsteps = GetNode<AudioStreamPlayer3D>("Footsteps");
        AttackBark = GetNode<AudioStreamPlayer3D>("AttackBark");
    }
}
