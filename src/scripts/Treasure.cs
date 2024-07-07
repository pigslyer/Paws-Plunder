
using System;
using System.Runtime.CompilerServices;
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

    public string DisplayName 
    {
        get
        {
            switch (_type)
            {
                case TreasureType.Lasagna:
                return "Lasagna";

                case TreasureType.WetLasaga:
                return "Wet Lasagna";

                case TreasureType.CatBust:
                return "Cat statue";

                case TreasureType.Tuna:
                return "Tuna";

                case TreasureType.Catnip:
                return "Catnip";

                case TreasureType.CatCoin:
                return "Coin";

                case TreasureType.CatBag:
                return "Bag of Coins";
            }

            throw new NotImplementedException();
        }
    }
    public int AssociatedScore
    {
        get
        {
            switch (_type)
            {
                case TreasureType.Lasagna:
                return 10000;

                case TreasureType.WetLasaga:
                return -1000;

                case TreasureType.CatBust:
                return 2000;

                case TreasureType.Catnip:
                return 5000;

                case TreasureType.CatCoin:
                return 500;

                case TreasureType.CatBag:
                return 1000;                 
            }

            throw new NotImplementedException();
        }
    }

    public override void _Ready()
    {
        Sprite3D sprite = GetNode<Sprite3D>("Sprite3D");
        sprite.Frame = (int)_type;
    }
}
