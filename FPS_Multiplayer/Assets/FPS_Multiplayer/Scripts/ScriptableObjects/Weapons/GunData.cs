using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Guns", menuName = "Weapons/Gun")]
public class GunData : ScriptableObject
{
    [Header("Gun Name")]
    public string GunName;

    [Header("Gun Stats")]
    public float Damage;
    public float MaxRange;

    [Header("Gun Shooting Stats")]
    public int StartingAmmo;
    public int AmmoCapacity;

    public float FireRate;
    public float ReloadTime;
    //[HideInInspector]
    public bool reload;

}
