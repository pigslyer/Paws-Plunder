using Godot;

namespace PawsPlunder;

public partial class GunnerSounds : Node
{
    public AudioStreamPlayer3D Death;
    public AudioStreamPlayer3D Footsteps;
    public AudioStreamPlayer3D Blunderbuss;
    public AudioStreamPlayer3D AttackBark;

    public override void _Ready()
    {
        Death = GetNode<AudioStreamPlayer3D>("Death");
        Footsteps = GetNode<AudioStreamPlayer3D>("Footsteps");
        Blunderbuss = GetNode<AudioStreamPlayer3D>("Blunderbuss");
        AttackBark = GetNode<AudioStreamPlayer3D>("AttackBark");
    }
}
