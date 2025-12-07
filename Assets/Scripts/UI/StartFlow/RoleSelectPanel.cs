using Mirror;
using UnityEngine;
using Events;
using TMPro;
using UnityEngine.UI; // 引入 UI 命名空间

public class RoleSelectPanel : MonoBehaviour
{
    public StartMenuController flow;
    
    [Header("角色图片按钮")]
    // 这里把你的图片按钮拖进去
    public Button poetButton;   // Ancient / Past
    public Button artistButton; // Modern / Present
    public Button analystButton; // Future

    [Header("角色立绘组件")]
    public Image poetCharacterImage;
    public Image artistCharacterImage;
    public Image analystCharacterImage;

    [Header("角色归属显示文本")]
    // 这里把你刚才新建在图片下方的 NameText 拖进去
    public TMP_Text poetNameText;
    public TMP_Text artistNameText;
    public TMP_Text analystNameText;

    [Header("开始游戏")]
    public Button startButton;
    public TMP_Text startButtonText;

    // 本地缓存
    private PlayerRole localRole;
    private TimelinePlayer localTimeline;

    void OnEnable()
    {
        // 绑定按钮点击事件
        poetButton.onClick.RemoveAllListeners();
        poetButton.onClick.AddListener(() => OnClickRole(0)); // 0 = Ancient

        artistButton.onClick.RemoveAllListeners();
        artistButton.onClick.AddListener(() => OnClickRole(1)); // 1 = Modern

        analystButton.onClick.RemoveAllListeners();
        analystButton.onClick.AddListener(() => OnClickRole(2)); // 2 = Future
    }

    void OnDisable()
    {
    }

    void Update()
    {
        // 每一帧都根据网络数据刷新 UI 显示
        // 因为 SyncVar 的更新是异步的，所以在 Update 里轮询是最简单的同步 UI 方式
        RefreshRoleUI();
        TryBindLocal(); // 确保本地引用存在
        UpdateStartButton();
    }

    /// <summary>
    /// 核心逻辑：遍历所有玩家，更新 UI 上的名字
    /// </summary>
    void RefreshRoleUI()
    {
        // 1. 先重置所有文本为 Unselected
        poetNameText.text = "Unselected";
        artistNameText.text = "Unselected";
        analystNameText.text = "Unselected";

        // 重置按钮交互性（假设没人选）
        bool ancientTaken = false;
        bool modernTaken = false;
        bool futureTaken = false;

        // 2. 查找场景里所有的 TimelinePlayer (包含自己和其他客户端的镜像)
        // 注意：TimelinePlayer 必须挂载在 Player Prefab 上
        TimelinePlayer[] allPlayers = FindObjectsByType<TimelinePlayer>(FindObjectsSortMode.None);

        foreach (var player in allPlayers)
        {
            // 根据玩家存储的 timeline 变量决定显示名字的位置
            // 0=Ancient, 1=Modern, 2=Future (对应 TimelinePlayer.cs 的逻辑)
            switch (player.timeline)
            {
                case 0:
                    poetNameText.text = player.playerName;
                    ancientTaken = true;
                    break;
                case 1:
                    artistNameText.text = player.playerName;
                    modernTaken = true;
                    break;
                case 2:
                    analystNameText.text = player.playerName;
                    futureTaken = true;
                    break;
            }
        }

        // 3. 更新按钮的可交互性
        // 规则：如果这个位置已经被占了(Taken)，无论是不是我，都变灰（不可点击）
        // 这样用户点击后会有“已选择”的反馈（变灰），想换角色点击其他亮着的按钮即可
        poetButton.interactable = !ancientTaken;
        artistButton.interactable = !modernTaken;
        analystButton.interactable = !futureTaken;

        // 4. 更新立绘颜色
        UpdateCharacterColor(poetCharacterImage, ancientTaken);
        UpdateCharacterColor(artistCharacterImage, modernTaken);
        UpdateCharacterColor(analystCharacterImage, futureTaken);
    }

    private void UpdateCharacterColor(Image img, bool isTaken)
    {
        if (img == null) return;
        // 如果被占用，显示白色（原色）；否则显示灰色（待选）
        img.color = isTaken ? Color.white : Color.gray;
    }

    /// <summary>
    /// 点击角色图片时调用
    /// </summary>
    /// <param name="timelineIndex">0, 1, 2</param>
    void OnClickRole(int timelineIndex)
    {
        if (localTimeline == null) return;

        // 发送命令给服务器：我要选这个角色
        // TimelinePlayer.cs 里的 CmdChooseRole 会更新 SyncVar，随后同步给所有人
        localTimeline.CmdChooseRole(timelineIndex);
    }

    void TryBindLocal()
    {
        if (localTimeline == null && NetworkClient.localPlayer != null)
        {
            localTimeline = NetworkClient.localPlayer.GetComponent<TimelinePlayer>();
        }
    }
    
    // --- 以下是房主开始游戏的逻辑 ---

    void UpdateStartButton()
    {
        if (startButton == null) return;
        
        // 确保按钮对所有人都可见
        startButton.gameObject.SetActive(true);

        if (NetworkServer.active)
        {
            // 房主逻辑
            bool allReady = CheckAllPlayersHaveRoles();
            startButton.interactable = allReady;
            if (startButtonText != null) startButtonText.text = "确认";
        }
        else
        {
            // 客户端逻辑
            startButton.interactable = false;
            if (startButtonText != null) startButtonText.text = "等待房主";
        }
    }

    private bool CheckAllPlayersHaveRoles()
    {
        TimelinePlayer[] allPlayers = FindObjectsByType<TimelinePlayer>(FindObjectsSortMode.None);
        if (allPlayers.Length == 0) return false;

        foreach (var p in allPlayers)
        {
            if (p.timeline == -1) return false; // 有人还没选
        }
        return true;
    }

    public void OnClickStartGame()
    {
        if (!NetworkServer.active) return;

        Debug.Log("[RoleSelectPanel] 房主点击了开始游戏按钮");

        // 开始游戏逻辑
        if (flow != null) flow.HideRolePanelImmediate();
        EventBus.LocalPublish(new GameStartedEvent());
        EventBus.Publish(new GameStartedEvent());
        
        // 你可能需要在这里通知 EchoNetworkManager 锁定房间或切换场景
    }
}
