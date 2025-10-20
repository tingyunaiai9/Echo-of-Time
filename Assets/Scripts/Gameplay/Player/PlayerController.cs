using UnityEngine;
using Mirror;

/// <summary>
/// Mirror 网络玩家控制器，仅本地玩家处理输入与物理。
/// </summary>
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

    Rigidbody rb;
    bool initialized;

    // 新增：背包打开时禁用游戏输入
    bool isBackpackOpen;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
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

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        if (rb != null) rb.isKinematic = false;
        initialized = true;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isLocalPlayer && rb != null)
        {
            rb.isKinematic = true;
        }
    }

    // 新增：本地玩家启动时订阅背包事件
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Inventory.OnBackpackStateChanged += OnBackpackStateChanged;
    }

    // 新增：销毁时取消订阅
    void OnDestroy()
    {
        if (isLocalPlayer)
        {
            Inventory.OnBackpackStateChanged -= OnBackpackStateChanged;
        }
    }

    // 新增：背包状态变化回调
    void OnBackpackStateChanged(bool isOpen)
    {
        isBackpackOpen = isOpen;
        Debug.Log($"[PlayerController] 背包状态变化: {(isOpen ? "打开" : "关闭")}，游戏输入: {(isOpen ? "禁用" : "启用")}");
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // B 键始终可用（用于开关背包）
        if (Input.GetKeyDown(KeyCode.B))
        {
            Inventory.ToggleBackpack();
        }

        // 背包打开时，禁用游戏输入（移动、交互等）
        if (isBackpackOpen) return;

        // 以下是原有的游戏输入逻辑
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v).normalized;

        if (input.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(input);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        if (rb == null)
        {
            transform.Translate(input * moveSpeed * Time.deltaTime, Space.World);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryInteract();
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        
        // 背包打开时，禁用物理移动
        if (isBackpackOpen) return;

        if (rb != null)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 input = new Vector3(h, 0f, v).normalized;
            Vector3 velocity = input * moveSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
    }

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}