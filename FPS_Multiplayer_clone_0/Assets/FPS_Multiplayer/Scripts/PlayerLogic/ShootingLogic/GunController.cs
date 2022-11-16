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
    [SerializeField] List <GunData> guns = new List<GunData>();
    [SerializeField] public readonly SyncList<uint> gunPrefabServer = new SyncList<uint>();
    [SerializeField] List <GameObject> gunPrefabClient = new List<GameObject>();
    [SerializeField] Transform muzzle;
    [SerializeField] Transform gunsParent;
    [SerializeField] List<GameObject> muzzleFlash = new List<GameObject>();

    [Header("Impact effects, Set by name")]
    [SerializeField] List<ParticleSystem> hitEffects = new List<ParticleSystem>();
    [SerializeField] TrailRenderer bullerTracer;

    [Header("CrossHairs")]
    [SerializeField] Transform crossHairs;
    [SerializeField] GameObject hitMarker;

    [Header("Recoil Settings")]
    private RecoilSet recoilStat;
    [SerializeField] Transform recoilTransform;

    public override void OnStartClient()
    {
        gunPrefabServer.Callback += OnWeaponAdded;

        // Process initial SyncList payload
        
        for (int index = 0; index < gunPrefabServer.Count; index++)
        {           
            OnWeaponAdded(SyncList<uint>.Operation.OP_ADD, index, new uint(), gunPrefabServer[index]);
        }
    }
    public override void OnStopClient()
    {
        gunPrefabServer.Callback -= OnWeaponAdded;
    }
    private void OnWeaponAdded(SyncList<uint>.Operation op, int itemIndex, uint oldItem, uint newItem)
    {
        switch (op)
        {
            case SyncList<uint>.Operation.OP_ADD:
                // index is where it was added into the list
                // newItem is the new item
                SetUpClientGuns(newItem);
                break;
            case SyncList<uint>.Operation.OP_INSERT:
                // index is where it was inserted into the list
                // newItem is the new item
                break;
            case SyncList<uint>.Operation.OP_REMOVEAT:
                // index is where it was removed from the list
                // oldItem is the item that was removed
                break;
            case SyncList<uint>.Operation.OP_SET:
                // index is of the item that was changed
                // oldItem is the previous value for the item at the index
                // newItem is the new value for the item at the index
                break;
            case SyncList<uint>.Operation.OP_CLEAR:
                // list got cleared
                break;
        }
    }
    private void SetUpClientGuns(uint ItemNew)
    {
        Debug.Log("Check run");
        if (!isClientOnly) { return; }
        GameObject serverGun = NetworkClient.spawned[ItemNew].gameObject;
        serverGun.transform.SetParent(gunsParent);
        serverGun.transform.localPosition = Vector3.zero;
        //GameObject gunToAdd = Instantiate(serverGun, gunsParent);
        Vector3 muzzlePos = serverGun.GetComponent<MeshFilter>().mesh.bounds.size;
        muzzle.localPosition = new Vector3(0f, muzzlePos.x, muzzlePos.y);
        serverGun.transform.localPosition = Vector3.zero;        
        gunPrefabClient.Add(serverGun);

    }
    public void SetInitialData()
    {
        WeaponSet(gun);
        //weapon spawning should be done locally, the clients will go through a list and spawn their weapons
              
        foreach (GunData gunPrefabs in guns)
        {
            GameObject gunToAdd = Instantiate(gunPrefabs.GunPrefab, gunsParent);
            Vector3 muzzlePos = gunToAdd.GetComponent<MeshFilter>().mesh.bounds.size;       
            muzzle.localPosition = new Vector3(0f, muzzlePos.x, muzzlePos.y);
            gunToAdd.transform.localPosition = Vector3.zero;
            gunToAdd.name = gunPrefabs.GunName;
            gunPrefabClient.Add(gunToAdd);
                      
        }
        if (!isOwned) { return; }
        CmdSpawnWeapon();
    }

    //sets up gun data on weapon switch
    private void WeaponSet(GunData gunActive)
    {
        recoilStat = new RecoilSet();
        recoilStat.recoilX = gunActive.recoilStats.recoilX;
        recoilStat.recoilY = gunActive.recoilStats.recoilY;
        recoilStat.recoilZ = gunActive.recoilStats.recoilZ;
        recoilStat.speed = gunActive.recoilStats.speed;
        recoilStat.strenght = gunActive.recoilStats.strenght;
    }
    public void Recoil()
    {
        //recoil
        recoilStat.targetRotation = Vector3.Lerp(recoilStat.targetRotation, Vector3.zero, recoilStat.speed * Time.deltaTime); 
        //kickback
        recoilStat.targetKickBack = Vector3.Lerp(recoilStat.targetKickBack, Vector3.zero, (recoilStat.speed * 4 )* Time.deltaTime);     
        //recoil
        recoilStat.CurrentRotation = Vector3.Slerp(recoilStat.CurrentRotation, recoilStat.targetRotation, recoilStat.strenght * Time.deltaTime);
        //kickback
        recoilStat.CurrentKickBack = Vector3.Slerp(recoilStat.CurrentKickBack, recoilStat.targetKickBack, (recoilStat.strenght / 2) * Time.deltaTime);

        recoilTransform.localRotation = Quaternion.Euler(recoilStat.CurrentRotation);
        recoilTransform.localPosition = new Vector3(0f, recoilTransform.localPosition.y, recoilStat.CurrentKickBack.z);
    }
    void RecoilFire()
    {
        recoilStat.targetRotation += new Vector3(recoilStat.recoilX,
            UnityEngine.Random.Range(-recoilStat.recoilY, recoilStat.recoilY),
            UnityEngine.Random.Range(-recoilStat.recoilZ, recoilStat.recoilZ));
        recoilStat.targetKickBack += new Vector3(0f, 0f, UnityEngine.Random.Range(-1, 0));
    }

    //Recoil Settings
    class RecoilSet
    {
        public float recoilX { get; set; }
        public float recoilY { get; set; }
        public float recoilZ { get; set; }
        public float strenght { get; set; }
        public float speed { get; set; }

        public Vector3 CurrentRotation { get; set; }
        public Vector3 CurrentKickBack { get; set; }
        public Vector3 targetRotation { get;  set; }
        public Vector3 targetKickBack { get;  set; }

    }
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
                Vector3 _screenPos = Camera.main.ScreenToWorldPoint(crossHairs.position);
                Vector3 rayPoint = Camera.main.transform.forward;
                rayCastDestination = (rayPoint  * gun.MaxRange) + _screenPos;
                
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
            string surface = hit.transform.tag;
            HitEffect(surface, hit);
            bullet.tracer.transform.position = hit.point;
            bullet.time = maxLifeBullet;
            StartCoroutine(HitMarkerOn(hit));
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
        RecoilFire();
    }
    private void HitEffect(string surface, RaycastHit hit)
    {       
        rayCastDestination = hit.point;
        ParticleSystem surfaceEffect = hitEffects.Find(x => x.name == surface);
        if (surfaceEffect == null) surfaceEffect = hitEffects[0];
        surfaceEffect.transform.position = hit.point;
        surfaceEffect.transform.forward = hit.normal;
        surfaceEffect.Emit(1);
        //the dmg from the player should only done locally not on all clients
        if (!isOwned) { return; }
        Damageable interactable = hit.transform.GetComponent<Damageable>();
        interactable?.CmdDamage(gun.Damage);
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
    private IEnumerator HitMarkerOn(RaycastHit hit)
    {
        //hit marker animation
        hitMarker.SetActive(true);
        hitMarker.transform.position = Camera.main.WorldToScreenPoint(hit.point);       
        float timeElapsed = 0;
        do {
            timeElapsed += Time.deltaTime;
            float normalizedTime = timeElapsed / 0.1f;            
            hitMarker.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(new Vector2(40f, 40f), new Vector2(80f, 80f), normalizedTime);            
            yield return null;            
        } while (timeElapsed < 0.1f);
        hitMarker.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 40f);
        hitMarker.SetActive(false);
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
    [Command]
    private void CmdSpawnWeapon()
    {
       foreach(GameObject gunServer in gunPrefabClient)
        {
            NetworkServer.Spawn(gunServer);
            gunPrefabServer.Add(gunServer.GetComponent<NetworkIdentity>().netId);
        }
        
    }
    #endregion
}
