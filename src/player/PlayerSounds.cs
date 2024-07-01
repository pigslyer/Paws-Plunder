using Godot;

public class PlayerSounds : Node
{
    public AudioStreamPlayer PickupTreasure;
    public AudioStreamPlayer Hurt;
    public AudioStreamPlayer Death;
    public AudioStreamPlayer Footsteps;
    public AudioStreamPlayer Jumping;
    public AudioStreamPlayer Landing;
    public AudioStreamPlayer ShootSingle;
    public AudioStreamPlayer PickupSingle;
    public AudioStreamPlayer ShootQuad;
    public AudioStreamPlayer PickupQuad;

    public override void _Ready()
    {
        PickupTreasure = GetNode<AudioStreamPlayer>("PickupTreasure");
        Hurt = GetNode<AudioStreamPlayer>("Hurt");
        Death = GetNode<AudioStreamPlayer>("Death");
        Footsteps = GetNode<AudioStreamPlayer>("Footsteps");
        Jumping = GetNode<AudioStreamPlayer>("Jumping");
        Landing = GetNode<AudioStreamPlayer>("Landing");
        ShootSingle = GetNode<AudioStreamPlayer>("ShootSingle");
        PickupSingle = GetNode<AudioStreamPlayer>("PickupSingle");
        ShootQuad = GetNode<AudioStreamPlayer>("ShootQuad");
        PickupQuad = GetNode<AudioStreamPlayer>("PickupQuad");
    }
}
