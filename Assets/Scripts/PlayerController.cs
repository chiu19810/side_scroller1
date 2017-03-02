using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator acon;
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider2D;
    private StageManager stage;
    private bool isGround;
    private bool horiJumpFlag;
    private bool jumpMoveFlag;
    private bool isMoveFlag;
    private int startCount;
    private float jumpButtonFrame;
    private float jumpFrame;
    private float horiFrame;
    private float idolFrame;
    private float moveSpeedChange;
    private const float m_centerY = 0.32f;
    private const float m_centerX = 0.48f;
    private const float horiCount = 20;
    private const int startConstCount = 20;

    public LayerMask groundLayer;
    public float speed;     // 5
    public float Sjump;     // 130
    public float Bjump;     // 220
    public float playerW;   // 0.64
    public float playerH;   // 0.64

    void Start()
    {
        init();
    }

    private void init()
    {
        acon = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        stage = GameObject.Find("StageManager").GetComponent<StageManager>();

        isGround = false;
        horiJumpFlag = false;
        jumpMoveFlag = false;
        isMoveFlag = true;
        startCount = startConstCount;

        jumpButtonFrame = 0;
        jumpFrame = 0;
        moveSpeedChange = 0;
        idolFrame = 0;
    }

    void Update()
    {
        if (isMoveFlag)
        {
            Time.timeScale = 1;
            Jump();
            Move();
        }
        else
        {
            Time.timeScale = 0;
        }
    }

    void FixedUpdate()
    {
        Vector2 pos = transform.position;
        Vector2 groundCheck = new Vector2(pos.x, pos.y - (m_centerY * transform.localScale.y));
        Vector2 groundArea = new Vector2(boxCollider2D.size.x * m_centerX, 0.08f);

        isGround = Physics2D.OverlapArea(groundCheck + groundArea, groundCheck - groundArea, groundLayer);

        if (!isGround)
        {
            float y = gameObject.transform.position.y;

            if (y < -5)
            {
                gameObject.transform.position = stage.getStartPos;
                rb.velocity = Vector2.zero;
            }
        }
    }

    private void Jump()
    {
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

            if (horiFrame > horiCount)
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

        if (stage.getMap == null)
            return;

        if (startCount > 0)
        {
            startCount--;
            return;
        }

        if (isGround)
        {
            moveSpeedChange = 0;

            if (horiFrame < horiCount)
                moveSpeedChange = speed / 2;
        }
        else
        {
            if (!horiJumpFlag)
                horiFrame = 0;

            moveSpeedChange = speed / 3;
        }

        string[,] map = stage.getMap;
        int mapX = stage.Data.map.mapSizeX;
        GameObject player = gameObject;
        float x = player.transform.position.x;
        float stageSizeW = stage.chipSizeX * mapX;

        if (h < 0)
        {
            acon.SetBool("Squat", false);
            acon.SetBool("Idol", false);
            acon.SetBool("Left", true);
            acon.SetBool("Right", false);
            horiFrame++;

            if (x <= 0)
            {
                h = 0;
            }
        }
        else if (h > 0)
        {
            acon.SetBool("Squat", false);
            acon.SetBool("Idol", false);
            acon.SetBool("Left", false);
            acon.SetBool("Right", true);
            horiFrame++;

            if (x + stage.chipSizeX >= stageSizeW)
            {
                h = 0;
            }
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
            boxCollider2D.size = new Vector2(boxCollider2D.size.x, 0.3f);
            boxCollider2D.offset = new Vector2(boxCollider2D.offset.x, -0.15f);
        }
        else if (v > 0)
        {
            boxCollider2D.size = new Vector2(boxCollider2D.size.x, 0.6f);
            boxCollider2D.offset = new Vector2(boxCollider2D.offset.x, 0);
        }
        else
        {
            boxCollider2D.size = new Vector2(boxCollider2D.size.x, 0.6f);
            boxCollider2D.offset = new Vector2(boxCollider2D.offset.x, 0);
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

    public float getPlayerW
    {
        get { return playerW; }
    }

    public float getPlayerH
    {
        get { return playerH; }
    }

    public bool PlayerMoveFlag
    {
        get { return isMoveFlag; }
        set { isMoveFlag = value; }
    }

    public void SetStart()
    {
        startCount = startConstCount;
    }
}
