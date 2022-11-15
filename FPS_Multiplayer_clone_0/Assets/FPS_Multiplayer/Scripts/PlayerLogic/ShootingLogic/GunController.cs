using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using System;

public class GunController : NetworkBehaviour
{
    [Header("Gun Scriptable")]
    [SerializeField] GunData gun;
    [SerializeField] Transform muzzle;
    [SerializeField] List<GameObject> muzzleFlash = new List<GameObject>();

    [Header("Impact effects, Set by name")]
    [SerializeField] List <ParticleSystem> hitEffects = new List<ParticleSystem>();
    [SerializeField] TrailRenderer bullerTracer;
    //bullet Settings
    class Bullet
    {
        public float time { get; set; }
        public Vector3 InitialPosition { get; set; }
        public Vector3 InitialVelocity { get; set; }
        public TrailRenderer tracer { get; set; }
    }
    private List<Bullet> bullets = new List<Bullet>();
    private float maxLifeBullet = 1f;
    Vector3 rayCastDestination;
    #region SyncVars
    [SyncVar]
    public float timeSinceLastShot;
    [SyncVar]
    public bool ShootingHeld = false;
    #endregion
    private bool muzzleFlashSet = false;

    public override void OnStartAuthority()
    {        
        if (!isOwned) { return; }
        enabled = true;
        
    }
    
    private bool CanShoot() => !gun.reload && timeSinceLastShot > 1f / (gun.FireRate / 60f);
    private void OnFire(InputValue value)
    {
        
        ShootingHeld = value.isPressed;
        if (!muzzleFlashSet)
        {
            muzzleFlashSet = true;
            //CmdSpawnMuzzle();
        }
        
        if (ShootingHeld)
        {
            CmdOnGunFired();
        }        
        if(gun.StartingAmmo <= 0)
        {
            CmdUpdateReload();
        }

    }
  
    private void OnReload()
    {
        CmdUpdateReload();
    }
   
   [ClientRpc]
    private void RpcShoot()
    {
        
        if (gun.StartingAmmo > 0)
        {
            if (CanShoot())
            {
                rayCastDestination = muzzle.forward * gun.MaxRange;
                GunFired();         

                gun.StartingAmmo--;
                timeSinceLastShot = 0;
                
                muzzleFlash[0].TryGetComponent(out ParticleSystem emmiter);                
                emmiter?.Emit(1);

            }
           
        }
    }
    public void UpdateShotTimer(float deltatime)
    {
        timeSinceLastShot += Time.deltaTime;
        BulletSimulation(deltatime);
        DestroyBullets();
    }

    private void DestroyBullets()
    {
        bullets.RemoveAll(bullet => bullet.time >= maxLifeBullet);
    }

    Vector3 GetBuLletPos(Bullet bullet)
    {
        Vector3 gravity = Vector3.down * gun.bulletDrop;

        return (bullet.InitialPosition + (bullet.InitialVelocity * bullet.time) + (0.5f * gravity * bullet.time * bullet.time));

    }
   void BulletSimulation(float deltatime)
    {
       
        bullets.ForEach(bullet =>
       {
           Vector3 pos0 = GetBuLletPos(bullet);
           bullet.time += deltatime;
           Vector3 posEnd = GetBuLletPos(bullet);
           RaycastSegmented(pos0, posEnd, bullet);
       });
    }

    private void RaycastSegmented(Vector3 pos0, Vector3 posEnd, Bullet bullet)
    {
        Vector3 direction = posEnd - pos0;
        float distance = direction.magnitude;
        Ray ray = new Ray();
        ray.origin = pos0;
        ray.direction = direction;
        if(Physics.Raycast(ray, out RaycastHit hit, distance))
        {
            Debug.Log("hitCheck");
            string surface = hit.transform.tag;
            HitEffect(surface, hit);
            bullet.tracer.transform.position = hit.point;
            bullet.time = maxLifeBullet;
        }
        else
        {
            bullet.tracer.transform.position = posEnd;
        }
    }

    Bullet CreateBullet(Vector3 initialPos, Vector3 initialVelocity)
    {
        Bullet bullet = new Bullet();
        bullet.InitialPosition = initialPos;
        bullet.InitialVelocity = initialVelocity;
        bullet.time = 0.0f;
        bullet.tracer = Instantiate(bullerTracer, initialPos, Quaternion.identity);
        bullet.tracer.AddPosition(initialPos);
        return bullet;
    }

    [ClientRpc]
    private void RpcReloading()
    {
        if (!gun.reload)
        {
            StartCoroutine(Reload());
        }
    }
    private void GunFired( )
    {           
        Vector3 velocity = (rayCastDestination - muzzle.position).normalized * gun.bulletSpeed;
        var bullet = CreateBullet(muzzle.position, velocity);
        bullets.Add(bullet);        
    }
    private void HitEffect(string surface, RaycastHit hit)
    {
        Damageable interactable = hit.transform.GetComponent<Damageable>();
        interactable?.CmdDamage(gun.Damage);
        rayCastDestination = hit.point;
        ParticleSystem surfaceEffect = hitEffects.Find(x => x.name == surface);
        if (surfaceEffect == null) surfaceEffect = hitEffects[0];
        surfaceEffect.transform.position = hit.point;
        surfaceEffect.transform.forward = hit.normal;
        surfaceEffect.Emit(1);
    }
    private void CmdOnGunFired()
    {
        StartCoroutine(Shooting());
    }
    private void OnWeaponSwitch()
    {

    }

    private IEnumerator Shooting()
    {
        while (ShootingHeld)
        {
            CmdShooting();
            yield return null;
        }
    }

    private IEnumerator Reload()
    {       
        gun.reload = true;
        float timeElapsed = 0;
        do 
        {
            timeElapsed += Time.fixedDeltaTime;
            gun.StartingAmmo++;
            gun.StartingAmmo = Mathf.Clamp(gun.StartingAmmo, 0, gun.AmmoCapacity);          
            yield return new WaitForSeconds(gun.ReloadTime / gun.AmmoCapacity);
        }        
        while (gun.StartingAmmo < gun.AmmoCapacity);
        gun.reload = false;
    }

    #region Server Command
    
    [Command]
    private void CmdUpdateReload()
    {
        RpcReloading();
    }

    [Command]
    private void CmdShooting()
    {
        RpcShoot();
    }
    
    #endregion
}
