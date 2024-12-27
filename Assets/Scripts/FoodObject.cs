using UnityEngine;

public class FoodObject : CellObject
{
    public int FoodAmount;
    public override void PlayerEntered()
    {
       Destroy(gameObject);
      
       //increase food
       GameManager.Instance.AdjustFoodAmount(FoodAmount);
    }
}
