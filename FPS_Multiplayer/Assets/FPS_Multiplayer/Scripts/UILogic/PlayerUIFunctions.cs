using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
public class PlayerUIFunctions : NetworkBehaviour
{
    [SerializeField] GameObject CanvasUI = null;
    [SerializeField] GameObject ControlPanel = null;
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
    private void OnUiOpen()
    {
        if (!isOwned) { return; }
        bool value = !ControlPanel.activeInHierarchy;
        ControlPanel.SetActive(value);
    }
}
