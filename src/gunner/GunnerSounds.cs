using Godot;

namespace PawsPlunder;

public partial class GunnerSounds : Node
{
    [Export] public AudioStreamPlayer3D Death = null!;
    [Export] public AudioStreamPlayer3D Footsteps = null!;
    [Export] public AudioStreamPlayer3D Blunderbuss = null!;
    [Export] public AudioStreamPlayer3D AttackBark = null!;
}
