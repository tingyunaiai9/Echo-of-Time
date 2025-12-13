using System.Collections.Generic;
using UnityEngine;
using Events;

/*
 * 道具背包面板：管理 Prop 物品的收集与展示
 */
public class PropBackpack : Inventory
{
    static readonly Dictionary<string, InventoryItem> _items = new Dictionary<string, InventoryItem>();

    /* 添加或更新道具 */
    public void AddOrUpdateItem(InventoryItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemId))
        {
            Debug.LogWarning("[PropBackpack.AddOrUpdateItem] item 为 null 或 itemId 为空");
            return;
        }

        Debug.Log($"[PropBackpack.AddOrUpdateItem] 收到物品: {item.itemId}, icon: {(item.icon != null ? item.icon.name : "null")}");

        if (_items.TryGetValue(item.itemId, out var exist))
        {
            exist.quantity += Mathf.Max(1, item.quantity);
            Debug.Log($"[PropBackpack.AddOrUpdateItem] 更新已有物品，新数量: {exist.quantity}");
            CreateOrUpdateItemUI(exist);
        }
        else
        {
            // 深拷贝以避免外部修改
            var newItem = new InventoryItem
            {
                itemId = item.itemId,
                description = item.description,  // 保存描述
                quantity = Mathf.Max(1, item.quantity),
                icon = item.icon
            };
            _items[item.itemId] = newItem;
            Debug.Log($"[PropBackpack.AddOrUpdateItem] 添加新物品: {newItem.itemId}, 数量: {newItem.quantity}");
            CreateOrUpdateItemUI(newItem);
        }
    }

    /* 使用静态构造函数确保在场景加载前就订阅 */
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EarlySubscribe()
    {
        Debug.Log("[PropBackpack] 静态初始化：准备订阅事件");
    }

    /* 订阅拾取事件（来自 prop.cs 的 ItemPickedUpEvent） */
    protected override void Awake()
    {
        base.Awake();
        // 在 Awake 就订阅事件，无论面板是否激活
        EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
        Debug.Log("[PropBackpack.Awake] 已订阅 ItemPickedUpEvent");
    }

    /* 在销毁时取消订阅 */
    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
        Debug.Log("[PropBackpack.OnDestroy] 已取消订阅 ItemPickedUpEvent");
        base.OnDestroy();
    }

    /* 处理物品拾取事件 */
    void OnItemPickedUp(ItemPickedUpEvent e)
    {
        Debug.Log($"[PropBackpack.OnItemPickedUp] 收到事件 - itemId: {e.itemId}, quantity: {e.quantity}, icon: {(e.icon != null ? e.icon.name : "null")}");
        
        // 将拾取事件转为背包条目
        var item = new InventoryItem
        {
            itemId = e.itemId,
            description = e.description,
            quantity = Mathf.Max(1, e.quantity), // 强制最小数量为 1
            icon = e.icon
        };
        AddOrUpdateItem(item);
    }

    public static int GetPropCount(string itemId)
    {
        if (_items.TryGetValue(itemId, out var item))
        {
            return item.quantity;
        }
        return 0;
    }
}