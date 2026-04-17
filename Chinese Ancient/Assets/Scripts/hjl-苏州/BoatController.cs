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
    public float exitDistance = 3.0f;       // 离岸边多近可以下船，可根据情况使用

    private bool isPlayerOnBoard = false;
    private PlayerController player;
    private CharacterController playerController;

    private float currentSpeed = 0f;
    private float swayTimer = 0f;

    // 记录船只原本的旋转，避免和晃动旋转冲突
    private Quaternion baseRotation;

    void Start()
    {
        baseRotation = transform.rotation;
    }

    void Update()
    {
        if (isPlayerOnBoard)
        {
            HandleBoatMovement();
            HandleInput();
        }
    }

    void HandleInput()
    {
        // 简单按下 E 键下船 (可根据自身交互系统修改)
        if (Input.GetKeyDown(KeyCode.E) && currentSpeed < 0.2f)
        {
            ExitBoat();
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
        float zSway = 0f;
        if (Mathf.Abs(currentSpeed) > 0.1f || vertical != 0 || horizontal != 0)
        {
            swayTimer += Time.deltaTime * swayFrequency * (1.0f + Mathf.Abs(currentSpeed) * 0.5f); // 速度越快晃动稍微快一点
            zSway = Mathf.Sin(swayTimer) * swayAngle;
        }
        else
        {
            // 停下时晃动缓慢恢复平稳
            swayTimer = 0f;
            zSway = Mathf.Lerp(transform.rotation.eulerAngles.z, 0, Time.deltaTime);
            if (zSway > 180) zSway -= 360; // 处理欧拉角跨越 0-360 的问题
        }

        // 综合旋转：基础水平转向 + 左右晃动
        transform.rotation = baseRotation * Quaternion.Euler(0, 0, zSway);

        // 5. 应用位置移动
        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            // 根据当前朝向移动 (不包含晃动的旋转)
            Vector3 moveDir = baseRotation * Vector3.forward;
            transform.position += moveDir * currentSpeed * Time.deltaTime;
        }

        // 将相机和玩家的位置同步到船上
        if (player != null && playerStandPoint != null)
        {
             // 如果使用了 CharacterController，可能需要暂时禁用它或者直接使其跟随
             player.transform.position = playerStandPoint.position;
        }
    }

    // 玩家上船
    public void BoardBoat(PlayerController p)
    {
        if (isPlayerOnBoard) return;

        player = p;
        playerController = player.GetComponent<CharacterController>();

        // 切换状态
        isPlayerOnBoard = true;
        player.isOnBoat = true;

        // 使得人物变成船的子物体（或者通过 Update 同步位置）
        if (playerController != null) playerController.enabled = false;
        
        player.transform.SetParent(transform);
        if (playerStandPoint != null)
        {
            player.transform.position = playerStandPoint.position;
            // 同样同步人物的基础朝向
            player.transform.rotation = playerStandPoint.rotation;
        }

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

    // 可通过触发器实现自动上船交互
    private void OnTriggerEnter(Collider other)
    {
        if (!isPlayerOnBoard && other.CompareTag("Player"))
        {
            PlayerController p = other.GetComponent<PlayerController>();
            if (p != null)
            {
                BoardBoat(p);
            }
        }
    }
}