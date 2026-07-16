using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool isSprinting = false;

    public bool IsSprinting => isSprinting;

    public float speed = 3f;
    public float walkSpeed = 3f;
    public float gravity = -9.8f;
    public float jumpHeight = 3f;
    public float sprintSpeed = 5f;
    
    private Vector3 horizontalVelocity;
    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void ProcessMove(Vector2 input)
    {
        var s = UnderwaterEnvironment.Instance?.Settings;
        float accel  = s ? s.acceleration : 3f;
        float g      = s ? s.gravity : gravity;
        float walk   = s ? s.walkSpeed : walkSpeed;
        float sprint = s ? s.sprintSpeed : sprintSpeed;
        float curSpeed = isSprinting ? sprint : walk;

        Vector3 dir = transform.TransformDirection(new Vector3(input.x, 0, input.y));
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, dir * curSpeed, accel * Time.deltaTime);
        controller.Move(horizontalVelocity * Time.deltaTime);

        playerVelocity.y += g * Time.deltaTime;
        if (isGrounded && playerVelocity.y < 0) playerVelocity.y = -2f;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    public void Jump()
    {
        if (!isGrounded) return;
        var s = UnderwaterEnvironment.Instance?.Settings;
        float g = s ? s.gravity : gravity;
        playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * g);
    }

    public void Sprint(bool sprinting)
    {
        isSprinting = sprinting;
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = controller.isGrounded;
    }
}
