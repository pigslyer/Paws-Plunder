using Godot;

namespace PawsPlunder;

public partial class MainMenu : Control
{
	[Export] private Control _menu = null!;
	[Export] private Options _options = null!;
	[Export] private Control _storyPanel = null!;	
	[Export] private Label _storyLabel = null!;
	[Export] private AudioStreamPlayer _musicMenu = null!;

	[Signal]
	public delegate void StartGameEventHandler();

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_menu.Visible = true;
		_options.Visible = false;
		_options.Load();
	}

	private void _on_PlayButton_pressed()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_musicMenu.Stop();

		/*
		foreach (Node node in GetTree().GetNodesInGroup("MAIN_BUTTON"))
		{
			if (node is CanvasItem canvasItem)
			{
				canvasItem.Visible = false;
			}
		}
		*/
		//_storyPanel.Visible = true;
		//_storyLabel.Text = _storyLabel.Text.Replace("%NAME%", Globals.ProtagonistName);

		OnIntroButtonPressed();
	}

	private void OnIntroButtonPressed()
	{
		EmitSignal(SignalName.StartGame);
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
