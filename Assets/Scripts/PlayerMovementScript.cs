using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public SpriteRenderer playerSpriteRenderer;

    private Rigidbody2D _rb;
    private Vector2 _movement;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        // Top-down: wall collisions must never spin the player, and interpolation smooths
        // the visible motion between physics steps.
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // Input is polled per-frame here; movement is applied in FixedUpdate.
    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        _movement = Vector2.ClampMagnitude(new Vector2(moveX, moveY), 1f);
        TurnPlayerSprite(moveX);
    }

    // Physics-based movement: the collider stops cleanly against walls instead of teleporting
    // into them (transform.Translate ignored collisions, which caused the clipping/jitter).
    void FixedUpdate()
    {
        _rb.linearVelocity = _movement * moveSpeed;
    }

    private void TurnPlayerSprite(float horizontalInput)
    {
        if (horizontalInput > 0)
        {
            playerSpriteRenderer.flipX = false; // Facing Right
        }
        else if (horizontalInput < 0)
        {
            playerSpriteRenderer.flipX = true; // Facing Left
        }
    }
}
