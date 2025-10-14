/* UI/UIManager.cs
 * UI管理器，统一管理所有UI面板的显示层级和状态
 * 协调不同UI界面之间的切换和交互
 */
using UnityEngine;

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
public class UIManager : MonoBehaviour
{
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