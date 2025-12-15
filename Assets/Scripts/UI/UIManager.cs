using UnityEngine;
using Events; // 引入事件命名空间

/*
 * UI管理器，协调所有UI系统的显示和交互
 */
public class UIManager : Singleton<UIManager>
{
    [Tooltip("日记界面游戏对象")]
    public GameObject DiaryPanel;
    [Tooltip("背包界面游戏对象")]
    public GameObject InventoryPanel;
    [Tooltip("指南界面游戏对象")]
    public GameObject TipPanel;

    [Tooltip("主 UI 画布（包含日记按钮等常驻 UI），用于在谜题中隐藏")]
    public Canvas mainCanvas;

    protected override void Awake()
    {
        base.Awake();
        // 尝试自动获取 Canvas
        if (mainCanvas == null)
        {
            mainCanvas = GetComponent<Canvas>();
            if (mainCanvas == null)
            {
                // 尝试在父级查找
                mainCanvas = GetComponentInParent<Canvas>();
            }
        }
        InitializeAllUI();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    /* 每帧更新 */
    void Update()
    {
        HandleUIInput();
        TestUI();
    }

    // 供UI组件调用以触发冻结事件
    public void EmitFreezeEvent(bool isOpen)
    {
        EventBus.LocalPublish(new FreezeEvent { isOpen = isOpen });
    }

    /// <summary>
    /// 设置主 UI（如日记按钮）的可见性/交互性
    /// </summary>
    public void SetMainUIActive(bool active)
    {
        if (mainCanvas != null)
        {
            mainCanvas.enabled = active;
            Debug.Log($"[UIManager] SetMainUIActive: {active}");
        }
        else
        {
            // 如果没有 Canvas，尝试禁用 DiaryPanel 的父级或者其他处理
            // 这里做一个简单的 fallback，如果 DiaryPanel 存在，禁用它的父物体（假设是 Canvas）
            if (DiaryPanel != null && DiaryPanel.transform.parent != null)
            {
                var parentCanvas = DiaryPanel.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    parentCanvas.enabled = active;
                    Debug.Log($"[UIManager] SetMainUIActive (via DiaryPanel parent): {active}");
                }
            }
        }
    }

    /// <summary>
    /// 关闭日记面板
    /// </summary>
    public void CloseDiary()
    {
        if (DiaryPanel != null && DiaryPanel.activeSelf)
        {
            DiaryPanel.SetActive(false);
            EventBus.LocalPublish(new FreezeEvent { isOpen = false });
            Debug.Log("[UIManager] CloseDiary called.");
        }
    }

    /* 处理所有 UI 相关的按键 */
    private void HandleUIInput()
    {
        // 背包开关 (B键)
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (InventoryPanel != null)
            {
                bool isActive = InventoryPanel.activeSelf;
                if (!isActive)
                {
                    InventoryPanel.SetActive(true);
                    Inventory.SwitchToProps();
                }
                else
                {
                    InventoryPanel.SetActive(false);
                }
            }
            EventBus.LocalPublish(new FreezeEvent { isOpen = InventoryPanel.activeSelf });
            Debug.Log("[UIManager] B键按下，切换背包。");
        }

        // 日记页面切换 (F1键)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (DiaryPanel != null)
            {
                bool isActive = DiaryPanel.activeSelf;
                DiaryPanel.SetActive(!isActive);
            }
            EventBus.LocalPublish(new FreezeEvent { isOpen = DiaryPanel.activeSelf });
            Debug.Log("[UIManager] F1键按下，切换日记页面。");
        }

        // 指南开关（H键）
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (TipPanel!= null)
            {
                bool isActive = TipPanel.activeSelf;
                TipPanel.SetActive(!isActive);
            }
            EventBus.LocalPublish(new FreezeEvent { isOpen = TipPanel.activeSelf });
            Debug.Log("[UIManager] H键按下，切换指南页面。");
        }
    }

    private void TestUI()
    {
        // 添加测试聊天消息 (Equals键)
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadEquals))
        {
            // 获取当前玩家的时间线
            int timeline = TimelinePlayer.Local.timeline;
            DialogPanel.AddChatMessage(
                "两只黄鹂鸣翠柳，一行白鹭上青天。",
                timeline);
            Debug.Log("[UIManager] Equals键按下，添加测试聊天消息。");
        }

        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            Sprite tymSprite = Resources.Load<Sprite>("tym");
            if (tymSprite != null)
            {
                // 获取当前玩家的时间线
                int timeline = TimelinePlayer.Local.timeline;
                // 压缩图片，避免过大
                byte[] spriteBytes = ImageUtils.CompressSpriteToJpegBytes(tymSprite, 60); // 可调整quality
                if (spriteBytes != null)
                {
                    DialogPanel.AddChatImage(spriteBytes, timeline);
                    Debug.Log("[UIManager] `键按下，添加压缩后的图片消息。");
                    Debug.Log($"[UIManager] 图片大小：{spriteBytes.Length} 字节");
                }
                else
                {
                    Debug.LogError("[UIManager] 图片压缩失败。");
                }
            }
            else
            {
                Debug.LogError("[UIManager] 无法加载 Sprite 文件 'tym'，请检查路径和文件名是否正确。");
            }
        }
        // 添加测试线索条目 (Minus键)
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            Debug.Log("[UIManager] Minus键按下，添加测试线索条目。");
            
            Sprite sprite = Resources.Load<Sprite>("Clue_Poem1");
            int timeline = TimelinePlayer.Local.timeline;
            // 压缩图片，避免过大
            byte[] spriteBytes = ImageUtils.CompressSpriteToJpegBytes(sprite, 80);
            Debug.Log($"[UIManager] 线索图片压缩成功，大小：{spriteBytes.Length} 字节");
            ClueBoard.AddClueEntry(timeline, spriteBytes);
        }
    }

    public void InitializeAllUI()
    {
        // 初始化 UI 面板...
    }
}