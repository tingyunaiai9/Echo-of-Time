/* Core/Services/Network/TimelinePlayer.cs
 * 时间线玩家，每个玩家在不同的时间线中
 */

using Mirror;
using UnityEngine;

public class TimelinePlayer : NetworkBehaviour
{
    [Header("时间线设置")]
    [SyncVar(hook = nameof(OnTimelineChanged))]
    public int timeline = -1;

    [Header("游戏进程")]
    [SyncVar(hook = nameof(OnLevelChanged))]
    public int currentLevel = 1;

    [SyncVar]
    public string playerName = "";

    [SyncVar]
    public uint transportId = 0;

    [Header("可见性")]
    [SyncVar(hook = nameof(OnVisibilityChanged))]
    public bool isVisible = false; // 初始不可见（只能看自己）

    [Server]
    public void ServerSetTimeline(int tl)
    {
        timeline = tl;  // Mirror 会把 SyncVar 同步回该玩家的客户端
    }

    [Command]
    public void CmdChooseRole(int roleIndex)
    {
        // TODO: 这里可做席位校验/冲突检测
        ServerSetTimeline(roleIndex);

        var nm = (EchoNetworkManager)NetworkManager.singleton;
        nm.ServerRememberTimeline(connectionToClient, roleIndex);
    }

    [Command]
    public void CmdReportedCorrectAnswer()
    {
        var nm = (EchoNetworkManager)NetworkManager.singleton;
        nm.ServerPlayerAnsweredCorrectly(this);
    }

    private void OnLevelChanged(int oldLevel, int newLevel)
    {
        Debug.Log($"[TimelinePlayer] {playerName} 的层数从 {oldLevel} 变为 {newLevel}");
        // 仅在本地玩家上重置本地UI（提交按钮从“正确！”恢复为“提交”）
        if (isLocalPlayer)
        {
            DialogPanel.ResetConfirmButtonForNewLevel();
        }
    }

    [Header("玩家信息")]
    private Color[] timelineColors = new Color[]
    {
        Color.red,      // 时间线 0 - 过去
        Color.green,    // 时间线 1 - 现在
        Color.blue      // 时间线 2 - 未来
    };
    
    private EchoNetworkManager networkManager;
    private Renderer playerRenderer;
    
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        
        networkManager = FindFirstObjectByType<EchoNetworkManager>();
        
        // 请求服务器分配时间线
        if (isClient)
        {
            CmdRequestTimeline();
        }
        
        Debug.Log($"[TimelinePlayer] Local player started");

        // 本地玩家始终可见（即使 isVisible=false 也显示自身模型）
        ApplyVisibility();
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // 获取渲染器组件
        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
        }
        
        // 应用时间线颜色
        if (timeline >= 0)
        {
            ApplyTimelineColor();
        }

        // 初始化可见性
        ApplyVisibility();
    }
    
    [Command]
    private void CmdRequestTimeline()
    {
        if (networkManager == null)
        {
            networkManager = FindFirstObjectByType<EchoNetworkManager>();
        }
        
        // 获取 Relay Transport ID
        var relayTransport = networkManager.GetComponent<Unity.Sync.Relay.Transport.Mirror.RelayTransportMirror>();
        if (relayTransport != null)
        {
            var currentPlayer = relayTransport.GetCurrentPlayer();
            if (currentPlayer != null)
            {
                transportId = currentPlayer.TransportId;
                playerName = currentPlayer.Name;
                
                // 从 NetworkManager 获取已分配的时间线
                int assignedTimeline = networkManager.GetPlayerTimeline(transportId);
                
                if (assignedTimeline >= 0)
                {
                    timeline = assignedTimeline;
                    Debug.Log($"[TimelinePlayer] Assigned to timeline {timeline}");
                }
            }
        }
    }
    
    private void OnTimelineChanged(int oldTimeline, int newTimeline)
    {
        Debug.Log($"[TimelinePlayer] Timeline changed from {oldTimeline} to {newTimeline}");
        ApplyTimelineColor();

        SceneDirector.Instance?.TryLoadTimelineNow();
    }

    private void OnVisibilityChanged(bool oldValue, bool newValue)
    {
        ApplyVisibility();
    }
    
    private void ApplyTimelineColor()
    {
        if (playerRenderer != null && timeline >= 0 && timeline < timelineColors.Length)
        {
            // 创建新材质实例以避免修改共享材质
            Material mat = new Material(playerRenderer.material);
            mat.color = timelineColors[timeline];
            playerRenderer.material = mat;
            
            Debug.Log($"[TimelinePlayer] Applied color for timeline {timeline}");
        }
    }

    private void ApplyVisibility()
    {
        // 规则：自身始终可见；其他玩家只有在 isVisible=true 后可见
        bool shouldShow = isLocalPlayer || isVisible;

        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.enabled = shouldShow;
            }
        }
    }

    [Server]
    public void ServerSetVisibility(bool visible)
    {
        isVisible = visible; // SyncVar 同步到所有客户端，触发 hook
    }
    
    void OnGUI()
    {
        if (!isLocalPlayer) return;
        
        // 显示玩家信息
        string info = $"玩家: {playerName}\n";
        info += $"时间线: {GetTimelineName(timeline)}\n";
        info += $"层数: {currentLevel}\n";
        info += $"Transport ID: {transportId}";
        
        GUI.Box(new Rect(10, 10, 200, 100), info);
    }
    
    private string GetTimelineName(int timeline)
    {
        switch (timeline)
        {
            case 0: return "过去 (Past)";
            case 1: return "现在 (Present)";
            case 2: return "未来 (Future)";
            default: return "未分配";
        }
    }
}
