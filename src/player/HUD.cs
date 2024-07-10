using Godot;

namespace PawsPlunder;
public partial class HUD : CanvasLayer
{
	[Export] private HealthContainer _healthContainer = null!;
	[Export] private CombatLog _logControl = null!;
	[Export] private ColorRect _damageEffect = null!;
	[Export] private DoomPortrait _doomPortrait = null!;
	[Export] private TextureRect _crosshair = null!;
	[Export] private Label _deathLabel = null!;

	public override void _Ready()
	{
	}

	public void SetDoomPortrait(DoomPortraitType type)
	{
		_doomPortrait.SetAnimation(type);
	}

	public void PushLog(string message)
	{
		_logControl.PushMsg(message);
	}

	public void TakeDamage()
	{
		_doomPortrait.SetAnimation(DoomPortraitType.Pain);
		_damageEffect.Material.Set("shader_parameter/enable", true);
	}

	public void HealDamage()
	{
		_damageEffect.Material.Set("shader_parameter/enable", false);
		_doomPortrait.SetAnimation(DoomPortraitType.Idle);
	}

	public void UpdateHealth(int newHealth)
	{
		bool isDead = newHealth <= 0;
		_healthContainer.SetHealth(newHealth);
		_doomPortrait.SetAnimation(isDead ? DoomPortraitType.Death : DoomPortraitType.Idle);
		_damageEffect.Material.Set("shader_parameter/enable", isDead);
		_deathLabel.Visible = isDead;
		_crosshair.Visible = !isDead;
	}
}
