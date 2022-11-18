using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
[DisallowMultipleComponent]
[AddComponentMenu("Network/Network Manager HUD")]
[RequireComponent(typeof(NetworkManager))]
public class NetworkHuD : MonoBehaviour
{
    //networking main menu ui
    NetworkManager manager;
    [SerializeField] TMP_InputField ipAdress;
    void Awake()
    {
        manager = GetComponent<NetworkManager>();
    }

    public void StartHosting()
    {
        if (!NetworkClient.active)
        {
            manager.StartHost();
        }
    }
    public void StartClientJoin()
    {
        if (!NetworkClient.active)
        {
            manager.networkAddress = ipAdress.text;
            manager.StartClient();
        }
        
    }

    
}
