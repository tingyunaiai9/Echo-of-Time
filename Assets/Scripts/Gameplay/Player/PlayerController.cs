using UnityEngine;
using Mirror;
using Events;

/*
 * 玩家控制器：处理玩家输入、移动、交互等功能
 */
public class PlayerController : NetworkBehaviour
{
    [Tooltip("移动速度（单位：米/秒)")]
    public float moveSpeed = 5f;

    [Tooltip("旋转速度（度/秒)，面向移动方向时使用")]
    public float rotationSpeed = 720f;

    [Header("交互设置 (Box检测)")]
    [Tooltip("交互检测盒子的尺寸 (宽, 高, 深)")]
    public Vector3 interactionBoxSize = new Vector3(1.5f, 2.5f, 2f);

    [Tooltip("交互检测盒子的偏移量 (相对于角色脚底)")]
    public Vector3 interactionOffset = new Vector3(0f, 1.25f, 0f);

    [Tooltip("可交互物体的 LayerMask")]
    public LayerMask interactableLayer;

    [Header("相机跟随设置")]
    [Tooltip("要跟随的相机 Transform，通常是场景中的 Main Camera。如果不指定，Awake 时会尝试自动查找。")]
    public Transform cameraTransform;

    [Tooltip("相机相对于玩家的固定偏移量。")]
    public Vector3 cameraOffset = new Vector3(0f, 0f, -10f);

    [Tooltip("相机跟随的平滑度（值越大，跟随越平滑）。")]
    [Range(0.01f, 1f)]
    public float cameraSmoothSpeed = 0.125f;

    [Header("运动约束")]
    [Tooltip("背景物体的 Tag，用于限定左右范围")]
    public string backgroundTag = "Background";
    [Tooltip("左右范围外额外留白")]
    public float horizontalPadding = 0.5f;

    private float minX;
    private float maxX;
    private bool hasBounds;
    Rigidbody rb;
    bool initialized;

    // 动画相关
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");

    /* 背包打开时禁用游戏输入 */
    bool isBackpackOpen;

    // 网络同步变量
    [SyncVar(hook = nameof(OnIsWalkingChanged))]
    private bool _isWalking;

    [SyncVar(hook = nameof(OnFlipXChanged))]
    private bool _flipX;

    // 本地状态追踪，防止重复发送 Command
    private bool _lastSentIsWalking;
    private bool _lastSentFlipX;

