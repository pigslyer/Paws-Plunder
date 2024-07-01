using Godot;

public class MainMenu : CanvasLayer
{
	private Control _menu;
	private Options _options;
	private Control _storyPanel;	
	private Label _storyLabel;
	
	[Signal]
	public delegate void StartGame();

	public override void _Ready()
	{
		_menu = GetNode<Control>("Menu");
		_options = GetNode<Options>("Options");
		_storyPanel = GetNode<Control>("Menu/StoryPanel");
		_storyLabel = GetNode<Label>("Menu/StoryPanel/MarginContainer/VBoxContainer/Intro");

		_menu.Visible = true;
		_options.Visible = false;
		_options.Load();
	}

	private void _on_PlayButton_pressed()
	{
		foreach (Node node in GetTree().GetNodesInGroup("MAIN_BUTTON"))
		{
			if (node is CanvasItem canvasItem)
			{
				canvasItem.Visible = false;
			}
		}

		_storyPanel.Visible = true;

		_storyLabel.Text = _storyLabel.Text.Replace("%NAME%", Globals.ProtagonistName);
	}

	private void OnIntroButtonPressed()
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
