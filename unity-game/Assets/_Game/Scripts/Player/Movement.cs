using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    public static Movement instance;

    [SerializeField] float speed;
    [SerializeField] float DashForce;
    [SerializeField] ParticleSystem dust;
    
    Camera cam;
    private Rigidbody2D rb;
    private Vector2 moveDir;
    private SpriteRenderer playerVisual;
    private Animator anim;

    public bool canMove;
    private static readonly int Speed = Animator.StringToHash("Speed");

    void Awake()
    {
        if (instance == null) instance = this;

        moveDir = new Vector2();
        rb = GetComponent<Rigidbody2D>();
        playerVisual = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        cam = Camera.main;
    }

    private void Start()
    {
        EnemyBehaviour.ended = false;
    }

    void Update()
    {
#if !UNITY_ANDROID && !UNITY_IOS
        if (canMove)
        {
            moveDir.x = Input.GetAxisRaw("Horizontal");
            moveDir.y = Input.GetAxisRaw("Vertical");

            if (moveDir.magnitude != 0)
            {
                CreateDust();
            }

            anim.SetFloat(Speed, moveDir.SqrMagnitude());

            Vector3 mousePosition = Input.mousePosition;
            mousePosition = cam.ScreenToWorldPoint(mousePosition);

            if (mousePosition.x > transform.position.x)
                playerVisual.flipX = false;
            else if (mousePosition.x < transform.position.x)
                playerVisual.flipX = true;
        }
#endif
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveDir * (speed * Time.fixedDeltaTime));
    }

    void CreateDust()
    {
        if (!dust.isPlaying)
            dust.Play();
    }

    public void EnableMovement(bool status)
    {
        canMove = status;
    }

    public bool IsMoving()
    {
        return moveDir.magnitude != 0;
    }
    
    // Only triggered when the Joystick is active. That happens on mobile platforms.
    public void OnJoystickMovement(InputAction.CallbackContext context)
    { 
        moveDir = context.ReadValue<Vector2>();

        moveDir *= 1.18f;
       
        if (moveDir.magnitude != 0)
        {
           CreateDust();
        }

        anim.SetFloat(Speed, moveDir.SqrMagnitude());

        playerVisual.flipX = !(moveDir.x >= 0);
    }
}
