using UnityEngine;
using Mirror;

// 挂在玩家 Prefab 上，与 LocalInventory 挨着
[RequireComponent(typeof(LocalInventory))]
public class PlayerSyncController : NetworkBehaviour
{
    private LocalInventory localInventory;

    public override void OnStartLocalPlayer()
    {
        // 获取本地背包
        localInventory = GetComponent<LocalInventory>();
    }

    // 假设 "SavePoint" 是一个 tag
    [ClientCallback] // 仅在客户端运行
    private void OnTriggerEnter(Collider other)
    {
        if (!isLocalPlayer) return; // 只关心本地玩家的触发

        if (other.CompareTag("SavePoint"))
        {
            Debug.Log("[Client] 触碰存档点！正在向主机同步本地背包...");
            
            // 1. 从本地背包获取当前状态
            List<string> items = localInventory.GetCurrentItems();
            
            // 2. 发送Command给主机
            CmdSyncMyInventory(items);
        }
    }

    // 3. [Command] 必须在 NetworkBehaviour 上
    [Command]
    void CmdSyncMyInventory(List<string> clientInventory)
    {
        // 4. 此代码在主机(Host)上运行
        Debug.Log($"[Host] 收到来自 {connectionToClient.connectionId} 的背包数据。正在缓存...");
        
        // 5. 主机找到全局管理器，并缓存这个数据
        // 我们把 'connectionToClient' (发送命令的玩家) 作为 Key
        HostSaveManager.Instance.UpdatePlayerInventoryCache(connectionToClient, clientInventory);
    }
}