using System.Diagnostics;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [SerializeField] private float currentHP;
    [SerializeField] private float maxHP;
    //probably other values (ammo, buffs, debuffs)
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHP;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void takeDamage(float damageAmount)
    {
        currentHP -= damageAmount;
        Debug.Log("HP: " + currentHP);
    }
}
