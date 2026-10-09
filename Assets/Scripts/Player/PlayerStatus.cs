using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// only allow one PlayerStatus component per GameObject
[DisallowMultipleComponent]

public class PlayerStatus : NetworkBehaviour
{
    [SerializeField] private float currentHP;
    [SerializeField] private float maxHP;
    [SerializeField] private Slider healthSlider;
    //probably other values (ammo, buffs, debuffs)

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        currentHP = maxHP;

        healthSlider.maxValue = maxHP;
        healthSlider.minValue = 0;
        healthSlider.value = currentHP;
    }

    public void takeDamage(float damageAmount)
    {
        Debug.Log("Applying Damage!");
        currentHP -= damageAmount;
        currentHP = Mathf.Max(0, currentHP); // prevent HP from dropping below 0
        healthSlider.value = currentHP;
        Debug.Log("HP: " + currentHP);
    }
}
