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

    [Header("运动约束")]
    [Tooltip("背景物体的 Tag，用于限定左右范围")]
    public string backgroundTag = "Background";
    [Tooltip("左右范围外额外留白(世界单位)")]
    public float horizontalPadding = 0.2f;

    private float minX;
    private float maxX;
    private bool hasBounds;
    Rigidbody rb;
    bool initialized;

    /* 背包打开时禁用游戏输入 */
    bool isBackpackOpen;

    /* 初始化刚体和相机 */
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
            // 冻结不使用的轴（不允许前后(Z) 与上下(Y) 位移）
            rb.constraints = RigidbodyConstraints.FreezeRotation |
                             RigidbodyConstraints.FreezePositionZ |
                             RigidbodyConstraints.FreezePositionY;
        }

        // AcquireBackgroundBounds();

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
                Debug.Log($"[PlayerController] 背景范围设置: {minX} ~ {maxX}");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerController] 未找到背景(Tag=Background)，不进行水平范围限制。");
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

    /* 本地玩家启动时订阅背包事件 */
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        EventBus.Subscribe<FreezeEvent>(OnBackpackStateChanged);

        AcquireBackgroundBounds();
        
    }

    /* 销毁时取消订阅 */
    void OnDestroy()
    {
        if (isLocalPlayer)
        {
            EventBus.Unsubscribe<FreezeEvent>(OnBackpackStateChanged);
        }
    }

    /* 背包状态变化回调 */
    void OnBackpackStateChanged(FreezeEvent e)
    {
        isBackpackOpen = e.isOpen;
    }

    /* 处理玩家输入和移动 */
    void Update()
    {
        if (!isLocalPlayer) return;

        // 背包打开时，禁用游戏输入（移动、交互等）
        if (isBackpackOpen) return;

        // 游戏输入逻辑（移动、旋转）
        float h = Input.GetAxisRaw("Horizontal");
        // 禁止前后：垂直输入直接忽略
        float v = 0f;

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(new Vector3(input.x, 0f, 0f));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

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
            Vector3 velocity = new Vector3(h * moveSpeed, rb.linearVelocity.y, 0f);
            rb.linearVelocity = velocity;
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
            Vector3 smoothedPosition = Vector3.Lerp(cameraTransform.position, desiredPosition, cameraSmoothSpeed);
            cameraTransform.position = smoothedPosition;
        }
    }

    /* 尝试与最近的交互物体互动 */
    private void TryInteract()
    {
        // 首先检查是否要打开谜题
        Collider[] puzzleColliders = Physics.OverlapSphere(transform.position, interactionRange, interactableLayer);
        if (puzzleColliders.Length > 0)
        {
            InteractToOpenPuzzle bestPuzzle = null;
            float closestPuzzleDistSqr = float.MaxValue;

            foreach (var hitCollider in puzzleColliders)
            {
                InteractToOpenPuzzle puzzle = hitCollider.GetComponent<InteractToOpenPuzzle>();
                if (puzzle != null)
                {
                    float distSqr = (hitCollider.transform.position - transform.position).sqrMagnitude;
                    if (distSqr < closestPuzzleDistSqr)
                    {
                        closestPuzzleDistSqr = distSqr;
                        bestPuzzle = puzzle;
                    }
                }
            }

            if (bestPuzzle != null)
            {
                Debug.Log($"找到谜题交互点: {bestPuzzle.gameObject.name}, 尝试打开。");
                bestPuzzle.OpenPuzzle();
                return; // 优先处理谜题，不再继续查找其他交互
            }
        }


        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRange, interactableLayer);

        if (hitColliders.Length > 0)
        {
            Interaction best = null;
            float closestDistanceSqr = float.MaxValue;
            Vector3 playerPosition = transform.position;

            foreach (var hitCollider in hitColliders)
            {
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
        }
    }

    /* 在编辑器中绘制交互范围 */
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}