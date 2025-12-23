/* Core/Services/Network/NetworkManager.cs
 * 
 * Mirror + Sync Relay 三人联机房间管理器
 * 负责房间创建、加入、离开、玩家时间线分配、Relay回调注册与处理
 * 通过 RelayTransportMirror 实现跨网络连接，支持 ParrelSync 多实例测试
 *
 * 主要职责：
 * - 创建/加入/离开房间（Host/Client 流程）
 * - 维护当前房间与玩家信息
 * - 注册并处理 Relay 回调（房间、玩家、主机迁移等）
 * - Mirror 生命周期钩子（OnStartServer/Client/Connect/Disconnect）
 * - 时间线分配与查询（3人合作场景）
 * - 支持本地测试模式和联网模式自动切换
 *
 * 使用说明：
 * - 挂载于场景唯一 NetworkManager 物体
 * - 配置 RelayTransportMirror 并粘贴 Room Profile UUID
 * - 设置 PlayerPrefab 后自动生成玩家对象
 * - 支持 ParrelSync 多实例本地测试
 * - 本地测试时设置 skipRelay = true
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Telepathy;
using Unity.Sync.Relay;
using Unity.Sync.Relay.Lobby;
using Unity.Sync.Relay.Model;
using Unity.Sync.Relay.Transport.Mirror;
using UnityEngine;
using Events;

/*
 * Echo 网络管理器，基于 Mirror 扩展的跨时空合作网络逻辑
 * 集成 Sync Relay 实现三人联机房间管理和时间线分配
 */
[DefaultExecutionOrder(-100)]
public class EchoNetworkManager : Mirror.NetworkManager
{
    [Header("开发测试选项")]
    public bool skipRelay = false; // 本地测试模式开关，true=跳过 Relay 初始化

    [Header("三人联机房间设置")]
    [SerializeField] private int maxPlayers = 3; // 房间最大玩家数
    [SerializeField] private string roomNamespace = "EchoOfTime"; // Relay 房间命名空间
    [SerializeField] private int relayHeartbeatTimeout = 60; // Relay 心跳超时时间（秒）

    [Header("玩家信息")]
    private string playerUuid; // 当前玩家唯一标识
    private string playerName; // 当前玩家显示名

    [Header("房间信息")]
    private RelayRoom currentRoom; // 当前房间信息（Relay 同步）
    private Dictionary<uint, int> playerTimelineMap = new Dictionary<uint, int>(); // 玩家时间线分配表（TransportId -> Timeline）
    private readonly Dictionary<int, int> _timelineByConnectionId = new Dictionary<int, int>(); // 连接ID -> 时间线映射（用于断线重连恢复）

    private RelayTransportMirror relayTransport; // Relay 传输组件（Mirror Transport）
    
    // 用于追踪当前层级答对的玩家与当前层数（服务器权威）
    private readonly HashSet<NetworkConnectionToClient> _answeredConnections = new HashSet<NetworkConnectionToClient>();
    private int _currentLevel = 1;
    private bool _playersRevealed = false; // 是否已揭示所有玩家（只执行一次）
    
    public override void Awake()
    {
        base.Awake();
        
        // 允许后台运行，防止切换窗口导致断线
        Application.runInBackground = true;

        // 设置 Relay 超时时间，防止长时间无操作断线
        var transport = GetComponent<RelayTransportMirror>();
        if (transport != null)
        {
            transport.HeartbeatTimeout = (uint)relayHeartbeatTimeout;
            Debug.Log($"[EchoNetworkManager] Set Relay HeartbeatTimeout to {relayHeartbeatTimeout}s");
        }
    }

