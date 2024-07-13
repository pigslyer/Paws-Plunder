
using System;
using Godot;

namespace PawsPlunder;

public enum TreasureType : int
{
    Lasagna,
    WetLasaga,
    CatBust,
    Tuna,
    Catnip,
    CatCoin,
    CatBag
}

public partial class Treasure : Node3D, IItem
{
    [Export] private TreasureType _type;

    public string ItemName => "Treasure";

    public string DisplayName => _type switch
    {
        TreasureType.Lasagna => "Lasagna",
        TreasureType.WetLasaga => "Wet Lasagna",
        TreasureType.CatBust => "Cat statue",
        TreasureType.Tuna => "Tuna",
        TreasureType.Catnip => "Catnip",
        TreasureType.CatCoin => "Coin",
        TreasureType.CatBag => "Bag of Coins",
        _ => throw new NotImplementedException(),
    };

    public int AssociatedScore => _type switch
    {
        TreasureType.Lasagna => 10000,
        TreasureType.WetLasaga => -1000,
        TreasureType.CatBust => 2000,
        TreasureType.Catnip => 5000,
        TreasureType.CatCoin => 500,
        TreasureType.CatBag => 1000,
        _ => throw new NotImplementedException(),
    };

    public override void _Ready()
    {
        Sprite3D sprite = GetNode<Sprite3D>("Sprite3D");
        sprite.Frame = (int)_type;
    }

    void IItem.PickedUp()
    {
        QueueFree();
    }
}
