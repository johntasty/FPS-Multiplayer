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
    private float timeSinceLastShot;
    public bool ShootingHeld = false;
    private bool CanShoot() => !gun.reload && timeSinceLastShot > 1f / (gun.FireRate / 60f);
    private void OnFire(InputValue value)
    {
        ShootingHeld = value.isPressed;
        if (ShootingHeld)
        {
            StartCoroutine(Shooting());
        }        
        if(gun.StartingAmmo <= 0)
        {
            Reloading();
        }

    }
    private void OnReload()
    {
        Reloading();
    }
    public void Shoot()
    {
        
        if(gun.StartingAmmo > 0)
        {
            if (CanShoot())
            {
                if(Physics.Raycast(muzzle.transform.position, muzzle.transform.forward, out RaycastHit hit, gun.MaxRange))
                {
                    Damageable interactable = hit.transform.GetComponent<Damageable>();
                    interactable?.CmdDamage(gun.Damage);
                }
                gun.StartingAmmo--;
                timeSinceLastShot = 0;
                OnGunFired();
            }
        }
    }
    public void UpdateShotTimer()
    {
        timeSinceLastShot += Time.deltaTime;
    }
    public void Reloading()
    {
        if (!gun.reload)
        {
            StartCoroutine(Reload());
        }
    }
    private void OnGunFired()
    {
        
    }
    private IEnumerator Shooting()
    {
        while (ShootingHeld)
        {
            Shoot();
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
}