    /*
     * Unity 生命周期：启动时初始化网络事件、Relay、注册回调、设置玩家信息
     * 支持本地测试模式和联网模式的自动切换
     */
    public override void Start()
    {
        base.Start();
        
        // 无论是否跳过 Relay，都需要注册网络事件处理器
        NetworkEventRelay.Instance.RegisterMessageHandlers();
        
        if (skipRelay)
        {
            Debug.Log("[EchoNetworkManager] 本地测试模式：跳过 Relay 初始化");
            // 本地测试模式下仍需要设置基本玩家信息（用于本地显示）
            playerUuid = Guid.NewGuid().ToString();
            playerName = "LocalPlayer-" + playerUuid.Substring(0, 8);
            Debug.Log($"[EchoNetworkManager] 本地测试玩家信息 - UUID: {playerUuid}, Name: {playerName}");
            return;
        }

        // 仅在联网模式下初始化 Relay 相关组件
        InitializeRelayTransport();
        RegisterRelayCallbacks();
        SetupPlayerInfo();
    }
    
    /*
     * 获取并初始化 RelayTransportMirror 组件
     * 联网模式必须组件，用于 Relay 通信
     */
    private void InitializeRelayTransport()
    {
        relayTransport = GetComponent<RelayTransportMirror>();
        if (relayTransport == null)
        {
            Debug.LogError("RelayTransportMirror component not found! Please add it to the NetworkManager.");
        }
    }
    
    /*
     * 生成玩家唯一 ID 与显示名，并设置到 Relay
     * UUID 用于 Relay 服务器识别，Name 用于显示
     */
    private void SetupPlayerInfo()
    {
        playerUuid = Guid.NewGuid().ToString();
        // 本地临时名字，进入游戏后会被 OnServerAddPlayer 覆盖为 Player 1/2/3
        playerName = "Guest-" + playerUuid.Substring(0, 4);
        
        if (relayTransport != null)
        {
            relayTransport.SetPlayerData(playerUuid, playerName);
            Debug.Log($"Player Info Set - UUID: {playerUuid}, Name: {playerName}");
        }
    }
    
    /*
     * 注册所有 Relay 回调（连接、玩家进出、房间信息、主机迁移等）
     * 通过 RelayCallbacks 监听 Relay 服务器事件
     */
    private void RegisterRelayCallbacks()
    {
        if (relayTransport == null)
        {
            Debug.LogError("RelayTransport is null - callbacks not registered");
            return;
        }
        
        RelayCallbacks callbacks = new RelayCallbacks();
        
        // 连接到 Relay 服务器的回调（房间信息同步，成功后 currentRoom 有效）
        callbacks.RegisterConnectToRelayServer((code, room) =>
        {
            if (code == (uint)RelayCode.OK)
            {
                currentRoom = room;
                Debug.Log($"Connected to Relay: room={room.Name}, code={room.RoomCode}, players={room.Players.Count}/3, status={room.Status}");
                OnRelayConnected(room);
            }
            else
            {
                Debug.LogError($"Failed to connect to Relay Server. Error Code: {code}");
            }
        });
        
        // 玩家进入房间的回调（用于分配时间线、更新玩家列表）
        callbacks.RegisterPlayerEnterRoom((player) =>
        {
            Debug.Log($"Player entered room - ID: {player.ID}, Name: {player.Name}, TransportId: {player.TransportId}");
            OnPlayerJoinedRoom(player);
        });
        
        // 玩家离开房间的回调（清理时间线分配）
        callbacks.RegisterPlayerLeaveRoom((player) =>
        {
            Debug.Log($"Player left room - ID: {player.ID}, Name: {player.Name}");
            OnPlayerLeftRoom(player);
        });
        
        // 房间信息更新的回调（玩家列表、状态变更等）
        callbacks.RegisterRoomInfoUpdate((room) =>
        {
            currentRoom = room;
            Debug.Log($"Room info updated - Players: {room.Players.Count}/{maxPlayers}");
        });
        
        // 玩家被踢出的回调（异常处理）
        callbacks.RegisterPlayerKicked((code, reason) =>
        {
            Debug.LogWarning($"Player kicked - Code: {code}, Reason: {reason}");
        });
        
        // MasterClient 迁移的回调（主机变更，通常用于断线重连场景）
        callbacks.RegisterMasterClientMigrate((newMasterClientID) =>
        {
            Debug.Log($"Master client migrated to: {newMasterClientID}");
        });
        
        relayTransport.SetCallbacks(callbacks);
    }
    
