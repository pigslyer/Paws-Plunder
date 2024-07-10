using Godot;

namespace PawsPlunder;

public partial class Pause : Control
{
	[Export] private Control _menu = null!;
	[Export] private Options _options = null!;
	[Export] private Button _saveOptionsButton = null!;

	public override void _Ready()
	{
		_options.Visible = false;
		_options.Load();
	}

	public void TogglePause(bool enable)
	{
		GetTree().Paused = enable;
		Visible = enable;
		_CloseSettings();
		if (enable)
		{
			Logger.Debug("Game paused.");
			Input.MouseMode = Input.MouseModeEnum.Visible;
			Logger.Debug("Mouse mode set to visible.");

		}
		else
		{
			Logger.Debug("Game resumed.");
			Input.MouseMode = Input.MouseModeEnum.Captured;
			Logger.Debug("Mouse mode set to captured.");
		}
	}

	private void _OnPlayButtonPressed()
	{
		TogglePause(false);
	}

	private void _OnOptionsButtonPressed()
	{
		GetNode<MarginContainer>("%ButtonsContainer").Visible = false;
		_options.Visible = true;
		_saveOptionsButton.Visible = true;
		_options.OnOpen();
	}

	private void _CloseSettings()
	{
		GetNode<MarginContainer>("%ButtonsContainer").Visible = true;
		_options.Visible = false;
		_saveOptionsButton.Visible = false;
	}
	private void _OnSaveSettingsButtonPressed()
	{
		_options.Save();
		_CloseSettings();
	}

	private void _OnReturnToMainMenuButtonPressed()
	{
		// TODO: implement
		GetTree().Quit();
	}
	private void _OnQuitButtonPressed()
	{
		GetTree().Quit();
	}
}

