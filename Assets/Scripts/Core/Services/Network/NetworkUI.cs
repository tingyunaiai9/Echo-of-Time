/* Core/Services/Network/NetworkUI.cs
 * 网络测试 UI 管理器，提供房间创建、加入、离开等网络功能的可视化界面
 * 
 * 主要功能：
 * - 创建房间（Host 模式）
 * - 加入可用房间（Client 模式）
 * - 通过房间代码加入指定房间
 * - 显示房间信息和玩家列表
 * - 实时更新玩家时间线分配状态
 * 
 * UI 面板：
 * - Menu Panel：主菜单，包含创建/加入房间选项
 * - Room Panel：房间面板，显示房间信息和玩家列表
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

/*
 * 网络测试 UI 控制器，管理网络相关 UI 交互逻辑
 */
public class NetworkUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject roomPanel;
    
    [Header("Menu Panel")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button joinByCodeButton;
    
    [Header("Room Panel")]
    [SerializeField] private TextMeshProUGUI roomInfoText;
    [SerializeField] private TextMeshProUGUI playersListText;
    [SerializeField] private Button leaveRoomButton;
    
    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;
    
    private EchoNetworkManager networkManager;
    
    /*
     * Unity 生命周期：初始化时查找网络管理器并设置 UI
     */
    void Start()
    {
        networkManager = FindFirstObjectByType<EchoNetworkManager>();
        
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager not found in scene!");
            return;
        }
        
        SetupUI();
        RegisterButtonEvents();
        UpdateUI();
    }
    
    /*
     * Unity 生命周期：每帧更新房间信息显示
     */
    void Update()
    {
        UpdateRoomInfo();
    }
    
    /*
     * 初始化 UI 元素，生成随机房间名并显示主菜单面板
     */
    private void SetupUI()
    {
        if (roomNameInput != null)
        {
            roomNameInput.text = "EchoRoom-" + Random.Range(1000, 9999);
        }
        
        ShowMenuPanel();
    }
    
    /*
     * 注册所有按钮的点击事件
     */
    private void RegisterButtonEvents()
    {
        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        }
        
        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
        }
        
        if (joinByCodeButton != null)
        {
            joinByCodeButton.onClick.AddListener(OnJoinByCodeClicked);
        }
        
        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
        }
    }
    
    /*
     * 创建房间按钮点击事件
     * 验证房间名后调用网络管理器创建房间（Host 模式）
     */
    private void OnCreateRoomClicked()
    {
        string roomName = roomNameInput != null ? roomNameInput.text : "DefaultRoom";
        
        if (string.IsNullOrEmpty(roomName))
        {
            UpdateStatus("请输入房间名称！", Color.red);
            return;
        }
        
        UpdateStatus("正在创建房间...", Color.yellow);
        DisableButtons();
        
        networkManager.CreateRoom(roomName, (success, message) =>
        {
            if (success)
            {
                UpdateStatus($"成功: {message}", Color.green);
                ShowRoomPanel();
            }
            else
            {
                UpdateStatus($"失败: {message}", Color.red);
                EnableButtons();
            }
        });
    }
    
    /*
     * 加入房间按钮点击事件
     * 自动查找并加入第一个可用房间（Client 模式）
     */
    private void OnJoinRoomClicked()
    {
        UpdateStatus("正在查找房间...", Color.yellow);
        DisableButtons();
        
        networkManager.JoinRoom((success, message) =>
        {
            if (success)
            {
                UpdateStatus($"成功: {message}", Color.green);
                ShowRoomPanel();
            }
            else
            {
                UpdateStatus($"失败: {message}", Color.red);
                EnableButtons();
            }
        });
    }
    
    /*
     * 通过房间代码加入按钮点击事件
     * 验证房间代码后加入指定房间（Client 模式）
     */
    private void OnJoinByCodeClicked()
    {
        string roomCode = roomCodeInput != null ? roomCodeInput.text : "";
        
        if (string.IsNullOrEmpty(roomCode))
        {
            UpdateStatus("请输入房间代码！", Color.red);
            return;
        }
        
        UpdateStatus("正在加入房间...", Color.yellow);
        DisableButtons();
        
        networkManager.JoinRoomByCode(roomCode, (success, message) =>
        {
            if (success)
            {
                UpdateStatus($"成功: {message}", Color.green);
                ShowRoomPanel();
            }
            else
            {
                UpdateStatus($"失败: {message}", Color.red);
                EnableButtons();
            }
        });
    }
    
    /*
     * 离开房间按钮点击事件
     * 断开网络连接并返回主菜单
     */
    private void OnLeaveRoomClicked()
    {
        networkManager.LeaveRoom();
        UpdateStatus("已离开房间", Color.white);
        ShowMenuPanel();
    }
    
    /*
     * 更新 UI 显示状态，根据网络连接状态切换面板
     */
    private void UpdateUI()
    {
        bool isInRoom = NetworkClient.isConnected || NetworkServer.active;
        
        if (isInRoom)
        {
            ShowRoomPanel();
        }
        else
        {
            ShowMenuPanel();
        }
    }
    
    /*
     * 实时更新房间信息显示
     * 包括房间名、房间代码、角色（Host/Client）、玩家列表、时间线分配等
     */
    private void UpdateRoomInfo()
    {
        if (roomPanel != null && roomPanel.activeSelf)
        {
            var room = networkManager.GetCurrentRoom();
            var player = networkManager.GetCurrentPlayer();
            
            // 更新房间基本信息
            if (roomInfoText != null)
            {
                string role = NetworkServer.active ? (NetworkClient.isConnected ? "Host" : "Server") : "Client";
                string roomInfo = room != null ? $"房间: {room.Name}\n房间代码: {room.RoomCode}\n角色: {role}" : "加载中...";
                roomInfoText.text = roomInfo;
            }
            
            // 更新玩家列表和时间线分配
            if (playersListText != null && room != null)
            {
                string playersList = $"玩家列表 ({room.Players.Count}/3):\n";
                foreach (var kvp in room.Players)
                {
                    var p = kvp.Value;
                    int timeline = networkManager.GetPlayerTimeline(p.TransportId);
                    string timelineStr = timeline >= 0 ? $" [时间线 {timeline}]" : "";
                    playersList += $"• {p.Name}{timelineStr}\n";
                }
                playersListText.text = playersList;
            }
        }
    }
    
    /*
     * 显示主菜单面板，隐藏房间面板
     */
    private void ShowMenuPanel()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (roomPanel != null) roomPanel.SetActive(false);
        EnableButtons();
    }
    
    /*
     * 显示房间面板，隐藏主菜单面板
     */
    private void ShowRoomPanel()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(true);
    }
    
    /*
     * 禁用所有交互按钮（请求进行中时）
     */
    private void DisableButtons()
    {
        if (createRoomButton != null) createRoomButton.interactable = false;
        if (joinRoomButton != null) joinRoomButton.interactable = false;
        if (joinByCodeButton != null) joinByCodeButton.interactable = false;
    }
    
    /*
     * 启用所有交互按钮
     */
    private void EnableButtons()
    {
        if (createRoomButton != null) createRoomButton.interactable = true;
        if (joinRoomButton != null) joinRoomButton.interactable = true;
        if (joinByCodeButton != null) joinByCodeButton.interactable = true;
    }
    
    /*
     * 更新状态文本显示
     * @param message 状态消息内容
     * @param color 消息颜色（绿色=成功，红色=失败，黄色=进行中）
     */
    private void UpdateStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        Debug.Log($"[NetworkUI] {message}");
    }
}