    /*
     * 创建房间（Host 流程，成功后自动 StartHost）
     * @param roomName 房间名称
     * @param callback 结果回调 (success, message)
     */
    public void CreateRoom(string roomName, Action<bool, string> callback = null)
    {
        if (skipRelay)
        {
            Debug.LogWarning("[EchoNetworkManager] 本地测试模式下不支持创建 Relay 房间，请使用 StartHost() 直接启动");
            callback?.Invoke(false, "本地测试模式不支持 Relay 房间");
            return;
        }
        
        if (relayTransport == null)
        {
            callback?.Invoke(false, "RelayTransport not initialized");
            return;
        }
        
        StartCoroutine(CreateRoomCoroutine(roomName, callback));
    }
    
    /*
     * 协程：异步创建房间并处理结果
     * 创建成功后自动启动 Host，房间状态必须为 ServerAllocated
     */
    private IEnumerator CreateRoomCoroutine(string roomName, Action<bool, string> callback)
    {
        Debug.Log($"Creating room: {roomName}");
        EventBus.Publish(new RoomProgressEvent { Progress = 0.1f, Message = "正在请求创建房间...", IsVisible = true });
        
        var request = new CreateRoomRequest()
        {
            Name = roomName,
            Namespace = roomNamespace,
            MaxPlayers = maxPlayers,
            OwnerId = playerUuid,
            Visibility = LobbyRoomVisibility.Public
        };
        
        yield return LobbyService.AsyncCreateRoom(request, (resp) =>
        {
            if (resp.Code == (uint)RelayCode.OK)
            {
                Debug.Log($"Room created successfully - RoomID: {resp.RoomUuid}, Status: {resp.Status}");
                
                if (resp.Status == LobbyRoomStatus.ServerAllocated)
                {
                    EventBus.Publish(new RoomProgressEvent { Progress = 0.8f, Message = "正在启动主机...", IsVisible = true });
                    relayTransport.SetRoomData(resp);
                    StartHost();
                    
                    callback?.Invoke(true, $"Room created: {roomName}");
                }
                else
                {
                    Debug.LogError($"Room status exception: {resp.Status}");
                    EventBus.Publish(new RoomProgressEvent { Progress = 0f, Message = "发生错误", IsVisible = false });
                    callback?.Invoke(false, $"Room status error: {resp.Status}");
                }
            }
            else
            {
                Debug.LogError($"Failed to create room - Code: {resp.Code}, Message: {resp.ErrorMessage}");
                EventBus.Publish(new RoomProgressEvent { Progress = 0f, Message = "发生错误", IsVisible = false });
                callback?.Invoke(false, $"Failed to create room: {resp.ErrorMessage}");
            }
        });
    }
    
    /*
     * 查询并加入可用房间（Client 流程，自动 StartClient）
     * @param callback 结果回调 (success, message)
     */
    public void JoinRoom(Action<bool, string> callback = null)
    {
        if (skipRelay)
        {
            Debug.LogWarning("[EchoNetworkManager] 本地测试模式下不支持加入 Relay 房间，请使用 StartClient() 或 StartHost()");
            callback?.Invoke(false, "本地测试模式不支持 Relay 房间");
            return;
        }
        
        if (relayTransport == null)
        {
            callback?.Invoke(false, "RelayTransport not initialized");
            return;
        }
        
        StartCoroutine(JoinRoomCoroutine(callback));
    }
    
