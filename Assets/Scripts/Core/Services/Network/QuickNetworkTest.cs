/* Core/Services/Network/QuickNetworkTest.cs
 * 快速网络测试，使用键盘快捷键测试网络功能
 * 按键：
 * H - 创建房间 (Host)
 * C - 加入房间 (Client)  
 * L - 离开房间
 */

using UnityEngine;
using Mirror;

public class QuickNetworkTest : MonoBehaviour
{
    private EchoNetworkManager networkManager;
    private bool isConnecting = false;
    // 移除重试与冗余验证，仅保留核心信息输出
    
    [Header("测试设置")]
    [SerializeField] private string defaultRoomName = "TestRoom";
    [SerializeField] private string roomCodeToJoin = ""; // 用于测试房间代码加入
    
    void Start()
    {
        networkManager = FindFirstObjectByType<EchoNetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("[QuickNetworkTest] NetworkManager not found!");
        }
    }
    
    void Update()
    {
        if (networkManager == null || isConnecting) return;
        
        // H - 创建房间 (Host)
        if (Input.GetKeyDown(KeyCode.H))
        {
            CreateTestRoom();
        }
        
        // C - 加入房间 (Client)
        if (Input.GetKeyDown(KeyCode.C))
        {
            JoinTestRoom();
        }
        
        // L - 离开房间
        if (Input.GetKeyDown(KeyCode.L))
        {
            LeaveRoom();
        }
        
        // R - 显示房间信息
        if (Input.GetKeyDown(KeyCode.R))
        {
            ShowRoomInfo();
        }
        
        // J - 通过房间代码加入（需要先在 Inspector 中设置房间代码）
        if (Input.GetKeyDown(KeyCode.J))
        {
            JoinByRoomCode();
        }
    }
    
    private void CreateTestRoom()
    {
        Debug.Log("[QuickNetworkTest] Creating test room...");
        isConnecting = true;
        
        
        string roomName = defaultRoomName + "-" + Random.Range(1000, 9999);
        networkManager.CreateRoom(roomName, (success, message) =>
        {
            isConnecting = false;
            if (success)
            {
                Debug.Log($"[QuickNetworkTest] ✓ Room created: {message}");
                ShowRoomInfo();
            }
            else
            {
                Debug.LogError($"[QuickNetworkTest] ✗ Failed: {message}");
            }
        });
    }
    
    private void JoinTestRoom()
    {
        Debug.Log("[QuickNetworkTest] Searching for rooms...");
        isConnecting = true;
        
        
        networkManager.JoinRoom((success, message) =>
        {
            isConnecting = false;
            if (success)
            {
                Debug.Log($"[QuickNetworkTest] ✓ Joined room: {message}");
                ShowRoomInfo();
            }
            else
            {
                Debug.LogError($"[QuickNetworkTest] ✗ Failed: {message}");
            }
        });
    }
    
    private void LeaveRoom()
    {
        Debug.Log("[QuickNetworkTest] Leaving room...");
        networkManager.LeaveRoom();
        Debug.Log("[QuickNetworkTest] Left room");
    }
    
    private void JoinByRoomCode()
    {
        if (string.IsNullOrEmpty(roomCodeToJoin))
        {
            Debug.LogWarning("[QuickNetworkTest] Room code is empty! Please set it in Inspector.");
            return;
        }
        
        Debug.Log($"[QuickNetworkTest] Joining room by code: {roomCodeToJoin}");
        isConnecting = true;
        
        networkManager.JoinRoomByCode(roomCodeToJoin, (success, message) =>
        {
            isConnecting = false;
            if (success)
            {
                Debug.Log($"[QuickNetworkTest] ✓ Joined room: {message}");
                ShowRoomInfo();
            }
            else
            {
                Debug.LogError($"[QuickNetworkTest] ✗ Failed: {message}");
            }
        });
    }
    
    private void ShowRoomInfo()
    {
        var room = networkManager.GetCurrentRoom();
        var player = networkManager.GetCurrentPlayer();
        
        if (room != null)
        {
            Debug.Log("========== 房间信息 ==========");
            Debug.Log($"房间名称: {room.Name}");
            Debug.Log($"房间ID: {room.ID}");
            Debug.Log($"房间代码: {room.RoomCode}");
            Debug.Log($"玩家数量: {room.Players.Count}/3");
            Debug.Log($"状态: {room.Status}");
            
            Debug.Log("\n玩家列表:");
            foreach (var kvp in room.Players)
            {
                var p = kvp.Value;
                int timeline = networkManager.GetPlayerTimeline(p.TransportId);
                string timelineStr = timeline >= 0 ? $"[时间线 {timeline}]" : "[未分配]";
                bool isMe = player != null && p.TransportId == player.TransportId;
                string marker = isMe ? " ← (你)" : "";
                Debug.Log($"  • {p.Name} {timelineStr}{marker}");
            }
            Debug.Log("==============================");
        }
        else
        {
            Debug.Log("[QuickNetworkTest] Room info not available yet. Ensure Relay connected and try again.");
        }
    }
    
    void OnGUI()
    {
        // 显示房间代码（如果在房间中）
        if (NetworkClient.isConnected || NetworkServer.active)
        {
            var room = networkManager.GetCurrentRoom();
            if (room != null)
            {
                GUILayout.BeginArea(new Rect(Screen.width - 210, 10, 200, 100));
                GUILayout.Box($"房间: {room.Name}\n房间代码: {room.RoomCode}\n玩家: {room.Players.Count}/3");
                GUILayout.EndArea();
            }
        }
    }
}
