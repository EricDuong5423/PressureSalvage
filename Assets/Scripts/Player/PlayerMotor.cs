using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool isSprinting = false;

    public float speed = 3f;
    public float walkSpeed = 3f;
    public float gravity = -9.8f;
    public float jumpHeight = 3f;
    public float sprintSpeed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;
        controller.Move(transform.TransformDirection(moveDirection) * speed * Time.deltaTime);
        playerVelocity.y += gravity * Time.deltaTime;
        if(isGrounded && playerVelocity.y < 0) playerVelocity.y = -2f;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    public void Jump()
    {
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
        }
    }

    public void Sprint(bool sprinting)
    {
        isSprinting = sprinting;
        if (isSprinting) speed = sprintSpeed;
        else speed = walkSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = controller.isGrounded;
    }
}
