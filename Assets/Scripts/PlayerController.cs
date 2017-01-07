using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private Animator acon;
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider2D;
    private StageManager stage;
    private bool isGround;
    private bool horiJumpFlag;
    private bool jumpMoveFlag;
    private float jumpButtonFrame;
    private float jumpFrame;
    private float horiFrame;
    private float idolFrame;
    private float moveSpeedChange;
    private const float m_centerY = 0.32f;

    public LayerMask groundLayer = -1;
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
        if (groundLayer == -1)
            Debug.Log("groundLayerが設定されていません！");

        acon = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        stage = GameObject.Find("StageManager").GetComponent<StageManager>();

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
        Jump();
        Move();
    }

    void FixedUpdate()
    {
        Vector2 pos = transform.position;
        Vector2 groundCheck = new Vector2(pos.x, pos.y - (m_centerY * transform.localScale.y));
        Vector2 groundArea = new Vector2(boxCollider2D.size.x * 0.48f, 0.08f);

        isGround = Physics2D.OverlapArea(groundCheck + groundArea, groundCheck - groundArea, groundLayer);
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

        if (jumpFrame > 0)
            jumpFrame--;

        if (isGround)
        {
            jumpMoveFlag = false;
            horiJumpFlag = false;
            moveSpeedChange = 0;

            if (horiFrame > 25)
                horiJumpFlag = true;

            if (jumpFrame == 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);
                acon.SetBool("Jump", false);

                if (jumpButtonFrame == 8 && !acon.GetBool("Jump"))
                {
                    jumpFrame = 10;
                    acon.SetBool("Jump", true);
                    rb.AddForce(Vector2.up * Bjump / 60, ForceMode2D.Impulse);
                }
                else if (jumpButtonFrame < 8 && isJumpUp && !acon.GetBool("Jump"))
                {
                    jumpFrame = 10;
                    acon.SetBool("Jump", true);
                    rb.AddForce(Vector2.up * Sjump / 60, ForceMode2D.Impulse);
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

            if (horiFrame < 25)
                moveSpeedChange = speed / 2;
        }
        else
        {
            if (!horiJumpFlag)
                horiFrame = 0;

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
            horiFrame = 0;
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
            moveSpeedChange = speed / 1.5f;

        rb.velocity = new Vector2(h * (speed - moveSpeedChange), rb.velocity.y);
    }

    void OnTriggerStay2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "AreaChange")
        {
            string[] stas = collider.gameObject.name.Split(':');
            float chipX = stage.chipSizeX;
            float chipY = stage.chipSizeY;

            stage.StageInit(stas[0]);

            if (stas[1] != "-1" && stas[2] != "-1")
                stage.GetPlayer.transform.position = new Vector2(chipX * int.Parse(stas[1]) + chipX / 2, chipY * int.Parse(stas[2]) + chipY / 2);
        }
    }
}
