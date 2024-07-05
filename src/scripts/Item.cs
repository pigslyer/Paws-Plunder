namespace PawsPlunder;

public interface IItem 
{
	string ItemName { get; }
	string DisplayName { get; }
	int AssociatedScore { get; }
}
