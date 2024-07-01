
using System.Runtime.CompilerServices;
using Godot;

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

public class Treasure : Item
{
    [Export] private TreasureType _type;

    public override void _Ready()
    {
        Sprite3D sprite = GetNode<Sprite3D>("Sprite3D");
        sprite.Frame = (int)_type;

        switch (_type)
        {
            case TreasureType.Lasagna:
            DisplayName = "Lasagna";
            AssociatedScore = 10000;
            break;            

            case TreasureType.WetLasaga:
            DisplayName = "Wet Lasagna";
            AssociatedScore = -1000;            
            break;            

            case TreasureType.CatBust:
            DisplayName = "Cat Bust";
            AssociatedScore = 2000;            
            break;            
            
            case TreasureType.Catnip:
            DisplayName = "Catnip";
            AssociatedScore = 5000;            
            break;            
            
            case TreasureType.CatCoin:
            DisplayName = "Cat Coin";
            AssociatedScore = 500;            
            break;            
            
            case TreasureType.CatBag:
            DisplayName = "Bag of Cat Coins";
            AssociatedScore = 1000;            
            break;            
        }
    }
}
