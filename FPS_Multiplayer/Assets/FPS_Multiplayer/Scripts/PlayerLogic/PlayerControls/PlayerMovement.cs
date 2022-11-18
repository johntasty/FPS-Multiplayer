using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using Cinemachine;
using TMPro;
public class PlayerMovement : NetworkBehaviour
{
    //movement variables
    private CharacterController _Controller;
    private Vector2 move;
    private float gravity = 9.8f;
    private float vSpeed = 0;
    private float jumpSpeed = 4f;
    public Vector2 _CameraRot;
    //player speed
    [SerializeField] float speed;
    //player rotation speed
    private float _RotationSpeed = 10f;
    private float RotSet = 0f;
    private float RotSety = 0f;

    //camera setttings
    [SerializeField] float mouseSensitivity;
    [SerializeField] CinemachineVirtualCamera _CameraHolder = null;
    [SerializeField] Transform _CameraParent = null;
    [SerializeField] Transform FocusCamera = null;
    private Cinemachine3rdPersonFollow follower;

    //bullets
    [Header("Bullets Canvas")]
    [SerializeField] TMP_Text bulletCount;
    //gun settings
    private GunSway gunSettings;
    private GunController ShootLogic;
    [SerializeField] Transform TargetAiming;

    //animators
    [SerializeField] Animator animationsController;
    [SerializeField] Animator LocomotionAnimator;

    private int speedX;
    private int speedY;

    float TargetSpeed = 3.5f;
    [SyncVar]
    public Vector2 currentVelocity;

    public float BlendSpeed = 1;
    private void Awake()
    {
        speedX = Animator.StringToHash("SpeedX");
        speedY = Animator.StringToHash("SpeedY");
        gunSettings = transform.GetComponent<GunSway>();
        ShootLogic = transform.GetComponent<GunController>();
        ShootLogic.SetInitialData();
    }
    public override void OnStartAuthority()
    {
        
        if (!isOwned) { return; }
        //enable script when loaded
        enabled = true;        
        
        //set each connected player with thir own camera
        _Controller = transform.GetComponent<CharacterController>();
        _CameraHolder.gameObject.SetActive(true);
        follower = _CameraHolder.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        Camera.main.transform.SetParent(_CameraParent.transform);        
        Camera.main.transform.localPosition = new Vector3(0, 0, 0);        
        Cursor.lockState = CursorLockMode.Locked;
        //compoments to update
        transform.GetComponent<PlayerInput>().enabled = true;      

    }
    #region InputActions
    //actions can be found in the input map in the inspectors
    private void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();        
    }
    private void OnLook(InputValue value)
    {
        _CameraRot = value.Get<Vector2>();
        
    }
    private void OnZoom(InputValue value)
    {
        
        if (value.isPressed)
        {
          
            animationsController.Play("SightAim");

        }
        else
        {

            animationsController.Play("SightOut");
           
        }
    }

    private void OnJump(InputValue value)
    {
       
        if (_Controller.isGrounded)
        {
           
            vSpeed = jumpSpeed;
           
        }
    }
    #endregion

    //character controller 
  
    private void MovementController()
    {      
        //moves forward/backwards etc.. based on transforms forward direction
        Vector3 movement = transform.right * move.x + transform.forward * move.y;
        float movementSpeed = currentVelocity.magnitude;

        vSpeed -= gravity * Time.deltaTime;
        movement.y = vSpeed;

        _Controller.Move(movement * movementSpeed * Time.fixedDeltaTime);

        currentVelocity.x = Mathf.Lerp(currentVelocity.x, move.x * TargetSpeed, BlendSpeed * Time.fixedDeltaTime);
        currentVelocity.y = Mathf.Lerp(currentVelocity.y, move.y * TargetSpeed, BlendSpeed * Time.fixedDeltaTime);

        LocomotionAnimator.SetFloat(speedX, currentVelocity.x);
        LocomotionAnimator.SetFloat(speedY, currentVelocity.y);
       
    }
    //camera rotations 
    private void CameraMovement()
    {
        RotSet += _CameraRot.x * mouseSensitivity * Time.deltaTime;
        RotSety -= _CameraRot.y * mouseSensitivity * Time.deltaTime;

        RotSety = Mathf.Clamp(RotSety, -90f, 90f);

        Quaternion movePos = Quaternion.AngleAxis(RotSet, Vector3.up);
        //rotating the camera up/down clamped angle for shooter
        Quaternion _CamRotation = Quaternion.AngleAxis(RotSety, Vector3.right);
        _CameraParent.transform.localRotation = _CamRotation; 
        //slowly rotate the player towards the movement direction
        transform.rotation = Quaternion.Slerp(transform.rotation, movePos, _RotationSpeed * Time.deltaTime);
    }
    
    private void FixedUpdate()
    {
        if (!isOwned) { return; }
        MovementController();

    }
    //camera and timers are in set in lateUpdate for smoothermovement
    private void LateUpdate()
    {
        if (!isOwned) { return; }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            TargetSpeed = 4;
        }
        else { TargetSpeed = 3.5f; }
        
        FocusCamera.position = ShootLogic.CameraFocusing.position;
        ShootLogic.UpdateShotTimer(Time.deltaTime);        
        gunSettings.AimGun();
        ShootLogic.Recoil();
        UpdateBulets();
        CameraMovement();

    }
    private void UpdateBulets()
    {
        Vector2 ammo = ShootLogic.GetGunAmmo();
        bulletCount.text = ammo.x.ToString() + " / " + ammo.y.ToString();
    }
    #region ServerCommand
    [Command(requiresAuthority = false)]

    private void CmdSynceAnimationState()
    {
        RpcSyncVelocity();
    }
    [ClientRpc(includeOwner = true)]
    private void RpcSyncVelocity()
    {
        
    }
    #endregion

}
