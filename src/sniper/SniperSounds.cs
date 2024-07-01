using Godot;

public class SniperSounds : Node
{
	public AudioStreamPlayer3D Death;
	public AudioStreamPlayer3D Shoot;
	public AudioStreamPlayer3D AttackBarkFront;
	public AudioStreamPlayer3D AttackBarkBack;

	public override void _Ready()
	{
		Death = GetNode<AudioStreamPlayer3D>("Death");
		Shoot = GetNode<AudioStreamPlayer3D>("Shoot");
		AttackBarkFront = GetNode<AudioStreamPlayer3D>("AttackBarkFront");
		AttackBarkBack = GetNode<AudioStreamPlayer3D>("AttackBarkBack");
	}
}
