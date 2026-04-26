using System.Numerics;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

//I think it would be valuable to have the health bar UI rotating to the players camera...
//...just not sure how to do that when accounting for multiple cameras (since this is multiplayer) 
public class TestHealthBar : MonoBehaviour
{
    [SerializeField]
    private Image healthbarSprite;
    //Call this anytime an enemy health is adjusted to update the ui
    public void UpdateHealthBar(float maxHP, float currentHP)
    {
        healthbarSprite.fillAmount = currentHP / maxHP;
    }


}
