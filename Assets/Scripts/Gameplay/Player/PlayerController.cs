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
using Mirror;

/* 玩家状态枚举 */
public enum PlayerState
{
    Idle = 0,
    Moving = 1,       // 左右移动
    Interacting = 2,  // 交互中
    Stunned = 3,      // 被控、无法移动
    Dead = 4          // 死亡/不可操作
}
/* 主玩家控制器 */
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float networkSmooth = 10f;

    [Header("Interaction")]
    [SerializeField] float interactRange = 1.2f;
    [SerializeField] LayerMask interactMask;

    Rigidbody rb;
    Animator animator;

    // 本地输入（只用 x 分量）
    float inputX;

    // 网络同步字段
    [SyncVar] Vector3 syncPosition;
    [SyncVar] Quaternion syncRotation;
    [SyncVar(hook = nameof(OnStateChanged))] PlayerState currentState;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        // 初始化同步值
        syncPosition = transform.position;
        syncRotation = transform.rotation;
    }

    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            // 客户端预测：即时移动以获得流畅手感
            Vector3 move = transform.right * inputX * moveSpeed * Time.fixedDeltaTime;
            if (rb != null)
                rb.MovePosition(rb.position + move);
            else
                transform.position += move;

            // 向服务器发送位置（可按需节流）
            CmdSendTransform(transform.position, transform.rotation);
        }
        else
        {
            // 非本地玩家使用插值平滑位置/朝向
            transform.position = Vector3.Lerp(transform.position, syncPosition, Time.fixedDeltaTime * networkSmooth);
            transform.rotation = Quaternion.Slerp(transform.rotation, syncRotation, Time.fixedDeltaTime * networkSmooth);
        }
    }

    /* 处理玩家移动输入 */
    public void HandleMovementInput(Vector2 input)
    {
        if (!isLocalPlayer) return;

        // 只使用左右（x），保留前后（y）但不启用
        inputX = input.x;

        // 更新动画参数（假设 Animator 有 "Speed" 参数）
        if (animator != null)
            animator.SetFloat("Speed", Mathf.Abs(inputX));
    }

    /* 执行环境交互 */
    public void PerformInteraction()
    {
        if (!isLocalPlayer) return;

        // 在玩家前方检测可交互对象
        Vector3 origin = transform.position;
        Collider[] hits = Physics.OverlapSphere(origin + transform.forward * 0.5f, interactRange, interactMask);
        if (hits.Length == 0) return;

        // 选最近的可交互对象
        Collider nearest = null;
        float minDist = float.MaxValue;
        foreach (var c in hits)
        {
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < minDist) { minDist = d; nearest = c; }
        }

        if (nearest != null && nearest.attachedRigidbody != null)
        {
            // 通过 NetworkIdentity 将对象引用发给服务器
            var ni = nearest.GetComponentInParent<NetworkIdentity>();
            if (ni != null)
                CmdRequestInteract(ni.gameObject);
            else
                CmdRequestInteract(nearest.gameObject); // 如果没有 NetworkIdentity 也尝试（服务器端需校验）
        }
    }

    /* 切换时间线视角 */
    public void SwitchTimelineView(int timelineId)
    {
        if (!isLocalPlayer) return;
        CmdSwitchTimelineView(timelineId);
    }

    /* 管理玩家状态 */
    public void UpdatePlayerState(PlayerState newState)
    {
        if (isServer)
        {
            // 服务器直接设置（权威）
            currentState = newState;
        }
        else if (isLocalPlayer)
        {
            // 客户端请求服务器更新
            CmdUpdatePlayerState(newState);
        }
    }

    // ---------- Mirror Commands / RPCs ----------

    [Command]
    void CmdSendTransform(Vector3 pos, Quaternion rot)
    {
        // 简单服务器权威：更新同步变量
        syncPosition = pos;
        syncRotation = rot;
    }

    [Command]
    void CmdRequestInteract(GameObject target)
    {
        // 服务器验证：目标在合理范围内且可交互
        if (target == null) return;
        if (Vector3.Distance(transform.position, target.transform.position) > interactRange + 1.5f) return;

        // 尝试调用 IInteractable（如果你的项目中有该接口）
        // var interactable = target.GetComponent<IInteractable>();
        // if (interactable != null)
        // {
        //     // connectionToClient 表示请求交互的客户端
        //     interactable.OnInteract(connectionToClient.identity.gameObject);
        // }

        // 广播播放交互动画/特效
        RpcPlayInteraction(target);
    }

    [ClientRpc]
    void RpcPlayInteraction(GameObject target)
    {
        // 在客户端播放交互动画或特效（可扩展）
        if (target == null) return;
        // 示例：尝试调用目标的动画触发器或发送消息
        target.SendMessage("OnClientPlayedInteraction", SendMessageOptions.DontRequireReceiver);
        // 本地玩家也可以播放自己的交互动画
        if (isLocalPlayer && animator != null)
            animator.SetTrigger("Interact");
    }

    [Command]
    void CmdSwitchTimelineView(int timelineId)
    {
        // 服务器端可做权限校验、加载数据等
        // 验证示例：仅允许玩家在某些状态下切换（省略具体逻辑）
        RpcSwitchTimelineView(timelineId);
    }

    [ClientRpc]
    void RpcSwitchTimelineView(int timelineId)
    {
        // 客户端更新视图（由 UI 或摄像机管理代码处理）
        // 这里发送消息供其他组件处理
        SendMessage("OnTimelineViewSwitched", timelineId, SendMessageOptions.DontRequireReceiver);
    }

    [Command]
    void CmdUpdatePlayerState(PlayerState newState)
    {
        // 服务器权威设置并通过 SyncVar 同步给所有客户端
        currentState = newState;
    }

    void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        // 在客户端应用状态改变（动画、碰撞、移动限制等）
        // 示例：根据状态设置动画层或参数
        if (animator != null)
            animator.SetInteger("PlayerState", (int)newState);
    }

    // 可选：调试绘制交互范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 0.5f, interactRange);
    }
}
