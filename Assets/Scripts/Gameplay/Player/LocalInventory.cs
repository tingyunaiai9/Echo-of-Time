using UnityEngine;
using System.Collections.Generic;

// 挂在玩家 Prefab 上。这是一个纯本地脚本。
public class LocalInventory : MonoBehaviour
{
    // 1. 客户端权威的本地背包
    private List<string> localItemIDs = new List<string>();

    // 2. 交互脚本 (prop.cs) 会调用这个本地方法
    public void AddItem(string itemID)
    {
        localItemIDs.Add(itemID);
        Debug.Log($"[Client-Local] 物品 '{itemID}' 已添加到本地背包。");
        
        // 在这里更新你的本地 UI (例如调用 Inventory.cs 的静态方法)
        // Inventory.AddClue(itemID, "新线索", null);
    }
    
    // 3. 获取当前背包状态 (用于发送给主机)
    public List<string> GetCurrentItems()
    {
        // 返回一个副本，防止在网络传输时被修改
        return new List<string>(localItemIDs);
    }

    // 4. (读档时需要)
    public void LoadItems(List<string> items)
    {
        localItemIDs = new List<string>(items);
        Debug.Log($"[Client-Local] 已从存档恢复 {items.Count} 个物品。");
        // (在这里刷新UI)
    }
}