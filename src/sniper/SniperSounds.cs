using Godot;

namespace PawsPlunder;

public partial class SniperSounds : Node
{
	[Export] public AudioStreamPlayer3D Death = null!;
	[Export] public AudioStreamPlayer3D Shoot = null!;
	[Export] public AudioStreamPlayer3D AttackBarkFront = null!;
	[Export] public AudioStreamPlayer3D AttackBarkBack = null!;
}
