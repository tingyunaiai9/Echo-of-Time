/*
 * PlayerController.cs
 *
 * Mirror 网络玩家控制器。
 * 负责本地玩家输入、移动、物理同步，支持多实例联机。
 *
 * 主要功能：
 * - 仅本地玩家处理输入与物理（isLocalPlayer）
 * - 服务器权威位置，客户端预测移动
 * - 支持刚体/非刚体移动
 * - 通过 NetworkTransform 实现位置/旋转同步
 *
 * 使用说明：
 * - Prefab 必须挂载 NetworkIdentity、NetworkTransform、Rigidbody、Collider
 * - PlayerPrefab 设置到 NetworkManager 后自动生成
 * - 场景中不要直接放玩家对象
 */
using UnityEngine;
// using Gameplay.Puzzle; // 如果 prop.cs 使用了命名空间，请在此添加
using Mirror;

/// <summary>
/// Mirror 网络玩家控制器，仅本地玩家处理输入与物理。
/// </summary>
public class PlayerController : NetworkBehaviour
{
    [Tooltip("移动速度（单位：米/秒)")]
    public float moveSpeed = 5f; // 玩家移动速度

    [Tooltip("旋转速度（度/秒)，面向移动方向时使用")]
    public float rotationSpeed = 720f; // 玩家旋转速度

    [Header("交互设置")]
    [Tooltip("交互的最大范围半径（米）。不再使用方向限制。")]
    public float interactionRange = 3f;

    [Tooltip("可交互物体的 LayerMask ( 可选，用于优化 )")]
    public LayerMask interactableLayer;

    [Header("相机跟随设置")]
    [Tooltip("要跟随的相机 Transform，通常是场景中的 Main Camera。如果不指定，Awake 时会尝试自动查找。")]
    public Transform cameraTransform;

    [Tooltip("相机相对于玩家的固定偏移量（如: (0, 10, -10)）。")]
    public Vector3 cameraOffset = new Vector3(0f, 0f, -10f);

    [Tooltip("相机跟随的平滑度（值越大，跟随越慢/平滑）。推荐值 0.05 到 0.2。")]
    [Range(0.01f, 1f)]
    public float cameraSmoothSpeed = 0.125f;

    Rigidbody rb; // 刚体组件
    bool initialized; // 权限初始化标记

    /// <summary>
    /// Unity生命周期：获取刚体并冻结旋转。
    /// </summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 推荐在带刚体的对象上冻结旋转，由脚本控制朝向
            rb.freezeRotation = true;
        }

        if (cameraTransform == null)
        {
            if (Camera.main != null) {
                cameraTransform = Camera.main.transform;
            }
            else {
                Debug.LogWarning("PlayerController: 未指定 cameraTransform，且场景中没有 Main Camera。请手动设置相机引用。");
            }
        }
    }

    /// <summary>
    /// Mirror回调：本地玩家获得控制权时允许物理模拟。
    /// </summary>
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        // 本地玩家获取控制权时，允许物理模拟
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        initialized = true;
    }

    /// <summary>
    /// Mirror回调：所有客户端实例初始化，非本地玩家刚体设为Kinematic。
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        // 非本地玩家在本机不进行物理模拟，位置由 NetworkTransform 同步
        if (!isLocalPlayer && rb != null)
        {
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Unity帧更新：仅本地玩家处理输入与移动。
    /// </summary>
    void Update()
    {
        // 仅本地玩家处理输入
        if (!isLocalPlayer) return;

        // 读取输入（Horizontal: A/D or ←/→, Vertical: W/S or ↑/↓）
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v).normalized;

        // 平滑朝向移动方向
        if (input.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(input);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        // 如果没有刚体，直接用 Transform 移动（在 Update 中）
        if (rb == null)
        {
            transform.Translate(input * moveSpeed * Time.deltaTime, Space.World);
        }

        // ------------------------
        // 交互输入检测
        // ------------------------
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryInteract();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            // 切换背包界面
            Inventory.ToggleBackpack();
        }
    }

    /// <summary>
    /// Unity物理帧：仅本地玩家驱动物理移动。
    /// </summary>
    void FixedUpdate()
    {
        // 仅本地玩家进行物理移动
        if (!isLocalPlayer) return;

        // 如果有刚体，使用物理移动（MovePosition）
        if (rb != null)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 input = new Vector3(h, 0f, v).normalized;
            Vector3 targetPos = rb.position + input * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPos);
        }
    }

    void LateUpdate()
    {
        if (!isLocalPlayer) return;
        if (cameraTransform != null)
        {
            // 1. 计算目标位置 (玩家位置 + 偏移量)
            Vector3 targetPosition = transform.position + cameraOffset;

            // 2. 使用 Lerp 进行平滑插值移动
            // Time.deltaTime / cameraSmoothSpeed 决定了平滑度，值越小越平滑
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position, 
                targetPosition, 
                Time.deltaTime * (1f / cameraSmoothSpeed)
            );
            
            // 3. (可选) 让相机始终面向玩家
            // cameraTransform.LookAt(transform.position); 
        }
    }

    /// <summary>
    /// 尝试与玩家附近的 prop.cs 物体进行交互（范围检测，不限方向）
    /// </summary>
    private void TryInteract()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRange, interactableLayer);

        if (hitColliders.Length > 0)
        {
            Interaction best = null;
            float closestDistanceSqr = float.MaxValue;
            Vector3 playerPosition = transform.position;

            foreach (var hitCollider in hitColliders)
            {
                // 优先找任意 Interaction 子类
                Interaction current = hitCollider.GetComponent<Interaction>();
                if (current != null)
                {
                    float distSqr = (hitCollider.transform.position - playerPosition).sqrMagnitude;
                    if (distSqr < closestDistanceSqr)
                    {
                        closestDistanceSqr = distSqr;
                        best = current;
                    }
                }
            }

            if (best != null)
            {
                Debug.Log($"在范围内找到最近的交互物体: {best.gameObject.name}, 尝试交互。");
                best.OnInteract(this);
                return;
            }

            // 兼容：若未找到 Interaction，再尝试老的 prop 逻辑
            prop fallback = null;
            float closestProp = float.MaxValue;
            foreach (var hitCollider in hitColliders)
            {
                var p = hitCollider.GetComponent<prop>();
                if (p != null)
                {
                    float distSqr = (hitCollider.transform.position - playerPosition).sqrMagnitude;
                    if (distSqr < closestProp)
                    {
                        closestProp = distSqr;
                        fallback = p;
                    }
                }
            }

            if (fallback != null)
            {
                Debug.Log($"在范围内找到 prop: {fallback.gameObject.name}, 尝试交互（拾取）。");
                fallback.OnInteract(this);
            }
            else
            {
                Debug.Log("在范围内检测到 Collider，但没有找到可交互对象。");
            }
        }
    }
    
    // 可选：在 Scene 视图中绘制交互范围，便于调试
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // 绘制一个球体来表示交互范围
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}