using UnityEngine;
using Mirror;
using System.Collections.Generic;

// 这是一个 Singleton，在场景中唯一
public class HostSaveManager : NetworkBehaviour
{
    public static HostSaveManager Instance { get; private set; }

    // 主机持有的所有客户端的背包状态“缓存”
    // Key: 玩家的网络连接
    // Value: 该玩家*上一次*同步的背包
    private Dictionary<NetworkConnection, List<string>> playerInventoryCache = 
        new Dictionary<NetworkConnection, List<string>>();

    public override void OnStartServer()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    [Server] // 明确标记为服务器方法
    public void UpdatePlayerInventoryCache(NetworkConnection conn, List<string> items)
    {
        if (conn == null) return;
        playerInventoryCache[conn] = items;
        Debug.Log($"[Host] 缓存已更新: 玩家 {conn.connectionId} 现在有 {items.Count} 个物品。");
    }
    
    [Server]
    public void SaveGame()
    {
        Debug.Log("[Host] 正在保存游戏...");
        
        // 1. 创建总存档状态 (来自我上次的设计)
        GameSaveState saveState = new GameSaveState();

        // 2. 保存全局数据 (e.g., Mechanism 状态)
        // ... (遍历所有 [SyncVar] 的 Mechanism) ...

        // 3. 保存所有已缓存的玩家数据
        foreach (var entry in playerInventoryCache)
        {
            NetworkConnection conn = entry.Key;
            List<string> items = entry.Value;
            
            if (conn == null || conn.identity == null) continue;

            // 我们需要从 'conn.identity' 获取玩家的时间线 (Timeline)
            // 假设玩家 prefab 上有 TimelinePlayer.cs 脚本
            var timelinePlayer = conn.identity.GetComponent<TimelinePlayer>();
            if (timelinePlayer == null) continue;

            int timeline = timelinePlayer.timeline; 
            
            var playerData = new PlayerSaveData();
            playerData.timeline = timeline;
            playerData.inventoryItems = new List<InventorySaveItem>();
            
            foreach(string id in items)
            {
                // 您需要一个物品数据库来转换ID
                playerData.inventoryItems.Add(new InventorySaveItem { itemId = id, quantity = 1 });
            }
            
            saveState.allPlayersData.Add(playerData);
        }
        
        // 4. 写入文件 (使用我上次设计的模型)
        // string json = JsonUtility.ToJson(saveState);
        // File.WriteAllText(GetSavePath("slot1"), json);
        Debug.Log($"[Host] 存档已保存，包含 {saveState.allPlayersData.Count} 名玩家的数据。");
    }

    // 5. 玩家断连时，必须清理缓存
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        playerInventoryCache.Remove(conn);
        Debug.Log($"[Host] 玩家 {conn.connectionId} 断开, 已从缓存中移除。");
        base.OnServerDisconnect(conn); // 调用基类
    }
}