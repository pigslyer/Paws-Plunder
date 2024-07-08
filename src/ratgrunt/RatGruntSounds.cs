using Godot;

namespace PawsPlunder;

public partial class RatGruntSounds : Node
{
    [Export] public AudioStreamPlayer3D Fire = null!;
    [Export] public AudioStreamPlayer3D Death = null!;
    [Export] public AudioStreamPlayer3D Footsteps = null!;
    [Export] public AudioStreamPlayer3D AttackBark = null!;
}
