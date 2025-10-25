/*
 * EchoNetworkManager.cs
 *
 * Mirror + Sync Relay 三人联机房间管理器。
 * 负责房间创建、加入、离开、玩家时间线分配、Relay回调注册与处理。
 * 通过 RelayTransportMirror 实现跨网络连接，支持 ParrelSync 多实例测试。
 *
 * 主要职责：
 * - 创建/加入/离开房间（Host/Client流程）
 * - 维护当前房间与玩家信息
 * - 注册并处理 Relay 回调（房间、玩家、主机迁移等）
 * - Mirror 生命周期钩子（OnStartServer/Client/Connect/Disconnect）
 * - 时间线分配与查询（3人合作场景）
 *
 * 使用说明：
 * - 挂载于场景唯一 NetworkManager 物体
 * - 配置 RelayTransportMirror 并粘贴 Room Profile UUID
 * - 设置 PlayerPrefab 后自动生成玩家对象
 * - 支持 ParrelSync 多实例本地测试
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Unity.Sync.Relay;
using Unity.Sync.Relay.Lobby;
using Unity.Sync.Relay.Model;
using Unity.Sync.Relay.Transport.Mirror;
using UnityEngine;

/*
 * 网络管理器，基于Mirror扩展的跨时空合作网络逻辑
 */
/// <summary>
/// Mirror 网络管理器，集成 Sync Relay 房间与玩家管理。
/// </summary>
public class EchoNetworkManager : Mirror.NetworkManager
{
    [Header("三人联机房间设置")]
    [SerializeField] private int maxPlayers = 3; // 房间最大玩家数
    [SerializeField] private string roomNamespace = "EchoOfTime"; // Relay房间命名空间

    [Header("玩家信息")]
    private string playerUuid; // 当前玩家唯一标识
    private string playerName; // 当前玩家显示名

    [Header("房间信息")]
    private RelayRoom currentRoom; // 当前房间信息（Relay同步）
    private Dictionary<uint, int> playerTimelineMap = new Dictionary<uint, int>(); // 玩家时间线分配表（TransportId -> Timeline）
    private readonly Dictionary<int, int> _timelineByConnectionId = new Dictionary<int, int>(); // 连接ID -> 时间线映射（用于断线重连恢复）

    private RelayTransportMirror relayTransport; // Relay传输组件（Mirror Transport）
    /// <summary>
    /// Unity生命周期：启动时初始化Relay、注册回调、设置玩家信息。
    /// </summary>
    public override void Start()
    {
        base.Start();
        NetworkEventRelay.Instance.RegisterMessageHandlers();
        InitializeRelayTransport();
        RegisterRelayCallbacks();
        SetupPlayerInfo();
    }
    
    /// <summary>
    /// 初始化 Relay Transport
    /// </summary>
    /// <summary>
    /// 获取并初始化 RelayTransportMirror。
    /// </summary>
    private void InitializeRelayTransport()
    {
        relayTransport = GetComponent<RelayTransportMirror>();
        if (relayTransport == null)
        {
            Debug.LogError("RelayTransportMirror component not found! Please add it to the NetworkManager.");
        }
    }
    
    /// <summary>
    /// 设置玩家信息
    /// </summary>
    /// <summary>
    /// 生成玩家唯一ID与显示名，并设置到Relay。
    /// </summary>
    private void SetupPlayerInfo()
    {
        playerUuid = Guid.NewGuid().ToString();
        playerName = "Player-" + playerUuid.Substring(0, 8);
        
        if (relayTransport != null)
        {
            relayTransport.SetPlayerData(playerUuid, playerName);
            Debug.Log($"Player Info Set - UUID: {playerUuid}, Name: {playerName}");
        }
    }
    
    /// <summary>
    /// 注册 Relay 回调函数
    /// </summary>
    /// <summary>
    /// 注册所有 Relay 回调（连接、玩家进出、房间信息、主机迁移等）。
    /// </summary>
    private void RegisterRelayCallbacks()
    {
        if (relayTransport == null)
        {
            Debug.LogError("RelayTransport is null - callbacks not registered");
            return;
        }
        
        RelayCallbacks callbacks = new RelayCallbacks();
        
    // 连接到Relay服务器的回调（房间信息同步，成功后 currentRoom 有效）
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
        
    // MasterClient迁移的回调（主机变更，通常用于断线重连场景）
        callbacks.RegisterMasterClientMigrate((newMasterClientID) =>
        {
            Debug.Log($"Master client migrated to: {newMasterClientID}");
        });
        
        relayTransport.SetCallbacks(callbacks);
    }
    
