using Godot;

namespace PawsPlunder;

public partial class TraderMouseSounds : Node
{
    public AudioStreamPlayer3D Death;
    public AudioStreamPlayer3D Footsteps;
    public AudioStreamPlayer3D Panic;

    public override void _Ready()
    {
        Death = GetNode<AudioStreamPlayer3D>("Death");
        Footsteps = GetNode<AudioStreamPlayer3D>("Footsteps");
        Panic = GetNode<AudioStreamPlayer3D>("Panic");
    }
}
