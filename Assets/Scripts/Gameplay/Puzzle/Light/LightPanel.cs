using UnityEngine;
using UnityEngine.UI;
using Events;

/* 镜槽（MirrorSlot)结构体
 * 用于存储镜槽信息
 */
[System.Serializable]
public struct MirrorSlot
{
    public int xindex;
    public int yindex;
    public float rotation; // 只能是0,45,90,135
    public bool active;
}

/*
 * 光线谜题管理器
 * 管理游戏进度和完成检测,支持静态方法控制面板开关
 */
public class LightPanel : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("根对象（用于显示/隐藏）")]
    public GameObject PanelRoot;

    [Tooltip("格子预制体（如果为空则动态创建）")]
    public GameObject CellPrefab;

    [Tooltip("格子数量 - 列")]
    public int columns = 16;

    [Tooltip("格子数量 - 行")]
    public int rows = 9;

    [Tooltip("格子颜色")]
    public Color cellColor = Color.white;

    [Header("镜槽配置（索引从0开始）")]
    [Tooltip("镜槽数组配置")]
    public MirrorSlot[] mirrorSlots = new MirrorSlot[10];

    [Tooltip("镜槽线段颜色")]
    public Color mirrorLineColor = Color.gray;

    // 静态变量
    private static LightPanel s_instance;
    private static bool s_isOpen;
    private static GameObject s_root;
    private static bool s_initialized = false;

    // 谜题完成标志
    private static bool s_isPuzzleCompleted = false;

    // 存储生成的格子
    public Transform ContentContainer;
    private GameObject[] cells;
    private bool cellsGenerated = false;

    void Awake()
    {
        s_instance = this;

        // 如果未指定根对象,使用当前GameObject
        if (PanelRoot == null)
            PanelRoot = gameObject;

        // 记录静态根,如果根发生变化则重置初始化状态
        if (s_root == null || s_root != PanelRoot)
        {
            s_root = PanelRoot;
            s_initialized = false;
            s_isOpen = false;
            s_isPuzzleCompleted = false; // 重置完成标志
        }

        ContentContainer = transform.Find("Background/Content");

        // 生成格子
        GenerateCells();
    }

    void Start()
    {
        // 初始化时关闭面板
        if (!s_initialized && s_root != null)
        {
            s_initialized = true;
            s_root.SetActive(true);
            Debug.Log("[LightPanel.Start] 光线面板已初始化并打开");
        }
    }

    void OnDestroy()
    {
        // 若当前实例绑定的根等于静态引用则清理静态状态
        if (PanelRoot != null && s_root == PanelRoot)
        {
            s_root = null;
            s_isOpen = false;
            s_initialized = false;
            s_instance = null;
            s_isPuzzleCompleted = false; // 清理完成标志
        }
    }

    /*
     * 生成格子
     */
    private void GenerateCells()
    {
        if (cellsGenerated || ContentContainer == null)
            return;

        int totalCells = columns * rows;
        cells = new GameObject[totalCells];

        for (int i = 0; i < totalCells; i++)
        {
            GameObject cell;

            if (CellPrefab != null)
            {
                // 使用预制体
                cell = Instantiate(CellPrefab, ContentContainer);
            }
            else
            {
                // 动态创建格子
                cell = new GameObject($"Cell_{i}");
                cell.transform.SetParent(ContentContainer, false);

                // 添加Image组件
                Image image = cell.AddComponent<Image>();
                image.color = cellColor;

                // 设置RectTransform
                RectTransform rectTransform = cell.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(89, 89);
            }

            cells[i] = cell;
        }

        // 绘制镜槽
        DrawMirrorSlots();

        cellsGenerated = true;
        Debug.Log($"[LightPanel] 已生成 {totalCells} 个格子（{columns}×{rows}）");
    }

    /*
     * 绘制镜槽
     */
    private void DrawMirrorSlots()
    {
        foreach (var slot in mirrorSlots)
        {
            if (!slot.active)
                continue;

            int index = slot.yindex * columns + slot.xindex;
            if (index < 0 || index >= cells.Length)
                continue;

            GameObject cell = cells[index];
            if (cell == null)
                continue;

            // 在格子上绘制线段
            GameObject line = new GameObject("MirrorLine");
            line.transform.SetParent(cell.transform, false);

            // 添加Image组件作为线段
            Image lineImage = line.AddComponent<Image>();
            lineImage.color = mirrorLineColor;

            // 设置线段的RectTransform
            RectTransform lineRect = line.GetComponent<RectTransform>();
            lineRect.pivot = new Vector2(0.5f, 0.5f);

            // 根据角度调整线段的长度和方向
            if (slot.rotation == 0 || slot.rotation == 90)
            {
                // 垂直或水平线段，充满格子
                lineRect.sizeDelta = new Vector2(5, 89); // 宽度为5，高度为89
            }
            else if (slot.rotation == 45 || slot.rotation == 135)
            {
                // 对角线段，长度为格子的对角线
                float diagonalLength = Mathf.Sqrt(89 * 89 + 89 * 89); // 对角线长度
                lineRect.sizeDelta = new Vector2(5, diagonalLength);
            }

            // 设置旋转角度
            line.transform.localRotation = Quaternion.Euler(0, 0, slot.rotation);

            // 添加 BoxCollider2D 组件
            BoxCollider2D collider = line.AddComponent<BoxCollider2D>();
            collider.size = lineRect.sizeDelta; // 设置碰撞器大小与线段一致
            collider.offset = Vector2.zero; // 碰撞器中心与线段对齐
            // collider.isTrigger = true; // 设置为触发器，避免物理碰撞

            // 设置 Layer 为 "Light"
            line.layer = LayerMask.NameToLayer("Light");
        }
    }
    
    /*
     * 清除所有格子
     */
    private void ClearCells()
    {
        if (ContentContainer == null || cells == null)
            return;

        foreach (GameObject cell in cells)
        {
            if (cell != null)
                Destroy(cell);
        }

        cells = null;
        cellsGenerated = false;
        Debug.Log("[LightPanel] 已清除所有格子");
    }

    /*
     * 谜题完成时调用
     */
    public void OnPuzzleCompleted()
    {
        Debug.Log("[LightPanel] 谜题完成！");

        // 设置完成标志
        s_isPuzzleCompleted = true;

        // 在这里添加完成后的逻辑（例如播放动画、显示提示等）
    }

    // ============ 静态面板控制方法 ============

    /*
     * 切换面板开关状态
     */
    public static void TogglePanel()
    {
        if (s_isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    /*
     * 打开光线谜题面板
     */
    public static void OpenPanel()
    {
        if (s_root == null)
        {
            Debug.LogWarning("[LightPanel] 无法打开面板：根对象为空");
            return;
        }

        s_isOpen = true;
        s_root.SetActive(true);
        Debug.Log("[LightPanel] 面板已打开");

        // 禁用玩家移动
        EventBus.LocalPublish(new FreezeEvent { isOpen = true });
    }

    /*
     * 关闭光线谜题面板
     */
    public static void ClosePanel()
    {
        if (s_root == null)
        {
            Debug.LogWarning("[LightPanel] 无法关闭面板：根对象为空");
            return;
        }

        s_isOpen = false;
        s_root.SetActive(false);
        Debug.Log("[LightPanel] 面板已关闭");

        // 恢复玩家移动
        EventBus.LocalPublish(new FreezeEvent { isOpen = false });
    }

    /*
     * 获取谜题是否已完成
     */
    public static bool IsPuzzleCompleted()
    {
        return s_isPuzzleCompleted;
    }
}