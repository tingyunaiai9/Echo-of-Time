using System.Collections.Generic;
using UnityEngine;
using Events;

/*
 * 道具背包面板：管理 Prop 物品的收集与展示
 */
public class PropBackpack : Inventory
{
    // 物品存储
    readonly Dictionary<string, InventoryItem> _items = new Dictionary<string, InventoryItem>();

    /* 外部调用：添加或更新道具 */
    public void AddOrUpdateItem(InventoryItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemId)) return;

        if (_items.TryGetValue(item.itemId, out var exist))
        {
            exist.quantity += Mathf.Max(1, item.quantity);
        }
        else
        {
            // 深拷贝以避免外部修改
            _items[item.itemId] = new InventoryItem
            {
                itemId = item.itemId,
                itemName = string.IsNullOrEmpty(item.itemName) ? item.itemId : item.itemName,
                description = item.description,
                quantity = Mathf.Max(1, item.quantity)
            };
        }

        // TODO: 刷新 UI 列表（如创建/更新一个道具条目，显示数量）
        Debug.Log($"PropBackpack: 获得道具 [{item.itemId}] x{Mathf.Max(1, item.quantity)}，当前数量 {_items[item.itemId].quantity}");
    }

    /* 订阅拾取事件（来自 prop.cs 的 ItemPickedUpEvent） */
    protected override void Awake()
    {
        base.Awake();
        Debug.Log($"[PropBackpack.Awake] gameObject.activeSelf={gameObject.activeSelf}");
    }

    protected virtual void OnEnable()
    {
        Debug.Log($"[PropBackpack.OnEnable] 被调用");
        EventBus.Instance.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
    }

    protected virtual void OnDisable()
    {
        Debug.Log($"[PropBackpack.OnDisable] 被调用");
        EventBus.Instance.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
    }

    void OnItemPickedUp(ItemPickedUpEvent e)
    {
        // 将拾取事件转为背包条目
        var item = new InventoryItem
        {
            itemId = e.itemId,
            itemName = e.itemId,
            description = "", // 如需可在此映射描述
            quantity = 1
        };
        AddOrUpdateItem(item);
    }
}