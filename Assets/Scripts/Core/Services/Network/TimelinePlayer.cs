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

    // 本地玩家的单例引用，方便全局访问
    public static TimelinePlayer Local { get; private set; }

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
        
        // 同时更新 NetworkManager 中的 playerTimelineMap，防止重连或场景切换后丢失
        nm.ServerRegisterTimelineSelection(transportId, roleIndex);
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
            DialogPanel.Reset();
            KeyPanel.Reset();
            ResultPanel.Reset();
            ClueBoard.Reset();
            SceneDirector.Instance?.TryLoadTimelineNow();
            UIManager.Instance?.CloseDiary();

            // 注意：位置重置逻辑已移交至 SceneDirector 在场景加载完成后调用
            // 避免因 OnLevelChanged 触发时机早于场景加载而导致的问题
        }
    }

    // 供外部（如 SceneDirector）调用，在场景加载完成后重置玩家位置
    public void TriggerResetPosition()
    {
        StartCoroutine(ResetPlayerPosition());
    }

    private System.Collections.IEnumerator ResetPlayerPosition()
    {
        // 等待一帧，确保场景加载开始或完成（如果是同步加载）
        // 由于 SceneDirector 是异步加载，这里可能需要更稳健的等待逻辑
        // 但通常 SpawnPoint 是随场景加载的，我们可以尝试轮询几次
        
        Transform spawnPoint = null;
        float timeout = 5f;
        float timer = 0f;

        while (spawnPoint == null && timer < timeout)
        {
            var obj = GameObject.Find("SpawnPoint");
            if (obj != null) spawnPoint = obj.transform;
            
            if (spawnPoint == null) yield return null;
            timer += Time.deltaTime;
        }

        if (spawnPoint != null)
        {
            Vector3 newPos = transform.position;
            newPos.x = spawnPoint.position.x;
            // 保持 Y 和 Z 不变，或者根据需求调整
            transform.position = newPos;
            Debug.Log($"[TimelinePlayer] Player position reset to SpawnPoint X: {newPos.x}");
        }
        else
        {
            Debug.LogWarning("[TimelinePlayer] SpawnPoint not found in new scene.");
        }
    }

    [Header("玩家信息")]
    private EchoNetworkManager networkManager;

    [Header("视觉表现")]
    // 在这里把 Skin_Past, Skin_Present, Skin_Future 拖进去
    public GameObject[] timelineSkins; 

    // 缓存当前激活的渲染器，用于控制可见性
    private Renderer currentActiveRenderer;
    
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        
        // 注册全局单例
        Local = this;

        networkManager = FindFirstObjectByType<EchoNetworkManager>();
        
        // 请求服务器分配时间线
        if (isClient)
        {
            string myName = "";
            uint myId = 0;

            // 获取本地 Relay 信息并发送给服务器
            var relayTransport = networkManager.GetComponent<Unity.Sync.Relay.Transport.Mirror.RelayTransportMirror>();
            if (relayTransport != null)
            {
                var currentPlayer = relayTransport.GetCurrentPlayer();
                if (currentPlayer != null)
                {
                    myName = currentPlayer.Name;
                    myId = currentPlayer.TransportId;
                }
            }

            CmdRequestTimeline(myName, myId);
        }
        
        Debug.Log($"[TimelinePlayer] Local player started");

        // 本地玩家始终可见（即使 isVisible=false 也显示自身模型）
        UpdateVisibilityStatus();
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        // 初始化：根据当前时间线显示正确的皮肤
        RefreshVisuals();
    }

    private void OnDestroy()
    {
        // 清理单例引用
        if (Local == this)
        {
            Local = null;
        }
    }
    
    [Command]
    private void CmdRequestTimeline(string clientName, uint clientId)
    {
        if (networkManager == null)
        {
            networkManager = FindFirstObjectByType<EchoNetworkManager>();
        }
        
        // 如果传入了有效的 Relay 信息，则更新
        if (clientId != 0)
        {
            transportId = clientId;
            // 保持服务器分配的 "Player N" 名字，不使用 Relay 中的 Guest 名字覆盖
            // playerName = clientName; 
            
            Debug.Log($"[TimelinePlayer] Linked Relay ID: {transportId}. Relay Name: {clientName}. Keeping Server Name: {playerName}");
            
            // 从 NetworkManager 获取已分配的时间线
            int assignedTimeline = networkManager.GetPlayerTimeline(transportId);
            
            if (assignedTimeline >= 0)
            {
                timeline = assignedTimeline;
                Debug.Log($"[TimelinePlayer] Assigned to timeline {timeline}");
            }
        }
    }
    
    private void OnTimelineChanged(int oldTimeline, int newTimeline)
    {
        Debug.Log($"[TimelinePlayer] Timeline changed from {oldTimeline} to {newTimeline}");
        RefreshVisuals();

        SceneDirector.Instance?.TryLoadTimelineNow();
    }

    private void OnVisibilityChanged(bool oldValue, bool newValue)
    {
        UpdateVisibilityStatus();
    }
    
    // 核心逻辑：切换皮肤
    private void RefreshVisuals()
    {
        // 1. 先隐藏所有皮肤
        foreach (var skin in timelineSkins)
        {
            if(skin != null) skin.SetActive(false);
        }

        currentActiveRenderer = null;

        // 2. 激活对应时间线的皮肤
        if (timeline >= 0 && timeline < timelineSkins.Length)
        {
            GameObject targetSkin = timelineSkins[timeline];
            if (targetSkin != null)
            {
                targetSkin.SetActive(true);
                // 获取该皮肤上的渲染器（可能是 SpriteRenderer 或 MeshRenderer）
                currentActiveRenderer = targetSkin.GetComponent<Renderer>();
                
                // 如果是帧动画，Animator 通常也在这个物体上，自动就会开始运行
            }
        }

        // 3. 应用可见性（因为切换皮肤后，新皮肤默认可能是可见的，需要重新检查规则）
        UpdateVisibilityStatus();
    }

    // 核心逻辑：控制显隐
    private void UpdateVisibilityStatus()
    {
        if (currentActiveRenderer == null) return;

        // 规则：如果是本地玩家，始终可见；如果是其他玩家，根据 isVisible 决定
        bool shouldShow = isLocalPlayer || isVisible;
        
        currentActiveRenderer.enabled = shouldShow;
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
