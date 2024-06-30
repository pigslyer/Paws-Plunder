using Godot;

public class VolumeSlider : HSlider
{
	[Export] private string _busName;
	
	public void Adjust()
	{
		Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(_getBusIndex()));
	}
	
	private void _on_slider_value_changed(float value)
	{
		AudioServer.SetBusVolumeDb(_getBusIndex(), GD.Linear2Db(value));
	}

	private int _getBusIndex()
	{
		return AudioServer.GetBusIndex(_busName);
	}
}
