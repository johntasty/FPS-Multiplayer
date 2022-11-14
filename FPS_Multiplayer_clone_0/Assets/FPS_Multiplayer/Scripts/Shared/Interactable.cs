using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Interactable : NetworkBehaviour, Damageable
{
    [SyncVar]
    public float Health = 100f;

    [Command(requiresAuthority = false)]
    public void CmdDamage(float damgeValue)
    {
       
        Health -= damgeValue;
        if (Health <= 0)
        {
            Debug.Log("Dead");
            DestoryNpc();
        }
    }
    #region Server
    [Server]
    private void DestoryNpc()
    {
        NetworkServer.Destroy(gameObject);
    }
    #endregion

}
