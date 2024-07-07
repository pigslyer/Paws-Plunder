using Godot;

namespace PawsPlunder;

public partial class HealthContainer : VBoxContainer
{
    private TextureRect[] _health = new TextureRect[3];

    public override void _Ready()
    {
        _health[0] = GetNode<TextureRect>("Health1/TextureRect");
        _health[1] = GetNode<TextureRect>("Health2/TextureRect");
        _health[2] = GetNode<TextureRect>("Health3/TextureRect");
    }

    public void SetHealth(int health)
    {
        for (int i = 0; i < 3; i++)
        {
            _health[i].Modulate = i < health ? new Color(1.0F, 1.0F, 1.0F) : new Color(0.2F, 0.2F, 0.2F);
        }
    }
}
