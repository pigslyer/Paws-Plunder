using Godot;

public class Options : Control
{
	private ConfigFile _configFile = new ConfigFile();
	private const string Path = "user://settings.ini";
	
	private HSlider _mouse;
	private VolumeSlider _master;
	private VolumeSlider _music;
	private VolumeSlider _sfx;
	private VolumeSlider _voice;

	public override void _Ready()
	{
		_mouse = GetNode<HSlider>("Container/VBoxContainer/Mouse/VBoxContainer/Slider");
		_master = GetNode<VolumeSlider>("Container/VBoxContainer/Master/VBoxContainer/Slider");
		_music = GetNode<VolumeSlider>("Container/VBoxContainer/Music/VBoxContainer/Slider");
		_voice = GetNode<VolumeSlider>("Container/VBoxContainer/SFX/VBoxContainer/Slider");
		_master = GetNode<VolumeSlider>("Container/VBoxContainer/Voice/VBoxContainer/Slider");
	}

	public void Save()
	{
		_configFile.SetValue("Misc", "MouseSensitivity", Globals.MouseSensitivity);
		_configFile.SetValue("Volume", "Master", GD.Db2Linear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Master"))));
		_configFile.SetValue("Volume", "Music", GD.Db2Linear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Music"))));
		_configFile.SetValue("Volume", "SFX", GD.Db2Linear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("SFX"))));
		_configFile.SetValue("Volume", "Voice", GD.Db2Linear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Voice"))));
		_configFile.Save(Path);
	}

	public void Show()
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
		if (_configFile.Load(Path) == Error.Ok)
		{
			Globals.MouseSensitivity = (float)_configFile.GetValue("Misc", "MouseSensitivity", 1.0F);
			AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), GD.Linear2Db((float)_configFile.GetValue("Volume", "Master", 1.0F)));
			AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), GD.Linear2Db((float)_configFile.GetValue("Volume", "Music", 1.0F)));
			AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), GD.Linear2Db((float)_configFile.GetValue("Volume", "SFX", 1.0F)));
			AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Voice"), GD.Linear2Db((float)_configFile.GetValue("Volume", "Voice", 1.0F)));
		}
	}
	
	private void _on_mouse_value_changed(float value)
	{
		GD.Print(value);
		Globals.MouseSensitivity = value;
	}
}