    /// <summary>
    /// 创建房间（作为 Host）
    /// </summary>
    /// <summary>
    /// 创建房间（Host流程，成功后自动 StartHost）。
    /// </summary>
    /// <param name="roomName">房间名</param>
    /// <param name="callback">结果回调</param>
    public void CreateRoom(string roomName, Action<bool, string> callback = null)
    {
        if (relayTransport == null)
        {
            callback?.Invoke(false, "RelayTransport not initialized");
            return;
        }
        
        StartCoroutine(CreateRoomCoroutine(roomName, callback));
    }
    
    /// <summary>
    /// 协程：异步创建房间并处理结果。
    /// </summary>
    private IEnumerator CreateRoomCoroutine(string roomName, Action<bool, string> callback)
    {
        Debug.Log($"Creating room: {roomName}");
        
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
                    relayTransport.SetRoomData(resp);
                    StartHost();
                    
                    callback?.Invoke(true, $"Room created: {roomName}");
                }
                else
                {
                    Debug.LogError($"Room status exception: {resp.Status}");
                    callback?.Invoke(false, $"Room status error: {resp.Status}");
                }
            }
            else
            {
                Debug.LogError($"Failed to create room - Code: {resp.Code}, Message: {resp.ErrorMessage}");
                callback?.Invoke(false, $"Failed to create room: {resp.ErrorMessage}");
            }
        });
    }
    
    /// <summary>
    /// 查询并加入房间（作为 Client）
    /// </summary>
    /// <summary>
    /// 查询并加入可用房间（Client流程，自动 StartClient）。
    /// </summary>
    /// <param name="callback">结果回调</param>
    public void JoinRoom(Action<bool, string> callback = null)
    {
        if (relayTransport == null)
        {
            callback?.Invoke(false, "RelayTransport not initialized");
            return;
        }
        
        StartCoroutine(JoinRoomCoroutine(callback));
    }
    
    /// <summary>
    /// 协程：异步查询房间列表并尝试加入第一个可用房间。
    /// </summary>
    private IEnumerator JoinRoomCoroutine(Action<bool, string> callback)
    {
        Debug.Log("Searching for available rooms...");
        
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
                    StartCoroutine(QueryAndJoinRoom(availableRoom.RoomUuid, callback));
                }
                else
                {
                    callback?.Invoke(false, "No available rooms found. Please create a room first.");
                }
            }
            else
            {
                Debug.LogError($"Failed to list rooms - Code: {resp.Code}");
                callback?.Invoke(false, $"Failed to list rooms: {resp.ErrorMessage}");
            }
        });
    }
    
    /// <summary>
    /// 协程：通过房间UUID查询并加入房间。
    /// </summary>
    private IEnumerator QueryAndJoinRoom(string roomUuid, Action<bool, string> callback)
    {
        Debug.Log($"Querying room: {roomUuid}");
        
        yield return LobbyService.AsyncQueryRoom(roomUuid, (resp) =>
        {
            if (resp.Code == (uint)RelayCode.OK)
            {
                Debug.Log($"Room queried successfully - Name: {resp.Name}, Players: {resp.PlayerCount}/{resp.MaxPlayers}");
                
                relayTransport.SetRoomData(resp);
                StartClient();
                
                callback?.Invoke(true, $"Joined room: {resp.Name}");
            }
            else
            {
                Debug.LogError($"Failed to query room - Code: {resp.Code}");
                callback?.Invoke(false, $"Failed to join room: {resp.ErrorMessage}");
            }
        });
    }
    
    /// <summary>
    /// 通过房间代码加入房间
    /// </summary>
    /// <summary>
    /// 通过房间码加入房间（适合 ParrelSync 多实例测试）。
    /// </summary>
    /// <param name="roomCode">房间码</param>
    /// <param name="callback">结果回调</param>
    public void JoinRoomByCode(string roomCode, Action<bool, string> callback = null)
    {
        StartCoroutine(JoinRoomByCodeCoroutine(roomCode, callback));
    }
    
    /// <summary>
    /// 协程：通过房间码异步查询并加入房间。
    /// </summary>
    private IEnumerator JoinRoomByCodeCoroutine(string roomCode, Action<bool, string> callback)
    {
        Debug.Log($"Joining room by code: {roomCode}");
        yield return QueryAndJoinRoomByCodeCoroutine(roomCode, callback);
    }

    /// <summary>
    /// 协程：轮询查询房间状态，直到房间就绪或超时。
    /// </summary>
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
                        StartClient();
                        callback?.Invoke(true, $"Joined room: {resp.Name}");
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
    
    /// <summary>
    /// 离开房间
    /// </summary>
    /// <summary>
    /// 离开房间（Host/Client/Server均可调用，清理状态）。
    /// </summary>
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
    
    /// <summary>
    /// 初始化跨时空合作房间（3玩家不同时间线）
    /// </summary>
    /// <summary>
    /// 初始化三人时间线合作房间（仅服务器调用）。
    /// </summary>
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
    /// 分配玩家到不同时间线
    /// </summary>
    /// <summary>
    /// 分配指定玩家到某个时间线（仅服务器调用）。
    /// </summary>
    /// <param name="transportId">玩家TransportId</param>
    /// <param name="timeline">时间线编号</param>
    public void AssignPlayerToTimeline(uint transportId, int timeline)
    {
        if (!NetworkServer.active) return;
        
        if (timeline < 0 || timeline >= maxPlayers)
        {
            Debug.LogError($"Invalid timeline: {timeline}. Must be between 0 and {maxPlayers - 1}");
            return;
        }
        
        if (playerTimelineMap.ContainsValue(timeline))
        {
            Debug.LogWarning($"Timeline {timeline} already occupied");
            return;
        }
        
        playerTimelineMap[transportId] = timeline;
        Debug.Log($"Assigned player {transportId} to timeline {timeline}");
    }
    
    /// <summary>
    /// 获取玩家的时间线
    /// </summary>
    /// <summary>
    /// 查询玩家所属时间线。
    /// </summary>
    /// <param name="transportId">玩家TransportId</param>
    /// <returns>时间线编号，未分配返回-1</returns>
    public int GetPlayerTimeline(uint transportId)
    {
        return playerTimelineMap.TryGetValue(transportId, out int timeline) ? timeline : -1;
    }
    
    /// <summary>
    /// 获取当前房间信息
    /// </summary>
    /// <summary>
    /// 获取当前房间信息（Relay同步）。
    /// </summary>
    public RelayRoom GetCurrentRoom()
    {
        return currentRoom;
    }
    
    /// <summary>
    /// 获取当前玩家信息
    /// </summary>
    /// <summary>
    /// 获取当前玩家信息（Relay同步）。
    /// </summary>
    public RelayPlayer GetCurrentPlayer()
    {
        return relayTransport?.GetCurrentPlayer();
    }

    /// <summary>
    /// 服务器记录连接ID与时间线的映射关系（用于断线重连恢复）。
    /// </summary>
    /// <param name="conn">网络连接</param>
    /// <param name="timeline">时间线编号</param>
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
    
    #endregion
    
    #region Relay 回调处理
    
    /// <summary>
    /// Relay连接成功回调（房间信息同步）。
    /// </summary>
    /// <param name="room">房间信息</param>
    private void OnRelayConnected(RelayRoom room)
    {
        Debug.Log($"Room Code: {room.RoomCode}");
        Debug.Log($"Players in room: {room.Players.Count}");
    }
    
    /// <summary>
    /// 玩家进入房间回调（自动分配时间线）。
    /// </summary>
    /// <param name="player">玩家信息</param>
    private void OnPlayerJoinedRoom(RelayPlayer player)
    {
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
    }
    
    /// <summary>
    /// 玩家离开房间回调（清理时间线分配）。
    /// </summary>
    /// <param name="player">玩家信息</param>
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
    
    /// <summary>
    /// 查找下一个可用时间线编号（0~maxPlayers-1）。
    /// </summary>
    /// <returns>可用时间线编号，无则-1</returns>
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
    
    /// <summary>
    /// Mirror服务器启动回调。
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Server started");
        InitializeCoopSession();
    }
    
    /// <summary>
    /// Mirror服务器停止回调。
    /// </summary>
    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("Server stopped");
    }
    
    /// <summary>
    /// Mirror客户端启动回调。
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("Client started");
    }
    
    /// <summary>
    /// Mirror客户端停止回调。
    /// </summary>
    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("Client stopped");
    }
    
    /// <summary>
    /// Mirror服务器收到客户端连接回调。
    /// </summary>
    /// <param name="conn">连接对象</param>
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"Client connected to server: {conn.connectionId}");
    }

    /// <summary>
    /// Mirror服务器为客户端添加玩家对象时调用（包括断线重连）。
    /// 如果该连接之前选择过时间线，会自动恢复。
    /// </summary>
    /// <param name="conn">连接对象</param>
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        if (conn.identity != null)
        {
            var tp = conn.identity.GetComponent<TimelinePlayer>();
            if (tp != null && _timelineByConnectionId.TryGetValue(conn.connectionId, out var timeline))
            {
                // 恢复之前选择的时间线（适用于断线重连场景）
                tp.ServerSetTimeline(timeline);
                Debug.Log($"Restored timeline {timeline} for reconnected player (Connection {conn.connectionId})");
            }
            else
            {
                Debug.Log($"New player added (Connection {conn.connectionId}), waiting for role selection...");
            }
        }
    }
    
    /// <summary>
    /// Mirror服务器收到客户端断开回调。
    /// </summary>
    /// <param name="conn">连接对象</param>
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
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
    
    /// <summary>
    /// Mirror客户端连接服务器回调。
    /// </summary>
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("Connected to server");
    }
    
    /// <summary>
    /// Mirror客户端断开服务器回调。
    /// </summary>
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("Disconnected from server");
    }
    
    #endregion
}