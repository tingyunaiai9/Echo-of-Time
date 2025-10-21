/* UI/CluePanel.cs
 * 线索面板控制器，显示解谜相关的提示和线索信息
 */
using System.Collections.Generic;
using UnityEngine;
using Events;

/*
 * 线索面板：继承 Inventory，负责展示与去重管理线索
 */
public class CluePanel : Inventory
{
    // 已收集线索的去重集合
    readonly HashSet<string> _clueIds = new HashSet<string>();

    /* 添加线索（只加入一次） */
    public void AddClue(string clueId, string clueText, Sprite icon = null)
    {
        if (string.IsNullOrEmpty(clueId)) return;
        if (!_clueIds.Add(clueId)) return; // 已存在则忽略

        Debug.Log($"[CluePanel.AddClue] 添加新线索: {clueId}, text: {clueText}, icon: {(icon != null ? icon.name : "null")}");
        // 创建 UI 条目
        var item = new InventoryItem
        {
            itemId = clueId,
            itemName = clueText,
            description = clueText,
            quantity = 1,
            icon = icon
        };
        CreateOrUpdateItemUI(item);
        Debug.Log($"CluePanel: 添加线索 [{clueId}] {clueText}");
    }

    /* 重载：兼容旧接口 */
    public void AddClue(string clueId, string clueText)
    {
        AddClue(clueId, clueText, null);
    }

    /* 订阅线索发现事件 */
    protected override void Awake()
    {
        base.Awake();
        Debug.Log($"[CluePanel.Awake] gameObject.activeSelf={gameObject.activeSelf}");

        // 关键修复：在 Awake 就订阅事件，无论面板是否激活
        EventBus.Instance.Subscribe<ClueDiscoveredEvent>(OnClueDiscovered);
        Debug.Log("[CluePanel.Awake] 已订阅 ClueDiscoveredEvent");
    }

    void OnDestroy()
    {
        // 在销毁时取消订阅
        EventBus.Instance.Unsubscribe<ClueDiscoveredEvent>(OnClueDiscovered);
        Debug.Log("[CluePanel.OnDestroy] 已取消订阅 ClueDiscoveredEvent");
    }

    // protected virtual void OnEnable()
    // {
    //     Debug.Log($"[CluePanel.OnEnable] 被调用");
    //     EventBus.Instance.Subscribe<ClueDiscoveredEvent>(OnClueDiscovered);
    // }

    // protected virtual void OnDisable()
    // {
    //     Debug.Log($"[CluePanel.OnDisable] 被调用");
    //     EventBus.Instance.Unsubscribe<ClueDiscoveredEvent>(OnClueDiscovered);
    // }

    void OnClueDiscovered(ClueDiscoveredEvent e)
    {
        Debug.Log($"[CluePanel.OnClueDiscovered] 收到事件 - clueId: {e.clueId}, text: {e.clueText}, icon: {(e.icon != null ? e.icon.name : "null")}");
        AddClue(e.clueId, e.clueText, e.icon);
    }

    /* 线索详细信息展示（可选） */
    public void DisplayClueDetail(string clueId)
    {
        // TODO: 根据 clueId 在 UI 中显示详细内容
    }

    /* 分享线索（可选） */
    public void HandleClueSharing(string clueId, int targetPlayer)
    {
        // TODO: 网络分享逻辑
    }

    /* 更新线索进度（可选） */
    public void UpdateClueProgress()
    {
        // TODO: 进度统计与 UI 刷新
    }
}