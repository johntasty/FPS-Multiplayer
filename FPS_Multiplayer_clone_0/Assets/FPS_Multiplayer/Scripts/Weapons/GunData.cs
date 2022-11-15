using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
[CreateAssetMenu(fileName = "Guns", menuName = "Weapons/Gun")]
public class GunData : ScriptableObject
{
    [Header("Gun Name")]
    public string GunName;

    [Header("Gun Stats")]
    public float Damage;
    public float MaxRange;

    [Header("Gun Shooting Stats")]
    [SyncVar]
    public int StartingAmmo;
    public int AmmoCapacity;

    public float FireRate;
    public float ReloadTime;
    [Header("Gun Effects")]
    public ParticleSystem muzzleFlash;
    [Header("Bullet Settings")]
    public float bulletSpeed;
    public float bulletDrop;

    //[HideInInspector]
    [SyncVar]
    public bool reload;

}
