using UnityEngine;

public class BoatController : MonoBehaviour
{
    [Header("--- 移动参数 (慢节奏) ---")]
    public float maxSpeed = 3.0f;           // 最大航行速度
    public float acceleration = 0.5f;       // 加速度（缓慢提速）
    public float deceleration = 0.3f;       // 减速度（惯性缓慢停下）
    public float turnSpeed = 15.0f;         // 转向速度（缓慢调转方向）

    [Header("--- 摇橹晃动参数 ---")]
    public float swayAngle = 3.0f;          // 左右晃动的最大角度
    public float swayFrequency = 2.0f;      // 晃动频率

    [Header("--- 交互与位置 ---")]
    public Transform playerStandPoint;      // 人物站在船上的位置
    public Transform lotusTargetPoint;      // 荷花自动飞往的专属目标存放点（空物体）
    public float interactDistance = 2.0f;   // 靠近多少米可以按F上船

    private bool isPlayerOnBoard = false;
    private PlayerController player;
    private CharacterController playerController;

    private float currentSpeed = 0f;
    private float swayTimer = 0f;
    private float currentSway = 0f; // 记录当前的倾斜角度，防止欧拉角计算翻转

    // 记录船只原本的旋转，避免和晃动旋转冲突
    private Quaternion baseRotation;

    void Start()
    {
        baseRotation = transform.rotation;
        // 尝试自动获取场景中的玩家
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.GetComponentInChildren<PlayerController>();
                }
            }
        }
    }

    void Update()
    {
        if (isPlayerOnBoard)
        {
            HandleBoatMovement();
            HandleOnBoardInput();
        }
        else
        {
            HandleOffBoardInput();
        }
    }

    void HandleOnBoardInput()
    {
        // 在船上按下 F 键下船
        if (Input.GetKeyDown(KeyCode.F) && currentSpeed < 0.2f)
        {
            ExitBoat();
        }
    }

    void HandleOffBoardInput()
    {
        // 不在船上时，检测玩家距离
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (distance <= interactDistance)
                {
                    BoardBoat(player);
                }
                else
                {
                    Debug.Log($"<color=yellow>[船只交互]</color> 距离太远无法上船，当前距离: {distance:F2}米，要求距离内: {interactDistance}米。如果一直按不上请在船的面板上调大 Interact Distance！");
                }
            }
        }
        else
        {
            // 实时查找玩家防丢
            player = FindObjectOfType<PlayerController>();
        }
    }

    void HandleBoatMovement()
    {
        // 1. 获取输入
        float vertical = Input.GetAxisRaw("Vertical");     // W/S 上下
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D 左右

        // 2. 转向处理 (A/D 按键)
        if (horizontal != 0)
        {
            // 缓慢旋转 baseRotation
            baseRotation *= Quaternion.Euler(0, horizontal * turnSpeed * Time.deltaTime, 0);
        }

        // 3. 速度与惯性处理 (W/S 按键)
        if (vertical != 0)
        {
            // 缓慢提速
            currentSpeed += vertical * acceleration * Time.deltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed * 0.5f, maxSpeed); // 后退速度减半
        }
        else
        {
            // 松开按键后，带着惯性慢慢停下
            currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * Time.deltaTime);
        }

        // 4. 计算摇橹的左右晃动效果
        // 只有在有按键输入或者船本身还在移动时才晃动
        if (Mathf.Abs(currentSpeed) > 0.1f || vertical != 0 || horizontal != 0)
        {
            swayTimer += Time.deltaTime * swayFrequency * (1.0f + Mathf.Abs(currentSpeed) * 0.5f); // 速度越快晃动稍微快一点
            currentSway = Mathf.Sin(swayTimer) * swayAngle;
        }
        else
        {
            // 停下时晃动缓慢恢复平稳
            swayTimer = 0f;
            currentSway = Mathf.Lerp(currentSway, 0, Time.deltaTime * 2f);
        }

        // 综合旋转：基础水平转向 + 左右晃动
        transform.rotation = baseRotation * Quaternion.Euler(0, 0, currentSway);

        // 5. 应用位置移动
        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            // 根据当前朝向移动 (不包含晃动的旋转)
            Vector3 moveDir = baseRotation * Vector3.forward;
            transform.position += moveDir * currentSpeed * Time.deltaTime;
        }

        // 强行同步位置，不要依赖父子层级的自动跟随（有些老版本的CharacterController在禁用后会有坐标缓存问题）
        if (player != null && playerStandPoint != null)
        {
             player.transform.position = playerStandPoint.position;
             // 注意不要直接去覆盖旋转，因为玩家视角的鼠标脚本会处理 X Y 轴旋转
        }
    }

    // 玩家上船
    public void BoardBoat(PlayerController p)
    {
        if (isPlayerOnBoard) return;

        player = p;
        // 把 GetComponentInChildren 改回 GetComponent，因为 Move里是在自身获取的
        playerController = player.GetComponent<CharacterController>();

        // 切换状态
        isPlayerOnBoard = true;
        player.isOnBoat = true;

        // 移除原有的移动干预，如果在船上，强制关掉CharacterController避免它把人物卡留在原地
        if (playerController != null) 
        {
            playerController.enabled = false;
        }
        
        player.transform.SetParent(transform);
        
        // 确保站立点一定是船的子物体，否则它会留在岸边导致人无法跟着船走！
        if (playerStandPoint != null && !playerStandPoint.IsChildOf(transform))
        {
            Debug.LogWarning("PlayerStandPoint 不是船的子物体！已自动将其设置为船的子物体。");
            playerStandPoint.SetParent(transform);
        }
        
        // 如果没有指定站立点，就生成一个默认的
        if (playerStandPoint == null)
        {
            GameObject sp = new GameObject("DefaultStandPoint");
            sp.transform.SetParent(transform);
            sp.transform.localPosition = new Vector3(0, 0.5f, 0); // 默认高一点点
            sp.transform.localRotation = Quaternion.identity;
            playerStandPoint = sp.transform;
            Debug.Log("未绑定PlayerStandPoint，已动态生成默认站立点！");
        }

        player.transform.position = playerStandPoint.position;
        player.transform.rotation = playerStandPoint.rotation;

        Debug.Log("<color=green>人物已上船</color>");
    }

    // 玩家下船
    public void ExitBoat()
    {
        if (!isPlayerOnBoard) return;

        isPlayerOnBoard = false;
        player.isOnBoat = false;

        player.transform.SetParent(null);
        if (playerController != null) playerController.enabled = true;

        Debug.Log("<color=green>人物已下船</color>");
        player = null;
    }

    // 可以选择使用 UI 提示玩家按 F 上船
    /* 
    private void OnGUI()
    {
        if (!isPlayerOnBoard && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= interactDistance)
            {
                GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height / 2 + 50, 200, 30), "按 [F] 键上船");
            }
        }
        else if (isPlayerOnBoard && currentSpeed < 0.2f)
        {
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height / 2 + 50, 200, 30), "按 [F] 键下船");
        }
    }
    */
}