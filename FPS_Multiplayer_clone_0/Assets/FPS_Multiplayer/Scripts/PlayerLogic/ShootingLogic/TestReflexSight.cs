using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class TestReflexSight : MonoBehaviour
{
    Vector2 movement;
    float TargetSpeed = 3.5f;
    public float speedMove = 1;
    public float BlendSpeed = 1;
    private Animator controller;
    private CharacterController player;

    private int speedX;
    private int speedY;

    private float _RotationSpeed = 10f;
    private float RotSet = 0f;
    private float RotSety = 0f;
    public Vector2 _CameraRot;
    //camera setttings
    [SerializeField] float mouseSensitivity;
    [SerializeField] Transform _CameraParent = null;
    [SerializeField] Animator controlle = null;

    public float CurrentVelocityX;
    public float CurrentVelocityY;
    Vector2 currentVelocity;
    private void Start()
    {
        player = transform.GetComponent<CharacterController>();       
        speedX = Animator.StringToHash("SpeedX");
        speedY = Animator.StringToHash("SpeedY");
    }
    private void OnMove(InputValue value)
    {
        movement = value.Get<Vector2>();
       
    }
    private void OnLook(InputValue value)
    {
        _CameraRot = value.Get<Vector2>();

    }
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
    private void moveing()
    {
        Vector3 movementVec = transform.right * movement.x + transform.forward * movement.y;
        float movemebtSpeent = currentVelocity.magnitude;
        CurrentVelocityX = movemebtSpeent;
        
        player.Move(movementVec * movemebtSpeent * Time.fixedDeltaTime);

        currentVelocity.x = Mathf.Lerp(currentVelocity.x, movement.x * TargetSpeed, BlendSpeed * Time.fixedDeltaTime);
        currentVelocity.y = Mathf.Lerp(currentVelocity.y, movement.y * TargetSpeed, BlendSpeed * Time.fixedDeltaTime);

        controlle.SetFloat(speedX, currentVelocity.x);
        controlle.SetFloat(speedY, currentVelocity.y);
    }
    private void Update()
    {
       
        if (Input.GetKey(KeyCode.LeftShift))
        {
            TargetSpeed = 4;
        }
        else { TargetSpeed = 3.5f; }
        CameraMovement();


    }
    private void FixedUpdate()
    {
        moveing();
    }
    
}
