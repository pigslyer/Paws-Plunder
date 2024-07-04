using Godot;

public class HardCodedItem : Node, IItem
{
    [Export] public string ItemName { get; private set; } = "Placeholder"; 

    [Export] public string DisplayName { get; private set; } = "Placeholder"; 

    [Export] public int AssociatedScore { get; private set; }
}
