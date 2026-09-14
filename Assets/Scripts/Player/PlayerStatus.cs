using Unity.Netcode;
using UnityEngine;

//Not sure if this is neededbut we "probably" won't need multiple status' on a player game object
[DisallowMultipleComponent]

public class PlayerStatus : NetworkBehaviour
{
    [SerializeField] private float currentHP;
    [SerializeField] private float maxHP;
    //probably other values (ammo, buffs, debuffs)
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        currentHP = maxHP;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void takeDamage(float damageAmount)
    {
        Debug.Log("Applying Damage!");
        //applies damage from currentHP
        currentHP -= damageAmount;
        Debug.Log("HP: " + currentHP);
    }
}
