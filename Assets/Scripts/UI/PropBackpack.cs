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
                itemName = string.IsNullOrEmpty(item.itemName) ? item.itemId : item.itemName,
                description = item.description,
                quantity = Mathf.Max(1, item.quantity),
                icon = item.icon
            };
            _items[item.itemId] = newItem;
            Debug.Log($"[PropBackpack.AddOrUpdateItem] 添加新物品: {newItem.itemName}, 数量: {newItem.quantity}");
            CreateOrUpdateItemUI(newItem);
        }

        Debug.Log($"PropBackpack: 获得道具 [{item.itemId}] x{Mathf.Max(1, item.quantity)}，当前数量 {_items[item.itemId].quantity}");
    }

    /* 使用静态构造函数确保在场景加载前就订阅 */
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EarlySubscribe()
    {
        Debug.Log("[PropBackpack] 静态初始化：准备订阅事件");
        // 等待场景加载完成后再订阅
    }

    /* 订阅拾取事件（来自 prop.cs 的 ItemPickedUpEvent） */
    protected override void Awake()
    {
        base.Awake();
        Debug.Log($"[PropBackpack.Awake] 开始 - gameObject: {gameObject.name}, activeSelf: {gameObject.activeSelf}, activeInHierarchy: {gameObject.activeInHierarchy}");
        
        // 关键修复：在 Awake 就订阅事件，无论面板是否激活
        EventBus.Instance.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
        Debug.Log("[PropBackpack.Awake] 已订阅 ItemPickedUpEvent");
    }

    void OnDestroy()
    {
        // 在销毁时取消订阅
        EventBus.Instance.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
        Debug.Log("[PropBackpack.OnDestroy] 已取消订阅 ItemPickedUpEvent");
    }

    void OnItemPickedUp(ItemPickedUpEvent e)
    {
        Debug.Log($"[PropBackpack.OnItemPickedUp] 收到事件 - itemId: {e.itemId}, icon: {(e.icon != null ? e.icon.name : "null")}");
        
        // 将拾取事件转为背包条目
        var item = new InventoryItem
        {
            itemId = e.itemId,
            itemName = e.itemId,
            description = "",
            quantity = 1,
            icon = e.icon
        };
        AddOrUpdateItem(item);
    }
}