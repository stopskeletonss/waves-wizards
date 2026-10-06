using System.Security.Cryptography;
using Unity.Netcode;
using UnityEngine;

//Not sure if this is neededbut we "probably" won't need multiple status' on a player game object
[DisallowMultipleComponent]

public class PlayerStatus : NetworkBehaviour
{
    [SerializeField] private float currentHP;
    [SerializeField] private float maxHP;
    //probably other values (ammo, buffs, debuffs)
    private bool isAlive;
    private FirstPersonController FPC;
    private bool invulnerable = false;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isAlive = true;
        currentHP = maxHP;
        FPC = GetComponent<FirstPersonController>();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyUp(KeyCode.I))
            {
                bool state = invulnerable;
                invulnerable = !state;
            }
        }
        
    }



    public void takeDamage(float damageAmount)
    {
        
        if(!invulnerable)
        {
        Debug.Log("Applying Damage!");
        //applies damage from currentHP
        currentHP -= damageAmount;
        Debug.Log("HP: " + currentHP);
        
            aliveCheck();
        }
    }

    void aliveCheck()
    {
        if(currentHP <= 0)
        {
            isAlive = false;
        }
        if(isAlive == false)
        {
            FPC.changeSpeed(0);
            FPC.enabled = false;
        }
    }
}
