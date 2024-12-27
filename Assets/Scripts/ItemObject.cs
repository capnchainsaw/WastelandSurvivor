using UnityEngine;

public class ItemObject : CellObject
{
    public int StrengthAmount;
    public int DefenseAmount;
    public int StaminaAmount;
    public override void PlayerEntered()
    {
       Destroy(gameObject);
      
       // Increase Stats
       PlayerController player = GameManager.Instance.PlayerController;
       player.AdjustStrength(StrengthAmount);
       player.AdjustDefense(DefenseAmount);
       player.AdjustStamina(StaminaAmount, 0);
    }
}
