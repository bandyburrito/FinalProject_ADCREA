using UnityEngine;

public class PlayerMovementScript : MonoBehaviour
{
    public float moveSpeed = 5f;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float moveY = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

        transform.Translate(moveX, moveY, 0);



    }
}
