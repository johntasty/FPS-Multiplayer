using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
public class GunSway : NetworkBehaviour
{
    private Vector2 aimControll;
  
    [Header("Gun sway settings")]
    [SerializeField] Transform gun;
    [SerializeField] private float speed;
    [SerializeField] private float sensitivityMu;

    private void OnLook(InputValue value)
    {
        aimControll = value.Get<Vector2>();
    }
    //adds some movement tothe gun when moved
    public void AimGun()
    {
        //runs contantly on the player, take the mouseposition at a point and rotates left and right
        float mouseX = aimControll.x * sensitivityMu;
        float mouseY = aimControll.y * sensitivityMu;
        mouseY = Mathf.Clamp(mouseY, -10, 10);
        Quaternion rotationX = Quaternion.AngleAxis(-mouseY, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(mouseX, Vector3.up);

        Quaternion targetRot = rotationX * rotationY;

        gun.localRotation = Quaternion.Slerp(gun.localRotation, targetRot, speed * Time.deltaTime);
    }
    
}
