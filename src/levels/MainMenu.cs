using Godot;

public class MainMenu : CanvasLayer
{
	private Control _menu;
	private Options _options;
	
	[Signal]
	public delegate void StartGame();

	public override void _Ready()
	{
		_menu = GetNode<Control>("Menu");
		_options = GetNode<Options>("Options");
		_menu.Visible = true;
		_options.Visible = false;
		_options.Load();
	}

	private void _on_PlayButton_pressed()
	{
		EmitSignal("StartGame");
	}
	
	private void _on_OptionsButton_pressed()
	{
		_menu.Visible = false;
		_options.Show();
	}
	
	private void _on_ExitButton_pressed()
	{
		GetTree().Quit();
	}
	
	private void _on_BackButton_pressed()
	{
		_menu.Visible = true;
		_options.Visible = false;
		_options.Save();
	}
}