    /* 初始化刚体和相机 */
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 尝试获取 Animator 和 SpriteRenderer，如果不在根节点，尝试在子节点查找
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (rb != null)
        {
            rb.freezeRotation = true;
            // 冻结不使用的轴，不允许前后(Z) 与上下(Y) 位移
            rb.constraints = RigidbodyConstraints.FreezeRotation |
                             RigidbodyConstraints.FreezePositionZ |
                             RigidbodyConstraints.FreezePositionY;
        }

        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("PlayerController: 未找到 Main Camera，相机跟随功能将不可用。");
            }
        }
    }

    void Start()
    {
        if (isLocalPlayer)
            StartCoroutine(TryAcquireBackgroundBounds());
    }

    System.Collections.IEnumerator TryAcquireBackgroundBounds()
    {
        // 最多重试 30 帧，应对延迟加载
        for (int i = 0; i < 30 && !hasBounds; i++)
        {
            AcquireBackgroundBounds();
            if (hasBounds) break;
            yield return null;
        }
    }

    void AcquireBackgroundBounds()
    {
        var bg = GameObject.FindWithTag(backgroundTag);
        if (bg != null)
        {
            var r = bg.GetComponent<Renderer>();
            if (r != null)
            {
                Bounds b = r.bounds;
                minX = b.min.x + horizontalPadding;
                maxX = b.max.x - horizontalPadding;
                hasBounds = true;
                Debug.Log($"[PlayerController] 背景范围设置: X[{minX},{maxX}]");
                return;
            }
        }
    }


    /* 权限启动时设置刚体 */
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        if (rb != null) rb.isKinematic = false;
        initialized = true;
    }

    /* 非本地玩家启动时设置刚体 */
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isLocalPlayer && rb != null)
        {
            rb.isKinematic = true;
        }
    }

    /* 本地玩家启动时初始化 */
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        AcquireBackgroundBounds();

        // 同步当前 UI 冻结状态
        var uiMgr = FindFirstObjectByType<UIManager>();
        bool frozen = uiMgr != null && uiMgr.UIFrozen;
        OnBackpackStateChanged(frozen);
        
        // 初始化同步状态记录
        _lastSentIsWalking = _isWalking;
        _lastSentFlipX = _flipX;
    }

    /* 销毁时 */
    void OnDestroy()
    {
        // 无事件订阅需要取消
    }

    /* 背包/冻结状态变化回调 */
    void OnBackpackStateChanged(bool isOpen)
    {
        isBackpackOpen = isOpen;
        if (isBackpackOpen)
        {
            // 停止移动
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
            // 停止动画
            UpdateAnimation(0f);
        }
    }

    /* 更新动画状态和朝向 */
    void UpdateAnimation(float horizontalInput)
    {
        EnsureComponents();

        // 1. 计算目标状态
        bool targetFlipX = _lastSentFlipX; // 保持上一次的状态
        if (horizontalInput != 0)
        {
            targetFlipX = (horizontalInput < 0);
        }
        
        bool targetIsWalking = Mathf.Abs(horizontalInput) > 0.01f;

        // 2. 本地立即应用表现 (预测)
        if (spriteRenderer != null) spriteRenderer.flipX = targetFlipX;
        if (animator != null) animator.SetBool(IsWalking, targetIsWalking);

        // 3. 检查是否需要同步到服务器
        if (targetFlipX != _lastSentFlipX)
        {
            CmdSetFlipX(targetFlipX);
            _lastSentFlipX = targetFlipX;
        }

        if (targetIsWalking != _lastSentIsWalking)
        {
            CmdSetWalking(targetIsWalking);
            _lastSentIsWalking = targetIsWalking;
        }
    }

    // 确保获取到当前激活的 Animator 和 SpriteRenderer (因为 TimelinePlayer 可能会切换皮肤)
    private void EnsureComponents()
    {
        if (animator == null || !animator.gameObject.activeInHierarchy)
        {
            animator = GetComponentInChildren<Animator>(false);
        }
        
        if (spriteRenderer == null || !spriteRenderer.gameObject.activeInHierarchy)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(false);
        }
    }

    #region Network Animation Sync

    [Command]
    private void CmdSetWalking(bool value)
    {
        _isWalking = value;
    }

    [Command]
    private void CmdSetFlipX(bool value)
    {
        _flipX = value;
    }

    private void OnIsWalkingChanged(bool oldVal, bool newVal)
    {
        // 非本地玩家通过 Hook 更新动画
        // 本地玩家在 UpdateAnimation 中已经更新了，但为了保证最终一致性，也可以执行
        // 为了避免本地预测时的冲突，通常本地玩家可以忽略 Hook，或者 Hook 只是作为修正
        // 这里简单处理：都执行，确保组件引用是最新的
        EnsureComponents();
        if (animator != null)
        {
            animator.SetBool(IsWalking, newVal);
        }
    }

    private void OnFlipXChanged(bool oldVal, bool newVal)
    {
        EnsureComponents();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = newVal;
        }
    }

    #endregion

    /* 当前选中的交互对象 */
    private Interaction currentInteraction;

    /* 每帧更新交互目标并处理高亮 */
    void UpdateInteractionTarget()
    {
        if (!isLocalPlayer || isBackpackOpen) return;

        // 计算检测盒子的中心点位置
        Vector3 center = transform.position + interactionOffset;

        // 1. 尝试 2D 检测 (Box)
        // 注意：OverlapBox 在 2D 中使用的是 "Size" (全尺寸)
        Collider2D[] hitColliders2D = Physics2D.OverlapBoxAll(center, new Vector2(interactionBoxSize.x, interactionBoxSize.y), 0f, interactableLayer);
        
        // 2. 尝试 3D 检测 (Box)
        // 注意：OverlapBox 在 3D 中使用的是 "HalfExtents" (半尺寸)，所以要除以 2
        Collider[] hitColliders3D = Physics.OverlapBox(center, interactionBoxSize / 2f, Quaternion.identity, interactableLayer);

        Interaction best = null;
        float closestDistanceSqr = float.MaxValue;
        Vector3 playerPosition = transform.position;

        // 处理 2D 结果
        foreach (var hit in hitColliders2D)
        {
            ProcessHit(hit.gameObject, playerPosition, ref best, ref closestDistanceSqr);
        }

        // 处理 3D 结果
        foreach (var hit in hitColliders3D)
        {
            ProcessHit(hit.gameObject, playerPosition, ref best, ref closestDistanceSqr);
        }

        // 切换高亮状态
        if (best != currentInteraction)
        {
            if (currentInteraction != null) currentInteraction.SetHighlight(false);
            currentInteraction = best;
            if (currentInteraction != null) currentInteraction.SetHighlight(true);
        }
    }

    // 辅助方法：处理碰撞结果，避免代码重复
    void ProcessHit(GameObject hitObj, Vector3 playerPos, ref Interaction best, ref float closestDist)
    {
        Interaction current = hitObj.GetComponent<Interaction>();
        if (current == null) current = hitObj.GetComponentInParent<Interaction>();

        if (current != null && current.isActiveAndEnabled)
        {
            float distSqr = (hitObj.transform.position - playerPos).sqrMagnitude;
            if (distSqr < closestDist)
            {
                closestDist = distSqr;
                best = current;
            }
        }
    }

    /* 处理玩家输入和移动 */
    void Update()
    {
        if (!isLocalPlayer) return;

        // 监测 UI 冻结状态变化
        var uiMgr = FindFirstObjectByType<UIManager>();
        bool uiFrozen = uiMgr != null && uiMgr.UIFrozen;
        if (uiFrozen != isBackpackOpen)
        {
            OnBackpackStateChanged(uiFrozen);
        }

        // 背包打开时，禁用游戏输入（移动、交互等）
        if (isBackpackOpen) return;

        // 更新交互目标
        UpdateInteractionTarget();

        // 游戏输入逻辑（移动、旋转）
        float h = Input.GetAxisRaw("Horizontal");
        // 禁止前后：垂直输入直接忽略
        float v = 0f;

        // 更新动画状态
        UpdateAnimation(h);

        Vector3 input = new Vector3(h, 0f, v);

        if (rb == null)
        {
            transform.Translate(new Vector3(input.x, 0f, 0f) * moveSpeed * Time.deltaTime, Space.World);
            ClampPosition();
        }

        // 交互键（F键）
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryInteract();
        }
    }

    /* 物理更新：处理刚体移动 */
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        
        // 背包打开时，禁用物理移动
        if (isBackpackOpen) return;

        if (rb != null)
        {
            float h = Input.GetAxisRaw("Horizontal");
            Vector3 v = rb.linearVelocity;
            v.x = h * moveSpeed;
            v.z = 0f;
            rb.linearVelocity = v;
            ClampPosition();
        }
    }

    void ClampPosition()
    {
        if (!hasBounds) return;
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.z = 0f; // 强制不前后移动
        transform.position = p;
    }

    /* 相机跟随 */
    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        if (cameraTransform != null)
        {
            Vector3 desiredPosition = transform.position + cameraOffset;
            // 相机位置裁剪，防止超出背景
            desiredPosition = ClampCameraToBackground(desiredPosition);

            Vector3 smoothedPosition = Vector3.Lerp(cameraTransform.position, desiredPosition, cameraSmoothSpeed);
            cameraTransform.position = smoothedPosition;
        }
    }

    // 将相机中心限制在背景内
    Vector3 ClampCameraToBackground(Vector3 camPos)
    {
        if (!hasBounds || cameraTransform == null) return camPos;

        var cam = cameraTransform.GetComponent<Camera>();
        if (cam == null) return camPos;

        // 正交相机半高半宽
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float cxMin = minX + halfW;
        float cxMax = maxX - halfW;

        float clampedX = (cxMin <= cxMax) ? Mathf.Clamp(camPos.x, cxMin, cxMax) : (minX + maxX) * 0.5f;

        return new Vector3(clampedX, camPos.y, camPos.z); // 只限制 X
    }

    /* 尝试与最近的交互物体互动 */
    private void TryInteract()
    {
        if (currentInteraction != null)
        {
            Debug.Log($"[PlayerController] 与 {currentInteraction.gameObject.name} 交互");
            currentInteraction.OnInteract(this);
        }
    }

    /* 在编辑器中绘制交互范围 */
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // 绘制交互检测框
        Vector3 center = transform.position + interactionOffset;
        Gizmos.DrawWireCube(center, interactionBoxSize);
    }
}