    /*
     * 协程：异步查询房间列表并尝试加入第一个可用房间
     * 查找 ServerAllocated 和 Ready 状态的房间
     */
    private IEnumerator JoinRoomCoroutine(Action<bool, string> callback)
    {
        Debug.Log("Searching for available rooms...");
        EventBus.Publish(new RoomProgressEvent { Progress = 0.2f, Message = "正在搜索房间...", IsVisible = true });
        
        var request = new ListRoomRequest()
        {
            Start = 0,
            Count = 10,
            Namespace = roomNamespace,
            // 查找 ServerAllocated 和 Ready 状态的房间（Host 创建的房间会是 ServerAllocated）
            Statuses = new List<LobbyRoomStatus>() { LobbyRoomStatus.ServerAllocated, LobbyRoomStatus.Ready }
        };
        
        yield return LobbyService.AsyncListRoom(request, (resp) =>
        {
            if (resp.Code == (uint)RelayCode.OK)
            {
                Debug.Log($"Found {resp.Items.Count} available rooms");
                
                if (resp.Items.Count > 0)
                {
                    // 找到第一个可用的房间（Ready 状态）
                    var availableRoom = resp.Items[0];
                    Debug.Log($"Attempting to join room: {availableRoom.Name} (Status: {availableRoom.Status})");
                    EventBus.Publish(new RoomProgressEvent { Progress = 0.5f, Message = "找到房间，正在加入...", IsVisible = true });
                    StartCoroutine(QueryAndJoinRoom(availableRoom.RoomUuid, callback));
                }
                else
                {
                    EventBus.Publish(new RoomProgressEvent { Progress = 0f, Message = "未找到房间", IsVisible = false });
                    callback?.Invoke(false, "暂无可用房间");
                }
            }
            else
            {
                Debug.LogError($"Failed to list rooms - Code: {resp.Code}");
                EventBus.Publish(new RoomProgressEvent { Progress = 0f, Message = "获取房间列表失败", IsVisible = false });
                callback?.Invoke(false, $"Failed to list rooms: {resp.ErrorMessage}");
            }
        });
    }
    
    /*
     * 启动客户端并验证连接（等待 LocalPlayer 生成）
     * 防止加入僵尸房间导致卡死
     */
    private IEnumerator StartClientAndVerify(Action<bool, string> callback)
    {
        StartClient();
        
        float timeout = 5f;
        float timer = 0f;
        
        try
        {
            EventBus.Publish(new RoomProgressEvent { Progress = 0.9f, Message = "正在验证主机响应...", IsVisible = true });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NetworkManager] Failed to publish progress event: {e.Message}");
        }

        while (timer < timeout)
        {
            // 检查是否已连接且本地玩家对象已生成（说明主机活着并响应了）
            if (NetworkClient.isConnected && NetworkClient.localPlayer != null)
            {
                EventBus.Publish(new RoomProgressEvent { Progress = 1.0f, Message = "加入成功！", IsVisible = false });
                callback?.Invoke(true, "Joined room successfully");
                yield break;
            }
            
            // 检查是否连接断开
            if (!NetworkClient.active) 
            {
                 EventBus.Publish(new RoomProgressEvent { Progress = 0f, Message = "连接断开", IsVisible = false });
                 callback?.Invoke(false, "Connection failed");
                 yield break;
            }

            yield return null;
            timer += Time.deltaTime;
        }
        
