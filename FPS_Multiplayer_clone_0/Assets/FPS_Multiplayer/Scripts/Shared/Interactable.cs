using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System;

public class Interactable : NetworkBehaviour, Damageable
{
    /*
    public enum AIState
    {
        Idle,
        Attack,
    }
    */
    public Transform TurretPivot;
    public Transform TurretAimPoint;

    public float AimRotationSharpness = 5f;
    public float LookAtRotationSharpness = 2.5f;
    public float DetectionFireDelay = 1f;
    public float AimingTransitionBlendTime = 1f;
    
    const string k_AnimOnDamagedParameter = "OnDamaged";

    //[SerializeField ] GameObject bulletPrefab;
    [SerializeField ] Animator animationController;
    //public AIState AiState { get; private set; }

    Quaternion m_RotationWeaponForwardToPivot;
    float m_TimeStartedDetection;
    float m_TimeLostDetection;

    Quaternion m_PreviousPivotAimingRotation;
    Quaternion m_PivotAimingRotation;

    [SyncVar]
    [SerializeField] Transform _Target;

    bool didFire = false;
    public bool rotating = false;

    [SyncVar]
    public float Health;

    [SyncVar]
    public float Maxhealth = 100f;

    [Tooltip("Image component displaying health left")]
    public Image HealthBarImage;

    private void OnEnable()
    {
        Health = Maxhealth;

        // Start with idle
        //AiState = AIState.Idle;

        m_TimeStartedDetection = Mathf.NegativeInfinity;
        m_PreviousPivotAimingRotation = TurretPivot.rotation;
    }
    private void LateUpdate()
    {
        UpdateTurretAiming();
        //UpdateCurrentAiState();
    }

    //private void UpdateCurrentAiState()
    //{
    //    // Handle logic 
    //    switch (AiState)
    //    {
    //        case AIState.Attack:
    //            bool mustShoot = Time.time > m_TimeStartedDetection + DetectionFireDelay;
    //            // Calculate the desired rotation of our turret (aim at target)
    //            Vector3 directionToTarget =
    //                (_Target.position - TurretAimPoint.position).normalized;
    //            Quaternion offsettedTargetRotation =
    //                Quaternion.LookRotation(directionToTarget) * m_RotationWeaponForwardToPivot;
    //            m_PivotAimingRotation = Quaternion.Slerp(m_PreviousPivotAimingRotation, offsettedTargetRotation,
    //                (mustShoot ? AimRotationSharpness : LookAtRotationSharpness) * Time.deltaTime);

    //            // shoot
    //            if (mustShoot)
    //            {
    //                Vector3 correctedDirectionToTarget =
    //                    (m_PivotAimingRotation * Quaternion.Inverse(m_RotationWeaponForwardToPivot)) *
    //                    Vector3.forward;

    //                TryAttack(TurretAimPoint.position + correctedDirectionToTarget);
    //            }

    //            break;
    //    }
    //}

    private void UpdateTurretAiming()
    {
        if(_Target == null) { return; }
        if (rotating) { return; }
        StartCoroutine(RotateTurret());
       
    }
    private IEnumerator RotateTurret()
    {
        rotating = true;
        Vector3 target = _Target.position;
        Vector3 _Direction = (target - TurretAimPoint.position).normalized;
        Quaternion currentrot = TurretPivot.rotation;
        Quaternion offsettedTargetRotation = Quaternion.LookRotation(_Direction);
        float elapsedTime = 0;
        do
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / LookAtRotationSharpness;
            TurretPivot.rotation = Quaternion.Slerp(currentrot, offsettedTargetRotation, normalizedTime);
            yield return null;
        } while (elapsedTime < LookAtRotationSharpness);
        rotating = false;
    }
    void OnTriggerEnter(Collider other)
    {
        bool enemy = other.transform.CompareTag("Player");
        if (enemy)
        {
            _Target = other.transform;
        }
        
    }
    void OnTriggerExit(Collider other)
    {
        _Target = null;
    }
    private void TryAttack(Vector3 attackDirection)
    {
        Vector3 weaponForward = (attackDirection - TurretAimPoint.position).normalized;

        if (didFire) { return; }
        
    }
    void OnDamaged()
    {
        animationController.SetTrigger(k_AnimOnDamagedParameter);
    }

    [Command(requiresAuthority = false)]
    public void CmdDamage(float damgeValue)
    {
       
        Health -= damgeValue;
        SetHpBar();
        OnDamaged();
        if (Health <= 0)
        {
            Debug.Log("Dead");
            DestoryNpc();
        }
    }
    void SetHpBar()
    {
        // update health bar value
        HealthBarImage.fillAmount = Health / Maxhealth;
    }

   
    #region Server
    [Server]
    private void DestoryNpc()
    {
        NetworkServer.Destroy(gameObject);
    }
    #endregion

}
