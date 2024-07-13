using Godot;

namespace PawsPlunder;

public partial class HardCodedItem : Node3D, IItem
{
    [Export] public string ItemName { get; private set; } = "Placeholder"; 

    [Export] public string DisplayName { get; private set; } = "Placeholder"; 

    [Export] public int AssociatedScore { get; private set; }

    void IItem.PickedUp()
    {
        QueueFree();
    }
}
