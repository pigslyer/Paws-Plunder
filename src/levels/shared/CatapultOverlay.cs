using Godot;

public partial class CatapultOverlay : ColorRect
{
	public override void _Ready()
	{
	}

	public void Reset()
	{
		Material.Set("shader_parameter/alpha", 1f);
		Material.Set("shader_parameter/inner_radius", 0.628f);
		Material.Set("shader_parameter/outer_radius", 1f);
	}

	public void FadeOut(ref Tween tween)
	{
		tween.TweenProperty(Material, "shader_parameter/alpha", 0f, 0.5)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.InOut);

		tween.TweenProperty(Material, "shader_parameter/inner_radius", 1f, 0.5)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.OutIn);

		tween.TweenProperty(Material, "shader_parameter/outer_radius", 1f, 0.5)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.InOut);
	}

	public override void _Process(double delta)
	{
	}
}
