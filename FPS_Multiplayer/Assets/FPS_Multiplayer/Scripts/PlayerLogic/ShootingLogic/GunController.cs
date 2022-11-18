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
    [SerializeField] public Transform CameraFocusing;
    [SerializeField] Transform gunsParent;
    [SerializeField] List<GameObject> muzzleFlash = new List<GameObject>();

    //gun swapping index
    [SyncVar]
    private int WeaponIndex = 0;

    [Header("Impact effects, Set by name")]
    [SerializeField] List<ParticleSystem> hitEffects = new List<ParticleSystem>();
    [SerializeField] TrailRenderer bullerTracer;

    [Header("CrossHairs")]
    [SerializeField] Transform crossHairs;
    [SerializeField] GameObject hitMarker;

    [Header("Recoil Settings")]
    private RecoilSet recoilStat;
    [SerializeField] Transform recoilTransform;

    #region Client Callbacks
    public override void OnStartClient()
    {
        //inventory of weapons
        gunPrefabServer.Callback += OnWeaponAdded;

        // Process Synclist on Spawn
        
        for (int index = 0; index < gunPrefabServer.Count; index++)
        {           
            OnWeaponAdded(SyncList<uint>.Operation.OP_ADD, index, new uint(), gunPrefabServer[index]);
        }
    }
    public override void OnStopClient()
    {
        gunPrefabServer.Callback -= OnWeaponAdded;
    }
    #endregion

    #region Weapon Inventory
    // sync list example from mirror
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
        //the check for client is done due to the host checking the list when its noit needed        
        if (!isClientOnly) { return; }
        //get the game object based on network id to avoid any null 
        //game objects and desyncs
        GameObject serverGun = NetworkClient.spawned[ItemNew].gameObject;
        serverGun.transform.SetParent(gunsParent);
        serverGun.transform.localPosition = Vector3.zero;
        //set the muzzle particle spawn to the correct space using the mesh bounds
        // saves time in not having to add a specific point in each prefab for a muzzle
        Vector3 muzzlePos = serverGun.GetComponent<MeshFilter>().mesh.bounds.size;
        muzzle.localPosition = new Vector3(0f, muzzlePos.x, muzzlePos.y);
        CameraFocusing.localPosition = muzzle.localPosition;
        serverGun.transform.localPosition = Vector3.zero;  
        //list of all availabe guns to the player, to be used as inventory for the switching
        gunPrefabClient.Add(serverGun);

    }
    #endregion
    public void SetInitialData()
    {
        //set for intial data of primary weapon to be ready to fire
        //TODO start with holstered weapon
        gun = guns[WeaponIndex];
        gun.reload = false;
        WeaponSet(gun);
        //weapon spawning should be done locally, the clients will go through a list and spawn their weapons
              
        foreach (GunData gunPrefabs in guns)
        {
            //calculating muzzle position
            GameObject gunToAdd = Instantiate(gunPrefabs.GunPrefab, gunsParent);
            Vector3 muzzlePos = gunToAdd.GetComponent<MeshFilter>().mesh.bounds.size;       
            muzzle.localPosition = new Vector3(0f, muzzlePos.x, muzzlePos.y);
            CameraFocusing.localPosition = muzzle.localPosition;
            //setting the gun prefabs to the inventory
            gunToAdd.transform.localPosition = Vector3.zero;
            gunToAdd.name = gunPrefabs.GunName;
            gunPrefabClient.Add(gunToAdd);
            gunToAdd.SetActive(false);
            
        }
        gunPrefabClient[0].SetActive(true);
        if (!isOwned) { return; }
        //serverweapon spawn for our copies
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
    //recoil implementation with kickback
    //called on late upadte from the player movement
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
    //getter function
    public Vector2 GetGunAmmo()
    {
        Vector2 ammo = new Vector2(gun.StartingAmmo, gun.AmmoCapacity);
        return ammo;

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
    //list of active bullets to deactivate
    //TODO Object Pooling
    private List<Bullet> bullets = new List<Bullet>();
    private float maxLifeBullet = 1f;
    public Vector3 rayCastDestination { get; set; }
    #region SyncVars
    [SyncVar]
    public float timeSinceLastShot;
    //ShootingHeld is used since the new input system has no functionality for holding key down
    [SyncVar]
    public bool ShootingHeld = false;
    #endregion
    private bool muzzleFlashSet = false;

    public override void OnStartAuthority()
    {
        
        if (!isOwned) { return; }
        enabled = true;
        
    }
    
    //timerfor fire rate
    private bool CanShoot() => !gun.reload && timeSinceLastShot > 1f / (gun.FireRate / 60f);
    //input system functions
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
        //calls all copiesof user to reload, if not copies wont be able to fire
        CmdUpdateReload();
    }
   //shoot command wrapped in RPC to sync on all clients, not entirly sure if this is good practice, its my understanding of networking
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
    //bullet function for bullet drop and position in space
    Vector3 GetBuLletPos(Bullet bullet)
    {
        Vector3 gravity = Vector3.down * gun.bulletDrop;

        return (bullet.InitialPosition + (bullet.InitialVelocity * bullet.time) + (0.5f * gravity * bullet.time * bullet.time));

    }
    //runs in constantly in the player movmeent
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
    //creating physicall bullets in segments, this can be used to change the material of the bullet to anything
    //and create impact per bullet and not just a ray with visuals
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
            //hit mark effect based on surface type
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
    //creation of physicall bullets from the class and gun data
    Bullet CreateBullet(Vector3 initialPos, Vector3 initialVelocity)
    {
        Bullet bullet = new Bullet();
        bullet.InitialPosition = initialPos;
        bullet.InitialVelocity = initialVelocity;
        bullet.time = 0.0f;
        if(gun.BulletTracer != null)
        {
            bullet.tracer = Instantiate(gun.BulletTracer, initialPos, Quaternion.identity);
        }
        else
        {
            bullet.tracer = Instantiate(bullerTracer, initialPos, Quaternion.identity);
        }
       
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
    //hit effect is a decal from unity, can be changed to anything inside the player prefab under gun, a list of them cna be added and used based on surface type
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
        gun.reload = false;
        CmdWeaponSwitchSync();
    }
    //the routine that runsfor simulating a held down button
    private IEnumerator Shooting()
    {
        while (ShootingHeld)
        {
           
            CmdShooting();            
            yield return null;
        }
    }
    //reload slowly based on the gun stats reload time
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
    //hit marker like any fps of whereyou hit
    //TODO adjust colors and marker as it is hard to see
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
    [Command]
    private void CmdWeaponSwitchSync()
    {
        RpcWeaponSwitchAll();
    }

    [ClientRpc(includeOwner = true)]
    private void RpcWeaponSwitchAll()
    {
        WeaponSet(guns[WeaponIndex]);
        gun = guns[WeaponIndex];
        gunPrefabClient[WeaponIndex].SetActive(false);
        WeaponIndex = (WeaponIndex + 1) % guns.Count;
        gunPrefabClient[WeaponIndex].SetActive(true);
    }
    #endregion
}
