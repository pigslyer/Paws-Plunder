using Godot;

namespace PawsPlunder;

public partial class Options : Control
{
	private ConfigFile _configFile = new();


	[Export] private HSlider _mouse = null!;
	[Export] private VolumeSlider _master = null!;
	[Export] private VolumeSlider _music = null!;
	[Export] private VolumeSlider _sfx = null!;
	[Export] private VolumeSlider _voice = null!;

	public void OnOpen()
	{
		_mouse.Value = Globals.MouseSensitivity;
		_master.Adjust();
		_music.Adjust();
		_voice.Adjust();
		_master.Adjust();
	}
	public void Load()
	{
		Error loadError = _configFile.Load(Constants.SETTINGS_PATH);
		if (loadError == Error.FileNotFound)
		{
			Logger.Warn("Settings file not found, creating new one");
			Save();
		}
		else if (loadError != Error.Ok)
		{
			Logger.Error($"Got load error {loadError} while trying to read settings file");
			return;
		}
		Logger.Info($"Settings file loaded from {Constants.SETTINGS_PATH}");
		Logger.Debug($"User settings:\n{_configFile.EncodeToText()}");
		Globals.MouseSensitivity = (float)_configFile.GetValue("Mouse", "MouseSensitivity", 1.0F);
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Mathf.LinearToDb((float)_configFile.GetValue("Volume", "Master", 1.0F)));
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), Mathf.LinearToDb((float)_configFile.GetValue("Volume", "Music", 1.0F)));
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), Mathf.LinearToDb((float)_configFile.GetValue("Volume", "SFX", 1.0F)));
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Voice"), Mathf.LinearToDb((float)_configFile.GetValue("Volume", "Voice", 1.0F)));
	}

	private void _OnMouseValueChanged(bool isChanged)
	{
		Globals.MouseSensitivity = (float)_mouse.Value;
	}

	public void Save()
	{
		_configFile.SetValue("Mouse", "MouseSensitivity", Globals.MouseSensitivity);
		_configFile.SetValue("Volume", "Master", Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Master"))));
		_configFile.SetValue("Volume", "Music", Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Music"))));
		_configFile.SetValue("Volume", "SFX", Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("SFX"))));
		_configFile.SetValue("Volume", "Voice", Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Voice"))));
		Error saveError = _configFile.Save(Constants.SETTINGS_PATH);
		if (saveError != Error.Ok)
		{
			Logger.Error($"Got save error {saveError} while trying to read settings file");
			return;
		}
		Logger.Info($"Settings file saved to {Constants.SETTINGS_PATH}");
		Logger.Debug($"New user settings:\n{_configFile.EncodeToText()}");
	}
}




