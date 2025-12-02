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

    protected override void Awake()
    {
        base.Awake();
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
                DialogPanel.AddChatImage(tymSprite, timeline);
                Debug.Log("[UIManager] `键按下，添加图片消息。");
            }
            else
            {
                Debug.LogError("[UIManager] 无法加载 Sprite 文件 'tym'，请检查路径和文件名是否正确。");
            }
        }

        // 添加测试线索条目 (Minus键)
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            ClueBoard clueBoardInstance = FindFirstObjectByType<ClueBoard>();
            if (clueBoardInstance != null)
            {
                clueBoardInstance.TestClueEntries();
            }
            Debug.Log("[UIManager] Minus键按下，添加测试线索条目。");
        }

    }

    public void InitializeAllUI()
    {
        // 初始化 UI 面板...
    }
}