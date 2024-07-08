using Godot;

namespace PawsPlunder;

public partial class PlayerSounds : Node
{
    [Export] public AudioStreamPlayer PickupTreasure = null!;
    [Export] public AudioStreamPlayer Hurt = null!;
    [Export] public AudioStreamPlayer Death = null!;
    [Export] public AudioStreamPlayer Footsteps = null!;
    [Export] public AudioStreamPlayer Jumping = null!;
    [Export] public AudioStreamPlayer Landing = null!;
    [Export] public AudioStreamPlayer ShootSingle = null!;
    [Export] public AudioStreamPlayer PickupSingle = null!;
    [Export] public AudioStreamPlayer ShootQuad = null!;
    [Export] public AudioStreamPlayer PickupQuad = null!;
    [Export] public AudioStreamPlayer Melee = null!;
    [Export] public AudioStreamPlayer MeleeMiss = null!;
}
