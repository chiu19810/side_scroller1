using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private Animator acon;
    private Rigidbody2D rb;
    private bool isGround;

    public LayerMask groundLayer;
    public float speed = -1;
    public float jump = -1;

	void Start ()
    {
        if (speed == -1)
        {
            Debug.Log("speedが設定されていません！");
        }

        if (jump == -1)
        {
            Debug.Log("jumpが設定されていません！");
        }

        acon = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
	}
	
	void Update ()
    {
        Move();
	}

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        bool isJump = Input.GetButtonDown("Jump");

        isGround = Physics2D.Linecast(transform.position + transform.up * 1, transform.position - transform.up * 0.5f, groundLayer);
        rb.velocity = new Vector2(h * speed, rb.velocity.y);

        if (h < 0)
        {
            acon.SetBool("Left", true);
            acon.SetBool("Right", false);
        }
        else if (h > 0)
        {
            acon.SetBool("Left", false);
            acon.SetBool("Right", true);
        }
        else
        {
            acon.SetBool("Left", false);
            acon.SetBool("Right", false);
            acon.SetBool("Jump", false);
        }

        if (isJump == true && isGround)
        {
            rb.AddForce(Vector2.up * jump);
            acon.SetBool("Jump", true);
        }
    }
}
