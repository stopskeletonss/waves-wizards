using System.Security.Cryptography;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

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
    //private Label healthUI;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isAlive = true;
        currentHP = maxHP;
        FPC = GetComponent<FirstPersonController>();
       // var uiRef = GetComponent<UIDocument>().rootVisualElement;
       // healthUI =  uiRef.Q<Label>("HPAmount");
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
        //Debug.Log("Applying Damage!");
        //applies damage from currentHP
        currentHP -= damageAmount;
        //healthUI.text = currentHP.ToString();
        //Debug.Log("HP: " + currentHP);
        
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
