using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private Animator acon;
    private Rigidbody2D rb;
    private bool isGround;
    private bool horiJumpFlag;
    private bool jumpMoveFlag;
    private float jumpButtonFrame;
    private float jumpFrame;
    private float horiFrame;
    private float idolFrame;
    private float moveSpeedChange;

    public LayerMask groundLayer;
    public float speed = -1;
    public float Sjump = -1;
    public float Bjump = -1;

	void Start ()
    {
        if (speed == -1)
            Debug.Log("speedが設定されていません！");
        if (Sjump == -1)
            Debug.Log("Sjumpが設定されていません！");
        if (Bjump == -1)
            Debug.Log("Bjumpが設定されていません！");

        acon = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        isGround = false;
        horiJumpFlag = false;
        jumpMoveFlag = false;
        jumpButtonFrame = 0;
        jumpFrame = 0;
        moveSpeedChange = 0;
        idolFrame = 0;
	}
	
	void Update ()
    {
        IsGround();
        Jump();
        Move();
    }

    private void Jump()
    {
        float h = Input.GetAxis("Horizontal");

        bool isJumpDown = Input.GetButtonDown("Jump");
        bool isJump = Input.GetButton("Jump");
        bool isJumpUp = Input.GetButtonUp("Jump");

        if (isJumpDown && jumpButtonFrame == 0)
            jumpButtonFrame++;

        if (isJump && jumpButtonFrame > 0)
            jumpButtonFrame++;

        if (isGround)
        {
            jumpMoveFlag = false;
            horiJumpFlag = false;
            moveSpeedChange = 0;

            if (jumpFrame == 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);
                acon.SetBool("Jump", false);

                if (jumpButtonFrame == 7 && !acon.GetBool("Jump"))
                {
                    jumpFrame = 10;
                    acon.SetBool("Jump", true);
                    rb.AddForce(Vector2.up * Bjump * Time.deltaTime, ForceMode2D.Impulse);

                    if (horiFrame > 15)
                    {
                        horiJumpFlag = true;
                    }
                }
                else if (jumpButtonFrame < 7 && isJumpUp && !acon.GetBool("Jump"))
                {
                    jumpFrame = 10;
                    acon.SetBool("Jump", true);
                    rb.AddForce(Vector2.up * Sjump * Time.deltaTime, ForceMode2D.Impulse);

                    if (horiFrame > 15)
                    {
                        horiJumpFlag = true;
                    }
                }
            }
        }
        else
        {
            if (acon.GetBool("Jump") && !horiJumpFlag)
            {
                if (rb.velocity.y > -1f && rb.velocity.y < 1f)
                {
                    jumpMoveFlag = true;
                }
            }
        }

        if (jumpFrame > 0)
            jumpFrame--;

        if (isJumpUp)
            jumpButtonFrame = 0;
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");


        if (isGround)
        {
            moveSpeedChange = 0;
        }
        else
        {
            moveSpeedChange = speed / 3;
        }
      
        if (h < 0)
        {
            acon.SetBool("Squat", false);
            acon.SetBool("Idol", false);
            acon.SetBool("Left", true);
            acon.SetBool("Right", false);
            horiFrame++;
        }
        else if (h > 0)
        {
            acon.SetBool("Squat", false);
            acon.SetBool("Idol", false);
            acon.SetBool("Left", false);
            acon.SetBool("Right", true);
            horiFrame++;
        }
        else
        {
            horiFrame = 0;
        }

        if (v < 0)
        {
            acon.SetBool("Squat", true);
            moveSpeedChange = speed / 1.5f;
        }
        else if (v > 0)
        {

        }

        if (h == 0 && v == 0)
        {
            if (idolFrame > 10)
            {
                idolFrame = 0;
                acon.SetBool("Squat", false);
                acon.SetBool("Idol", true);
                acon.SetBool("Left", false);
                acon.SetBool("Right", false);
            }

            idolFrame++;
        }

        if (!horiJumpFlag && !jumpMoveFlag && !isGround)
            moveSpeedChange = speed / 1.2f;

        transform.Translate(h * (speed - moveSpeedChange) * Time.deltaTime, 0, 0);
    }

    private void IsGround()
    {
        isGround = Physics2D.Linecast(transform.position, transform.position - transform.up * 0.37f, groundLayer);
    }
}
