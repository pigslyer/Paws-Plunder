using Godot;

namespace PawsPlunder;

public partial class VolumeSlider : HSlider
{
	[Export] private string _busName = "";
	
	public void Adjust()
	{
		Value = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(_getBusIndex()));
	}
	
	private void _on_slider_value_changed(float value)
	{
		AudioServer.SetBusVolumeDb(_getBusIndex(), Mathf.LinearToDb(value));
	}

	private int _getBusIndex()
	{
		return AudioServer.GetBusIndex(_busName);
	}
}
