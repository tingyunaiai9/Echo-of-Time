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

        // 诗词谜题 (F2键)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            PoemManager.TogglePanel();
            Debug.Log("[UIManager] F2键按下，切换诗词谜题页面。");
        }

        // 光线谜题 (F3键)
        if (Input.GetKeyDown(KeyCode.F3))
        {
            LightPanel.TogglePanel();
            Debug.Log("[UIManager] F3键按下，切换光线谜题页面。");
        }

        // 拼画谜题 (F4键)
        if (Input.GetKeyDown(KeyCode.F4))
        {
            PuzzlePanel.TogglePanel();
            Debug.Log("[UIManager] F4键按下，切换拼画谜题页面。");
        }

        // 添加测试聊天消息 (Plus键)
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadEquals))
        {
            DialogPanel.AddChatMessage(
                "托马斯·库恩在《科学革命的结构》中提出的范式理论，深刻重构了科学演进的理解框架。本书第三章《常规科学的本质》与第九章《科学革命的本质与必然性》分别从科学实践的稳定性和变革性两个维度展开论述，系统揭示了范式在科学活动中的核心作用。", 
                MessageType.Modern);
            Debug.Log("[UIManager] Equals键按下，添加测试聊天消息。");
        }

        // 添加测试线索条目 (Minus键)
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            ClueBoard.AddClueEntry("这是一个测试线索条目，记录玩家的发现。");
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