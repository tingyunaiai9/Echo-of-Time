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
    private EventBus eventBus;

    void Awake()
    {
        // 获取或创建全局EventBus实例（可根据实际项目结构调整）
        eventBus = EventBus.Instance;
        if (eventBus != null)
        {
            eventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
        }
        else
        {
            Debug.LogWarning("UIManager: 未找到EventBus实例，无法监听事件。");
        }
    }

    void OnDestroy()
    {
        if (eventBus != null)
        {
            eventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
        }
    }

    // 监听物品拾取事件，Debug.Log模拟弹窗
    private void OnItemPickedUp(ItemPickedUpEvent evt)
    {
        Debug.Log($"[UIManager] 玩家 {evt.playerNetId} 拾取了物品 {evt.itemId}，弹窗提醒！");
        ShowPopup($"获得物品：{evt.itemId}");
    }

    // 简单弹窗方法（实际项目可替换为UI面板控制）
    private void ShowPopup(string message)
    {
        // 示例：用Debug.Log模拟弹窗，实际应弹出UI面板
        Debug.Log($"[UI弹窗] {message}");
    }

    /* 初始化所有UI系统 */
    public void InitializeAllUI()
    {
        // 按顺序初始化UI组件
        // 建立UI事件总线
        // 设置UI层级管理
    }

    /* 管理UI层级显示 */
    public void ManageUILayers(UIPanel panel, UILayer layer)
    {
        // 验证层级权限
        // 调整显示顺序
        // 处理层级冲突
    }

    /* 处理UI模式切换 */
    public void SwitchUIMode(UIMode newMode)
    {
        // 保存当前模式状态
        // 执行模式切换动画
        // 更新输入处理逻辑
    }

    /* 协调跨时间线UI同步 */
    public void SyncUITimelineState(int timelineId)
    {
        // 更新时间线相关UI
        // 同步玩家状态显示
        // 处理UI状态冲突
    }
}