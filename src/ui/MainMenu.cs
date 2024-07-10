using Godot;

namespace PawsPlunder;

public partial class MainMenu : Control
{
	[Export] private Control _menu = null!;
	[Export] private Options _options = null!;
	[Export] private Button _saveOptionsButton = null!;
	[Export] private Control _storyPanel = null!;	
	[Export] private Label _storyLabel = null!;
	[Export] private AudioStreamPlayer _musicMenu = null!;

	[Signal]
	public delegate void StartGameEventHandler();

	public override void _Ready()
	{
		GetNode<Label>("%VersionLabel").Text = "Version: " + ProjectSettings.GetSetting("application/config/version").ToString();
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Logger.Debug("Mouse mode set to visible.");
		_menu.Visible = true;
		_options.Visible = false;
		_options.Load();
	}

	private void _OnPlayButtonPressed()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		Logger.Debug("Mouse mode set to captured.");
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

		_OnIntroButtonPressed();
	}

	private void _OnIntroButtonPressed()
	{
		EmitSignal(SignalName.StartGame);
	}
	
	private void _OnOptionsButtonPressed()
	{
		GetNode<MarginContainer>("%ButtonsContainer").Visible = false;
		_options.Visible = true;
		_saveOptionsButton.Visible = true;
		_options.OnOpen();
	}

	private void _OnSaveSettingsButtonPressed()
	{
		_options.Save();
		GetNode<MarginContainer>("%ButtonsContainer").Visible = true;
		_options.Visible = false;
		_saveOptionsButton.Visible = false;
	}
	
	private void _OnExitButtonPressed()
	{
		GetTree().Quit();
	}
}




