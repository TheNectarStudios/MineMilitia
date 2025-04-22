using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Optional: Disable camera follow or extra logic on non-local players
        if (!IsOwner)
        {
            // Example: disable a camera follow script
            // GetComponent<PlayerCameraFollow>()?.enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return; // Only local player processes input

        // Get WASD / Arrow key input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        Vector2 movement = moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }
}
