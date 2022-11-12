using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class PlayerUIFunctions : NetworkBehaviour
{
    [SerializeField] GameObject CanvasUI = null;
    public override void OnStartAuthority()
    {
        if (!isOwned) { return; }
        CanvasUI.SetActive(true);
    }
    public void ExitGame()
    {
        if (isClientOnly)
        {
            NetworkManager.singleton.StopClient();

        }
        NetworkManager.singleton.StopHost();
    }
}
