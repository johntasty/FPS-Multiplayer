using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Interactable : NetworkBehaviour, Damageable
{
    [SyncVar]
    public float Health = 100f;

    public void Damage(float damgeValue)
    {
        Debug.Log("Shot");
        Health -= damgeValue;
        if (Health <= 0)
        {
            Debug.Log("Dead");
        }
    }


}
