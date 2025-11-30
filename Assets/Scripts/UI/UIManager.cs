using UnityEngine;
using Events; // 引入事件命名空间

public enum UIPanel
{
    MainMenu,
    InGameHUD,
    PauseMenu,
    Inventory,
    Settings,
    Dialogue,
    Cutscene
}

public enum UILayer
{
    Background = 0,
    Default = 1,
    Popup = 2,
    Overlay = 3,
    TopMost = 4
}

public enum UIMode
{
    Exploration,
    Combat,
    Dialogue,
    Cutscene
}

/*
 * UI管理器，协调所有UI系统的显示和交互
 */
public class UIManager : Singleton<UIManager>
{
    protected override void Awake()
    {
        base.Awake();
        // 订阅物品拾取事件
        EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
        InitializeAllUI();
    }

    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
        base.OnDestroy();
    }

    /* 监听物品拾取事件，Debug.Log模拟弹窗 */
    private void OnItemPickedUp(ItemPickedUpEvent evt)
    {
        Debug.Log($"[UIManager] 玩家 {evt.playerNetId} 拾取了物品 {evt.itemId}，弹窗提醒！");
    }

    /* 每帧更新 */
    void Update()
    {
        HandleUIInput();
    }

    /* 处理所有 UI 相关的按键 */
    private void HandleUIInput()
    {
        // 背包开关 (B键)
        if (Input.GetKeyDown(KeyCode.B))
        {
            Inventory.ToggleBackpack();
            Debug.Log("[UIManager] B键按下，切换背包。");
        }

        // 日记页面切换 (F1键)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Diary.TogglePanel();
            Debug.Log("[UIManager] F1键按下，切换日记页面。");
        }




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
    
    public void ManageUILayers(UIPanel panel, UILayer layer)
    {
        // 验证层级权限
        // 调整显示顺序
        // 处理层级冲突
    }

    public void SwitchUIMode(UIMode newMode)
    {
        // 保存当前模式状态
        // 执行模式切换动画
        // 更新输入处理逻辑
    }

    public void SyncUITimelineState(int timelineId)
    {
        // 更新时间线相关UI
        // 同步玩家状态显示
        // 处理UI状态冲突
    }
}