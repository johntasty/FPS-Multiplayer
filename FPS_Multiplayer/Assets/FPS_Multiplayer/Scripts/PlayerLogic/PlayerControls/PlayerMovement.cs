using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using Cinemachine;

public class PlayerMovement : NetworkBehaviour
{
    //movement variables
    private CharacterController _Controller;
    private Vector2 move;
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
    private Cinemachine3rdPersonFollow follower;

    //gun settings
    private GunSway gunSettings;
    private GunController ShootLogic;
    //[SerializeField] Transform gun;

    //animators
    [SerializeField] Animator animationsController;

    private void Awake()
    {
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
    #endregion

    //character controller 
    private void MovementController()
    {      
        //moves forward/backwards etc.. based on transforms forward direction
        Vector3 movement = transform.right * move.x + transform.forward * move.y;   

        _Controller.Move(movement * speed * Time.fixedDeltaTime);
               

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
        ShootLogic.UpdateShotTimer(Time.deltaTime);
        gunSettings.AimGun();
        ShootLogic.Recoil();
        CameraMovement();
    }
}
