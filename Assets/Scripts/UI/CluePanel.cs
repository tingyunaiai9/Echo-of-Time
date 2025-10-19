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
    public new void AddClue(string clueId, string clueText)
    {
        if (string.IsNullOrEmpty(clueId)) return;
        if (!_clueIds.Add(clueId)) return; // 已存在则忽略

        // TODO: 在此生成/更新 UI 元素（例如实例化一条列表项，显示 clueText）
        Debug.Log($"CluePanel: 添加线索 [{clueId}] {clueText}");
    }

    /* 订阅线索发现事件 */
    protected override void Awake()
    {
        base.Awake();
        Debug.Log($"[CluePanel.Awake] gameObject.activeSelf={gameObject.activeSelf}");
    }

    protected virtual void OnEnable()
    {
        Debug.Log($"[CluePanel.OnEnable] 被调用");
        EventBus.Instance.Subscribe<ClueDiscoveredEvent>(OnClueDiscovered);
    }

    protected virtual void OnDisable()
    {
        Debug.Log($"[CluePanel.OnDisable] 被调用");
        EventBus.Instance.Unsubscribe<ClueDiscoveredEvent>(OnClueDiscovered);
    }

    void OnClueDiscovered(ClueDiscoveredEvent e)
    {
        AddClue(e.clueId, e.clueText);
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