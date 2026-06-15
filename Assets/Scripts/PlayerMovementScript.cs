using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public SpriteRenderer playerSpriteRenderer;

    private Rigidbody2D _rb;
    private Vector2 _movement;

    private float _baseMoveSpeed;
    private float _speedMultiplier = 1f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _baseMoveSpeed = moveSpeed;

        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void ApplyMoveSpeedPercentUpgrade(float percent)
    {
        _speedMultiplier *= 1f + percent / 100f;

        _speedMultiplier = Mathf.Max(_speedMultiplier, 0.1f);
        moveSpeed = _baseMoveSpeed * _speedMultiplier;
    }

    public void ResetForNewRun()
    {
        _speedMultiplier = 1f;
        moveSpeed = _baseMoveSpeed;
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        _movement = Vector2.ClampMagnitude(new Vector2(moveX, moveY), 1f);
        TurnPlayerSprite(moveX);
    }

    void FixedUpdate()
    {
        _rb.linearVelocity = _movement * moveSpeed;
    }

    private void TurnPlayerSprite(float horizontalInput)
    {
        if (horizontalInput > 0)
        {
            playerSpriteRenderer.flipX = false;
        }
        else if (horizontalInput < 0)
        {
            playerSpriteRenderer.flipX = true;
        }
    }
}
