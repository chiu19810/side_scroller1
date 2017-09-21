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
    private bool squatTransReturnFlg;
    private bool squatFlag;
    private int startCount;
    private float jumpButtonFrame;
    private float jumpFrame;
    private float horiFrame;
    private float idolFrame;
    private float moveSpeedChange;
    private float squatTransFrame;
    private float squatTransReturnFrame;
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
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!((squatTransFrame < 40 && squatTransFrame > 0) || (squatTransReturnFrame < 40 && squatTransReturnFrame > 0)))
        {
            if (isJumpDown && jumpButtonFrame == 0)
                jumpButtonFrame++;

            if (isJump && jumpButtonFrame > 0)
                jumpButtonFrame++;

            if (jumpFrame > 0)
                jumpFrame--;
        }

        if (isGround)
        {
            jumpMoveFlag = false;
            horiJumpFlag = false;
            moveSpeedChange = 0;

            if (horiFrame > horiCount)
                horiJumpFlag = true;

            if (jumpFrame == 0)
            {
                acon.SetInteger("Jump", 3);
                rb.velocity = new Vector2(rb.velocity.x, 0);

                if (jumpButtonFrame > 0 && jumpButtonFrame < 30)
                    acon.SetInteger("Jump", 1);

                if (shift || acon.GetBool("Squat"))
                {
                    if (isJump && acon.GetInteger("Jump") == 1)
                    {
                        jumpFrame = 10;
                        rb.AddForce(Vector2.up * Sjump / 60, ForceMode2D.Impulse);
                    }
                }
                else
                {

                    if (jumpButtonFrame == 8 && acon.GetInteger("Jump") == 1)
                    {
                        jumpFrame = 10;
                        rb.AddForce(Vector2.up * Bjump / 60, ForceMode2D.Impulse);
                    }
                    else if (jumpButtonFrame < 8 && isJumpUp && acon.GetInteger("Jump") == 1)
                    {
                        jumpFrame = 10;
                        rb.AddForce(Vector2.up * Sjump / 60, ForceMode2D.Impulse);
                    }
                }
            }
        }
        else
        {
            if (acon.GetInteger("Jump") > 0)
            {
                if (rb.velocity.y > -1f && rb.velocity.y < 1f)
                {
                    acon.SetInteger("Jump", 2);
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
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

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
            horiFrame++;
            squatFlag = false;
            acon.SetBool("Idol", false);
            acon.SetBool("Left", true);
            acon.SetBool("Right", false);

            if (horiFrame < horiCount)
                acon.SetBool("Run", false);
            else
                acon.SetBool("Run", true);

            if (x <= 0)
            {
                h = 0;
            }
        }
        else if (h > 0)
        {
            horiFrame++;
            squatFlag = false;
            acon.SetBool("Idol", false);
            acon.SetBool("Left", false);
            acon.SetBool("Right", true);

            if (horiFrame < horiCount)
                acon.SetBool("Run", false);
            else
                acon.SetBool("Run", true);

            if (x + stage.chipSizeX >= stageSizeW)
            {
                h = 0;
            }
        }
        else
        {
            if (!acon.GetBool("Run"))
                horiFrame = 0;
        }

        if (v < 0)
        {
            if (h == 0)
            {
                acon.SetBool("Left", false);
                acon.SetBool("Right", false);
            }
            squatFlag = true;
            acon.SetBool("Run", false);
            moveSpeedChange = speed / 1.5f;
            horiFrame = 0;
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
                squatFlag = false;
                acon.SetBool("Run", false);
                acon.SetBool("Idol", true);
                acon.SetBool("Left", false);
                acon.SetBool("Right", false);
            }
            idolFrame++;
        }

        if (shift)
        {
            acon.SetBool("Run", false);
            moveSpeedChange = speed / 1.5f;
        }

        if (!horiJumpFlag && !jumpMoveFlag && !isGround)
            moveSpeedChange = speed / 1.5f;

        if (acon.GetInteger("Jump") == 3)
        {
            if (squatFlag)
            {
                squatTransReturnFlg = false;
                squatTransReturnFrame = 0;
                squatTransFrame++;
            }
            else
            {
                if (squatTransFrame > 0 && !squatTransReturnFlg)
                    squatTransReturnFlg = true;
                squatTransFrame = 0;
            }

            if (squatTransReturnFlg)
                squatTransReturnFrame++;

            if ((squatTransFrame < 40 && squatTransFrame > 0) || (squatTransReturnFrame < 40 && squatTransReturnFrame > 0))
            {
                moveSpeedChange = speed;
                if (squatTransFrame == 1 || squatTransReturnFrame == 1)
                {
                    GameObject effectPlayer = new GameObject("EffectPlayer");
                    SpriteRenderer sr = effectPlayer.AddComponent<SpriteRenderer>();
                    Animator am = effectPlayer.AddComponent<Animator>();
                    Sprite sprite = Resources.Load("Textures/Chara/Player/Player_Transform", typeof(Sprite)) as Sprite;
                    sr.sprite = sprite;
                    sr.sortingOrder = 1;
                    effectPlayer.transform.localScale = new Vector3(3, 3, 3);
                    effectPlayer.transform.position = player.transform.position;
                    am.runtimeAnimatorController = (RuntimeAnimatorController)Instantiate(Resources.Load("Animations/Animators/Effect"));
                }
            }

            if (squatTransFrame > 40)
            {
                boxCollider2D.size = new Vector2(boxCollider2D.size.x, 0.3f);
                boxCollider2D.offset = new Vector2(boxCollider2D.offset.x, -0.15f);
            }

            if (squatTransFrame > 10)
            {
                acon.SetBool("Squat", true);
            }
            else if (squatTransReturnFrame > 10)
            {
                acon.SetBool("Squat", false);
            }

            if (squatTransFrame > 40 || squatTransReturnFrame > 40)
                Destroy(GameObject.Find("EffectPlayer"));
        }

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
