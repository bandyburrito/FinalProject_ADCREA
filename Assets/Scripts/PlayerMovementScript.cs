using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public SpriteRenderer playerSpriteRenderer;

    private Rigidbody2D _rb;
    private Vector2 _movement;

    // The inspector value is the permanent baseline; movement-speed upgrades stack onto
    // this multiplier during a run, and a new run snaps it back to 1.
    private float _baseMoveSpeed;
    private float _speedMultiplier = 1f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _baseMoveSpeed = moveSpeed;
        // Top-down: wall collisions must never spin the player, and interpolation smooths
        // the visible motion between physics steps.
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    /// <summary>Compounds a percentage move-speed upgrade (10 = +10%) for the rest of the run.</summary>
    public void ApplyMoveSpeedPercentUpgrade(float percent)
    {
        _speedMultiplier *= 1f + percent / 100f;
        // Never let stacked penalties stall the player completely.
        _speedMultiplier = Mathf.Max(_speedMultiplier, 0.1f);
        moveSpeed = _baseMoveSpeed * _speedMultiplier;
    }

    /// <summary>Roguelike death rule: upgrades are gone, so speed returns to the baseline.</summary>
    public void ResetForNewRun()
    {
        _speedMultiplier = 1f;
        moveSpeed = _baseMoveSpeed;
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
