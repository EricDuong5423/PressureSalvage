using System;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    private float xRotation = 0f;
    
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;

    private void Start()
    {
        cam = GetComponentInChildren<Camera>();
    }

    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;
        
        //Calculate camera rotation for looking up and down
        xRotation -= (mouseY * Time.deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        //Rotate camera
        cam.transform.localRotation = Quaternion.Euler(xRotation , 0f, 0f);
        //Rotate player
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSensitivity);
    }
}
