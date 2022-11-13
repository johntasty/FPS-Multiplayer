using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
public class ButtonFunctionality : MonoBehaviour
{
    public void TurnOnOff(GameObject objOn)
    {
        bool value = !objOn.activeInHierarchy;
        objOn.SetActive(value);
    }
   

}
