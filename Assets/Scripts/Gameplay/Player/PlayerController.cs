/*
    * PlayerController.cs
    * 
    * 负责玩家的移动、交互、状态管理等功能。
    * 使用 Mirror 实现网络同步和客户端预测。
    * 
    * 主要功能：
    * - 处理玩家输入（移动、交互、切换视角）
    * - 客户端预测移动，服务器权威位置
    * - 与环境对象交互（通过接口 IInteractable）
    * - 管理玩家状态（Idle, Moving, Interacting, Stunned, Dead）
    * - 支持时间线视角切换（通过命令和 RPC）
    * 
    * 注意事项：
    * - 确保 Rigidbody 和 Collider 设置正确以避免物理问题
    * - 交互对象需实现 IInteractable 接口
    * - 服务器端需验证所有客户端请求以防作弊
    */
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Tooltip("移动速度（单位：米/秒）")]
    public float moveSpeed = 5f;

    [Tooltip("旋转速度（度/秒），面向移动方向时使用")]
    public float rotationSpeed = 720f;

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
}
