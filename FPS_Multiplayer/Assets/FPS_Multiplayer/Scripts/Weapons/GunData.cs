using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
[CreateAssetMenu(fileName = "Guns", menuName = "Weapons/Gun")]
public class GunData : ScriptableObject
{
    [Header("Gun Name")]
    public string GunName;

    [Header("Gun Prefab")]
    public GameObject GunPrefab;

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
    [Header("Bullet Tracer")]
    public TrailRenderer BulletTracer;
    [Header("Bullet Settings")]
    public float bulletSpeed;
    public float bulletDrop;
    
    //[HideInInspector]
    [SyncVar]
    public bool reload;

    [Header("Recoil Settings")]
    public RecoilSettings recoilStats;

    [System.Serializable]    
    public struct RecoilSettings
    {
        [Tooltip("Controls vertical recoil, set to Minus moves up, Plus moves down")]
        public float recoilX;
        [Tooltip("Controls horizontal recoil")]
        public float recoilY;
        [Tooltip("Adds more fluid movement to the recoil")]
        public float recoilZ;
        [Tooltip("Force of recoil")]
        public float strenght;
        [Tooltip("Speed at which it comes back")]
        public float speed;
    }

}
