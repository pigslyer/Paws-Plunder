namespace PawsPlunder;

public interface IItem 
{
	string ItemName { get; }
	string DisplayName { get; }
	int AssociatedScore { get; }

	/// <summary>
	/// Signifies that this item has been picked up and should in some way cease to exist.
	/// </summary> 
	void PickedUp();
}