        // 超时处理
        StopClient();
        try
        {
            EventBus.Publish(new RoomProgressEvent { Progress = 0f, Message = "连接超时", IsVisible = false });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NetworkManager] Failed to publish timeout event: {e.Message}");
        }
        callback?.Invoke(false, "Connection timed out. Host may be unresponsive.");
    }

    /*
     * 协程：通过房间 UUID 查询并加入房间
     * @param roomUuid 房间唯一标识符
     * @param callback 结果回调 (success, message)
     */
    private IEnumerator QueryAndJoinRoom(string roomUuid, Action<bool, string> callback)
    {
        Debug.Log($"Querying room: {roomUuid}");
        
        yield return LobbyService.AsyncQueryRoom(roomUuid, (resp) =>
        {
            if (resp.Code == (uint)RelayCode.OK)
            {
                Debug.Log($"Room queried successfully - Name: {resp.Name}, Players: {resp.PlayerCount}/{resp.MaxPlayers}");
                
                EventBus.Publish(new RoomProgressEvent { Progress = 0.8f, Message = "正在连接房间...", IsVisible = true });
                relayTransport.SetRoomData(resp);
                StartCoroutine(StartClientAndVerify(callback));
            }
            else
            {
                Debug.LogError($"Failed to query room - Code: {resp.Code}");
                EventBus.Publish(new RoomProgressEvent { Progress = 0f, Message = "查询房间失败", IsVisible = false });
                callback?.Invoke(false, $"Failed to join room: {resp.ErrorMessage}");
            }
        });
    }
    
    /*
     * 通过房间码加入房间（适合 ParrelSync 多实例测试）
     * @param roomCode 房间码
     * @param callback 结果回调 (success, message)
     */
    public void JoinRoomByCode(string roomCode, Action<bool, string> callback = null)
    {
        if (skipRelay)
        {
            Debug.LogWarning("[EchoNetworkManager] 本地测试模式下不支持通过房间码加入 Relay 房间");
            callback?.Invoke(false, "本地测试模式不支持 Relay 房间");
            return;
        }
        
        StartCoroutine(JoinRoomByCodeCoroutine(roomCode, callback));
    }
    
    /*
     * 协程：通过房间码异步查询并加入房间
     * @param roomCode 房间码
     * @param callback 结果回调 (success, message)
     */
    private IEnumerator JoinRoomByCodeCoroutine(string roomCode, Action<bool, string> callback)
    {
        Debug.Log($"Joining room by code: {roomCode}");
        yield return QueryAndJoinRoomByCodeCoroutine(roomCode, callback);
    }

    /*
     * 协程：轮询查询房间状态，直到房间就绪或超时
     * @param roomCode 房间码
     * @param callback 结果回调 (success, message)
     * @param timeoutSeconds 超时时间（秒），默认 10 秒
     */
    private IEnumerator QueryAndJoinRoomByCodeCoroutine(string roomCode, Action<bool, string> callback, int timeoutSeconds = 10)
    {
        float time = 0;
        bool joined = false;

        while (time < timeoutSeconds && !joined)
        {
            yield return LobbyService.AsyncQueryRoomByRoomCode(roomCode, (resp) =>
            {
                if (resp.Code == (uint)RelayCode.OK)
                {
                    // 检查房间状态是否已就绪
                    if (resp.Status == LobbyRoomStatus.ServerAllocated || resp.Status == LobbyRoomStatus.Ready)
                    {
                        Debug.Log($"Room '{resp.Name}' is ready. Joining...");
                        relayTransport.SetRoomData(resp);
                        StartCoroutine(StartClientAndVerify(callback));
                        joined = true;
                    }
                    else
                    {
                        Debug.Log($"Waiting for room to be ready... Current status: {resp.Status}");
                    }
                }
                else
                {
                    // 如果查询失败，则直接中止
                    callback?.Invoke(false, $"Failed to query room: {resp.ErrorMessage}");
                    joined = true; // 标记为true以跳出循环
                }
            });

            if (!joined)
            {
                yield return new WaitForSeconds(1);
                time += 1;
            }
        }

        if (!joined)
        {
            callback?.Invoke(false, "Join room timed out. The room was not ready in time.");
        }
    }
    
    /*
     * 离开房间（Host/Client/Server 均可调用，清理状态）
     * 根据当前角色停止相应的网络服务并清理数据
     */
    public void LeaveRoom()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            StopClient();
        }
        else if (NetworkServer.active)
        {
            StopServer();
        }
        
        playerTimelineMap.Clear();
        currentRoom = null;
        Debug.Log("Left room");
    }
    
    #region 时间线相关功能
    
    /*
     * 初始化三人时间线合作房间（仅服务器调用）
     * 清空时间线分配表，准备为新玩家分配时间线
     */
    public void InitializeCoopSession()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("Only server can initialize coop session");
            return;
        }
        
        Debug.Log("Initializing 3-player timeline coop session");
        playerTimelineMap.Clear();
    }
    
    /// <summary>
    /// 服务器端：记录一个玩家已正确回答当前层级的谜题。
    /// </summary>
    [Server]
    public void ServerPlayerAnsweredCorrectly(TimelinePlayer player)
    {
        if (player == null || player.connectionToClient == null)
        {
            Debug.LogWarning("[NetworkManager] 收到无效的玩家或连接，忽略");
            return;
        }

        if (player.currentLevel != _currentLevel)
        {
            Debug.LogWarning($"[NetworkManager] 玩家 {player.playerName} (层数: {player.currentLevel}) 尝试回答第 {_currentLevel} 层的题目。忽略。");
            return;
        }

        if (_answeredConnections.Add(player.connectionToClient))
        {
            Debug.Log($"[NetworkManager] 玩家 {player.playerName} 已正确回答第 {_currentLevel} 层的题目。当前进度: {_answeredConnections.Count}/{numPlayers}");

            // 检查是否所有玩家都已答对
            if (_answeredConnections.Count >= numPlayers)
            {
                ServerAdvanceToNextLevel();
            }
        }
    }

    /// <summary>
    /// 服务器端：提升所有玩家到下一层。
    /// </summary>
    [Server]
    private void ServerAdvanceToNextLevel()
    {
        _currentLevel++;
        Debug.Log($"[NetworkManager] 所有玩家均已答对！正在进入第 {_currentLevel} 层...");

        // 如果进入第3层，揭示所有玩家（去除遮罩/不可见状态）
        if (_currentLevel == 3)
        {
            ServerRevealAllPlayers();
        }

        // 清空答对列表，为下一层做准备
        _answeredConnections.Clear();

        // 提升所有在线玩家的层数
        foreach (var player in NetworkServer.spawned.Values.Select(identity => identity.GetComponent<TimelinePlayer>()))
        {
            if (player != null)
            {
                player.currentLevel = _currentLevel;
            }
        }
        
        // 不再切换到 Plot 场景，而是直接在当前场景中通过 TimelinePlayer 的 OnLevelChanged 回调加载新关卡
        Debug.Log($"[NetworkManager] Level advanced to {_currentLevel}. Clients should load new timeline scenes.");
    }

    [Server]
    public void ServerFinishPlot()
    {
        Debug.Log("[NetworkManager] Plot finished.");
        // Plot 现在是 Panel，不需要切换场景
    }

    [Server]
    private void ServerRevealAllPlayers()
    {
        Debug.Log("[NetworkManager] 揭示所有玩家，可见性设为 true");
        foreach (var tp in NetworkServer.spawned.Values.Select(i => i.GetComponent<TimelinePlayer>()))
        {
            if (tp != null)
            {
                tp.ServerSetVisibility(true);
            }
        }
        _playersRevealed = true;
    }
    
    /*
     * 分配指定玩家到某个时间线（仅服务器调用）
     * @param transportId 玩家 TransportId
     * @param timeline 时间线编号（0=Ancient, 1=Modern, 2=Future）
     */
    public void AssignPlayerToTimeline(uint transportId, int timeline)
    {
        if (!NetworkServer.active) return;
        
        if (timeline < 0 || timeline >= maxPlayers)
        {
            Debug.LogError($"Invalid timeline: {timeline}. Must be between 0 and {maxPlayers - 1}");
            return;
        }
        
        // 允许覆盖自己的选择，但检查是否被他人占用
        foreach (var kvp in playerTimelineMap)
        {
            if (kvp.Value == timeline && kvp.Key != transportId)
            {
                Debug.LogWarning($"Timeline {timeline} already occupied by {kvp.Key}");
                return;
            }
        }
        
        playerTimelineMap[transportId] = timeline;
        Debug.Log($"Assigned player {transportId} to timeline {timeline}");
    }

    /*
     * 注册玩家的时间线选择（更新映射表），供 TimelinePlayer 调用
     * @param transportId 玩家 TransportId
     * @param timeline 时间线编号
     */
    public void ServerRegisterTimelineSelection(uint transportId, int timeline)
    {
        AssignPlayerToTimeline(transportId, timeline);
    }
    
    /*
     * 获取玩家所属时间线
     * @param transportId 玩家 TransportId
     * @returns 时间线编号（0=Ancient, 1=Modern, 2=Future），未分配返回 -1
     */
    public int GetPlayerTimeline(uint transportId)
    {
        return playerTimelineMap.TryGetValue(transportId, out int timeline) ? timeline : -1;
    }
    
    /*
     * 获取当前房间信息（Relay 同步）
     * @returns 当前房间对象，未加入返回 null
     */
    public RelayRoom GetCurrentRoom()
    {
        return currentRoom;
    }
    
    /*
     * 获取当前玩家信息（Relay 同步）
     * @returns 当前玩家对象，未连接返回 null
     */
    public RelayPlayer GetCurrentPlayer()
    {
        return relayTransport?.GetCurrentPlayer();
    }

    /*
     * 服务器记录连接 ID 与时间线的映射关系（用于断线重连恢复）
     * @param conn 网络连接
     * @param timeline 时间线编号（0=Ancient, 1=Modern, 2=Future）
     */
    [Server]
    public void ServerRememberTimeline(NetworkConnectionToClient conn, int timeline)
    {
        if (conn == null)
        {
            Debug.LogWarning("Cannot remember timeline: connection is null");
            return;
        }

        if (timeline < 0 || timeline >= maxPlayers)
        {
            Debug.LogError($"Invalid timeline {timeline} for connection {conn.connectionId}");
            return;
        }

        _timelineByConnectionId[conn.connectionId] = timeline;
        Debug.Log($"Server remembered: Connection {conn.connectionId} -> Timeline {timeline}");
    }

    /*
     * 检查所有玩家是否准备就绪（仅服务器调用）
     * @returns 如果所有玩家都已准备返回 true，否则返回 false
     */
    [Server]
    public bool CheckAllPlayersReady()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("CheckAllPlayersReady can only be called on server");
            return false;
        }

        int totalPlayers = 0;
        int selectedPlayers = 0;

        // 遍历所有连接的玩家
        foreach (var conn in NetworkServer.connections)
        {
            if (conn.Value == null) continue;
            var identity = conn.Value.identity;
            if (identity == null) continue;

            var playerRole = identity.GetComponent<PlayerRole>();
            if (playerRole != null)
            {
                totalPlayers++;
                if (playerRole.isRoleSelected)
                {
                    selectedPlayers++;
                }
            }
        }

        // 至少需要1个玩家，且所有玩家都已选择角色
        return totalPlayers > 0 && selectedPlayers == totalPlayers;
    }
    
    #endregion
    
    #region Relay 回调处理
    
    /*
     * Relay 连接成功回调（房间信息同步）
     * @param room 房间信息对象
     */
    private void OnRelayConnected(RelayRoom room)
    {
        Debug.Log($"Room Code: {room.RoomCode}");
        Debug.Log($"Players in room: {room.Players.Count}");
        EventBus.Publish(new RoomProgressEvent { Progress = 1.0f, Message = "已连接！", IsVisible = false });
    }
    
    /*
     * 玩家进入房间回调（自动分配时间线）
     * @param player 玩家信息对象
     */
    private void OnPlayerJoinedRoom(RelayPlayer player)
    {
        // 移除自动分配逻辑，等待玩家手动选择角色
        /*
        // 当新玩家加入时，如果是服务器，自动分配时间线
        if (NetworkServer.active && playerTimelineMap.Count < maxPlayers)
        {
            int nextTimeline = FindNextAvailableTimeline();
            if (nextTimeline >= 0)
            {
                playerTimelineMap[player.TransportId] = nextTimeline;
                Debug.Log($"Auto-assigned player {player.TransportId} to timeline {nextTimeline}");
            }
        }
        */
        Debug.Log($"Player joined room: {player.TransportId}. Waiting for role selection.");
    }
    
    /*
     * 玩家离开房间回调（清理时间线分配）
     * @param player 玩家信息对象
     */
    private void OnPlayerLeftRoom(RelayPlayer player)
    {
        // 玩家离开时清理时间线分配
        if (playerTimelineMap.ContainsKey(player.TransportId))
        {
            int timeline = playerTimelineMap[player.TransportId];
            playerTimelineMap.Remove(player.TransportId);
            Debug.Log($"Player {player.TransportId} left timeline {timeline}");
        }
    }
    
    /*
     * 查找下一个可用时间线编号（0~maxPlayers-1）
     * @returns 可用时间线编号，无可用返回 -1
     */
    private int FindNextAvailableTimeline()
    {
        for (int i = 0; i < maxPlayers; i++)
        {
            if (!playerTimelineMap.ContainsValue(i))
            {
                return i;
            }
        }
        return -1;
    }
    
    #endregion
    
    #region Mirror 重写方法
    
    /*
     * Mirror 服务器启动回调
     */
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Server started");
        InitializeCoopSession();
    }
    
    /*
     * Mirror 服务器停止回调
     */
    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("Server stopped");
    }
    
    /*
     * Mirror 客户端启动回调
     */
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("Client started");
    }
    
    /*
     * Mirror 客户端停止回调
     */
    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("Client stopped");
    }
    
    /*
     * Mirror 服务器收到客户端连接回调
     * @param conn 客户端连接对象
     */
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"Client connected to server: {conn.connectionId}");
    }

    /*
     * Mirror 服务器为客户端添加玩家对象时调用（包括断线重连）
     * 如果该连接之前选择过时间线，会自动恢复
     * @param conn 客户端连接对象
     */
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        if (conn.identity != null)
        {
            var tp = conn.identity.GetComponent<TimelinePlayer>();
            if (tp != null)
            {
                // --- 新增命名逻辑 ---
                // numPlayers 是 Mirror 内置属性，表示当前服务器上的玩家总数
                // 第一个进来的就是 Player 1，第二个是 Player 2...
                string sequentialName = $"Player {numPlayers}"; 
                
                // 直接修改 TimelinePlayer 的 SyncVar，这会自动同步给所有客户端
                tp.playerName = sequentialName;
                // ------------------

                // 同步当前层数
                tp.currentLevel = _currentLevel;

                if (_timelineByConnectionId.TryGetValue(conn.connectionId, out var timeline))
                {
                    // 恢复之前选择的时间线（适用于断线重连场景）
                    tp.ServerSetTimeline(timeline);
                    Debug.Log($"Restored timeline {timeline} for {sequentialName} (Connection {conn.connectionId})");
                }
                else
                {
                    Debug.Log($"New player added: {sequentialName} (Connection {conn.connectionId}), waiting for role selection...");
                }
            }
        }
    }
    
    /*
     * Mirror 服务器收到客户端断开回调
     * @param conn 客户端连接对象
     */
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // 当玩家断开连接时，也从答题列表中移除
        _answeredConnections.Remove(conn);
        
        base.OnServerDisconnect(conn);
        
        // 清理玩家的时间线分配
        uint connId = (uint)conn.connectionId;
        if (playerTimelineMap.ContainsKey(connId))
        {
            playerTimelineMap.Remove(connId);
            Debug.Log($"Client disconnected from server: {connId}");
        }

        // 注意：我们保留 _timelineByConnectionId 中的记录，以便断线重连时恢复
        // 如果需要彻底清理，可以在这里添加：
        // _timelineByConnectionId.Remove(conn.connectionId);
    }
    
    /*
     * Mirror 客户端连接服务器回调
     */
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("Connected to server");
    }
    
    /*
     * Mirror 客户端断开服务器回调
     */
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("Disconnected from server");
    }
    
    #endregion
}