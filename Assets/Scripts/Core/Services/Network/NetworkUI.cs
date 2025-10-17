/* Core/Services/Network/NetworkUI.cs
 * 网络测试 UI，用于测试房间创建和加入功能
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

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
    
    void Update()
    {
        UpdateRoomInfo();
    }
    
    private void SetupUI()
    {
        if (roomNameInput != null)
        {
            roomNameInput.text = "EchoRoom-" + Random.Range(1000, 9999);
        }
        
        ShowMenuPanel();
    }
    
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
    
    private void OnLeaveRoomClicked()
    {
        networkManager.LeaveRoom();
        UpdateStatus("已离开房间", Color.white);
        ShowMenuPanel();
    }
    
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
    
    private void UpdateRoomInfo()
    {
        if (roomPanel != null && roomPanel.activeSelf)
        {
            var room = networkManager.GetCurrentRoom();
            var player = networkManager.GetCurrentPlayer();
            
            if (roomInfoText != null)
            {
                string role = NetworkServer.active ? (NetworkClient.isConnected ? "Host" : "Server") : "Client";
                string roomInfo = room != null ? $"房间: {room.Name}\n房间代码: {room.RoomCode}\n角色: {role}" : "加载中...";
                roomInfoText.text = roomInfo;
            }
            
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
    
    private void ShowMenuPanel()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (roomPanel != null) roomPanel.SetActive(false);
        EnableButtons();
    }
    
    private void ShowRoomPanel()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(true);
    }
    
    private void DisableButtons()
    {
        if (createRoomButton != null) createRoomButton.interactable = false;
        if (joinRoomButton != null) joinRoomButton.interactable = false;
        if (joinByCodeButton != null) joinByCodeButton.interactable = false;
    }
    
    private void EnableButtons()
    {
        if (createRoomButton != null) createRoomButton.interactable = true;
        if (joinRoomButton != null) joinRoomButton.interactable = true;
        if (joinByCodeButton != null) joinByCodeButton.interactable = true;
    }
    
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
