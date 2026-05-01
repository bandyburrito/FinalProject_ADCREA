using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public SpriteRenderer playerSpriteRenderer;


    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        PlayerMovement();
        TurnPlayerSprite();
    }

    private void TurnPlayerSprite()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        if (horizontalInput > 0)
        {
            playerSpriteRenderer.flipX = false; // Facing Right
        }
        else if (horizontalInput < 0)
        {
            playerSpriteRenderer.flipX = true; // Facing Left
        }
    }

    
    private void PlayerMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        Vector2 movement = new Vector2(moveX, moveY).normalized;
        transform.Translate(movement * moveSpeed * Time.deltaTime);
    }
}
