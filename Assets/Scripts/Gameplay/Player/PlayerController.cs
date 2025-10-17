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
}
