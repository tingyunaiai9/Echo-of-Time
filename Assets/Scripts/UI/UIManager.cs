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

    protected override void Awake()
    {
        base.Awake();
        // 订阅物品拾取事件
        EventBus.Instance.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
        InitializeAllUI();
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
    }

    
    public void InitializeAllUI()
    {
        // 主动查找并激活关键 UI 面板，确保 Awake 执行
        var diaryPanel = GameObject.Find("DiaryPanel");
        if (diaryPanel != null)
        {
            var diary = diaryPanel.GetComponent<Diary>();
            if (diary != null)
            {
                // 可选：先激活再关闭，确保 Awake 执行
                diaryPanel.SetActive(true);
                diaryPanel.SetActive(false);
                Debug.Log("[UIManager] DiaryPanel 初始化完成");
            }
        }

        var inventoryPanel = GameObject.Find("BackpackRoot");
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            inventoryPanel.SetActive(false);
            Debug.Log("[UIManager] InventoryPanel 初始化完成");
        }

        // 可继续初始化其他 UI 面板...
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