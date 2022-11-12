using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
public class PlayerMovement : NetworkBehaviour
{
    private CharacterController _Controller;
    private Vector2 move;
    private Vector2 _Rotation;
    [SerializeField] float speed;
    [SerializeField] float mouseSensitivity;
    [SerializeField] Transform _CameraHolder = null;

    private float xRotSet = 0;
    public override void OnStartAuthority()
    {
        if (!isOwned) { return; }
        transform.GetComponent<PlayerMovement>().enabled = true;
        _Controller = transform.GetComponent<CharacterController>();
        
        //Camera.main.transform.localPosition = new Vector3(0, 0, 0);

    }
    private void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();        
    }
    private void OnLook(InputValue value)
    {
        _Rotation = value.Get<Vector2>();
    }
    private void MovementController()
    {
        float xRot = _Rotation.x * mouseSensitivity * Time.fixedDeltaTime;
        float yRot = _Rotation.y * mouseSensitivity * Time.fixedDeltaTime;

        xRotSet -= yRot;
        
        Vector3 movePos = new Vector3(move.x * speed, 0, move.y *speed);
        _Controller.Move(movePos * Time.fixedDeltaTime);
    }
    private void FixedUpdate()
    {
        MovementController();
    }
}
