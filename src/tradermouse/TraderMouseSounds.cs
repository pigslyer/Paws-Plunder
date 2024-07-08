using Godot;

namespace PawsPlunder;

public partial class TraderMouseSounds : Node
{
    [Export] public AudioStreamPlayer3D Death = null!;
    [Export] public AudioStreamPlayer3D Footsteps = null!;
    [Export] public AudioStreamPlayer3D Panic = null!;
}
