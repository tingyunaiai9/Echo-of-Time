/*
 * PlayerController.cs
 * * 负责玩家的移动、交互、状态管理等功能。
 * 使用 Mirror 实现网络同步和客户端预测。
 * * 主要功能：
 * - 处理玩家输入（移动、交互、切换视角）
 * - 客户端预测移动，服务器权威位置
 * - 与环境对象交互（通过接口 IInteractable）
 * - 管理玩家状态（Idle, Moving, Interacting, Stunned, Dead）
 * - 支持时间线视角切换（通过命令和 RPC）
 * * 注意事项：
 * - 确保 Rigidbody 和 Collider 设置正确以避免物理问题
 * - 交互对象需实现 IInteractable 接口
 * - 服务器端需验证所有客户端请求以防作弊
 */
using UnityEngine;
// using Gameplay.Puzzle; // 如果 prop.cs 使用了命名空间，请在此添加

public class PlayerController : MonoBehaviour
{
    [Tooltip("移动速度（单位：米/秒）")]
    public float moveSpeed = 5f;

    [Tooltip("旋转速度（度/秒），面向移动方向时使用")]
    public float rotationSpeed = 720f;

    [Header("交互设置")]
    [Tooltip("交互的最大范围半径（米）。不再使用方向限制。")]
    public float interactionRange = 3f;

    [Tooltip("可交互物体的 LayerMask (可选，用于优化)")]
    public LayerMask interactableLayer; // <--- 定义在这里！

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 推荐在带刚体的对象上冻结旋转，由脚本控制朝向
            rb.freezeRotation = true;
        }
    }

    void Update()
    {
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
    }

    void FixedUpdate()
    {
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

    /// <summary>
    /// 尝试与玩家附近的 prop.cs 物体进行交互（范围检测，不限方向）
    /// </summary>
    private void TryInteract()
    {
        // 使用 Physics.OverlapSphere 检测玩家周围 interactionRange 范围内的所有 Collider
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRange, interactableLayer);

        if (hitColliders.Length > 0)
        {
            prop bestProp = null;
            float closestDistanceSqr = float.MaxValue;
            Vector3 playerPosition = transform.position;

            // 遍历所有检测到的 Collider，找到最近且具有 prop.cs 脚本的物体
            foreach (var hitCollider in hitColliders)
            {
                prop currentProp = hitCollider.GetComponent<prop>();

                // 检查是否具有 prop.cs 脚本
                if (currentProp != null)
                {
                    // 计算距离平方，避免开方运算，优化性能
                    float distSqr = (hitCollider.transform.position - playerPosition).sqrMagnitude;
                    
                    if (distSqr < closestDistanceSqr)
                    {
                        closestDistanceSqr = distSqr;
                        bestProp = currentProp;
                    }
                }
            }

            // 如果找到了最近的可交互物体
            if (bestProp != null)
            {
                Debug.Log($"在范围内找到最近的物体: {bestProp.gameObject.name}, 尝试交互。");
                bestProp.DisappearImmediately(); // 调用 prop.cs 的 DisappearImmediately 方法

                // 【本地测试/单机模式下】: 直接调用 prop.cs 的 Interact 方法
                // interactableProp.Interact(this); 
            }
            else
            {
                Debug.Log("在范围内检测到 Collider，但没有找到有效的 prop.cs 脚本。");
            }
        }
        else
        {
            // Debug.Log("未在交互范围内检测到任何物体。");
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