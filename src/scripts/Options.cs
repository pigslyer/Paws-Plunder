using Godot;

namespace PawsPlunder;

public partial class Options : Control
{
	private ConfigFile _configFile = new();
	private const string SettingsPath = "user://settings.ini";
	
	[Export] private HSlider _mouse = null!;
	[Export] private VolumeSlider _master = null!;
	[Export] private VolumeSlider _music = null!;
	[Export] private VolumeSlider _sfx = null!;
	[Export] private VolumeSlider _voice = null!;

	public void Save()
	{
		_configFile.SetValue("Misc", "MouseSensitivity", Globals.MouseSensitivity);
		_configFile.SetValue("Volume", "Master", Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Master"))));
		_configFile.SetValue("Volume", "Music", Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Music"))));
		_configFile.SetValue("Volume", "SFX", Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("SFX"))));
		_configFile.SetValue("Volume", "Voice", Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Voice"))));
		_configFile.Save(SettingsPath);
	}

	// TODO: make this not use Show, figure out what the editor is doing
	public new void Show()
	{
		Visible = true;
		_mouse.Value = Globals.MouseSensitivity;
		_master.Adjust();
		_music.Adjust();
		_voice.Adjust();
		_master.Adjust();
	}
	
	public void Load()
	{
		Error loadError = _configFile.Load(SettingsPath);

		if (loadError != Error.Ok)
		{
			GD.PushError($"Got load error {loadError} while trying to read settings file");
			return;
		}

		Globals.MouseSensitivity = (float)_configFile.GetValue("Misc", "MouseSensitivity", 1.0F);
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Mathf.LinearToDb((float)_configFile.GetValue("Volume", "Master", 1.0F)));
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), Mathf.LinearToDb((float)_configFile.GetValue("Volume", "Music", 1.0F)));
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), Mathf.LinearToDb((float)_configFile.GetValue("Volume", "SFX", 1.0F)));
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Voice"), Mathf.LinearToDb((float)_configFile.GetValue("Volume", "Voice", 1.0F)));
	}
	
	private void _on_mouse_value_changed(float value)
	{
		Globals.MouseSensitivity = value;
	}
}
