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

    [Header("角色归属显示文本")]
    // 这里把你刚才新建在图片下方的 NameText 拖进去
    public TMP_Text poetNameText;
    public TMP_Text artistNameText;
    public TMP_Text analystNameText;

    [Header("开始游戏")]
    public Button startButton;

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

        // 房主每0.5秒检查一次是否可以开始游戏
        if (NetworkServer.active)
        {
            InvokeRepeating(nameof(CheckStartButtonState), 0.5f, 0.5f);
        }
    }

    void OnDisable()
    {
        CancelInvoke(nameof(CheckStartButtonState));
    }

    void Update()
    {
        // 每一帧都根据网络数据刷新 UI 显示
        // 因为 SyncVar 的更新是异步的，所以在 Update 里轮询是最简单的同步 UI 方式
        RefreshRoleUI();
        TryBindLocal(); // 确保本地引用存在
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
        // 规则：如果这个位置已经被占了(Taken)，且不是我自己占的，那就不能点
        // 如果想要让玩家可以点击“抢”位置，可以去掉这个 interactable 的限制
        
        // 获取本地当前选中的角色
        int myCurrentSelection = localTimeline != null ? localTimeline.timeline : -1;

        poetButton.interactable = !ancientTaken || (myCurrentSelection == 0);
        artistButton.interactable = !modernTaken || (myCurrentSelection == 1);
        analystButton.interactable = !futureTaken || (myCurrentSelection == 2);
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

    void CheckStartButtonState()
    {
        if (startButton == null) return;
        
        // 只有房主能看到开始按钮
        if (!NetworkServer.active) 
        {
            startButton.gameObject.SetActive(false);
            return;
        }

        startButton.gameObject.SetActive(true);
        var nm = FindFirstObjectByType<EchoNetworkManager>();
        
        // 检查所有玩家是否都分配了有效的时间线 (timeline != -1)
        bool allReady = CheckAllPlayersHaveRoles();
        startButton.interactable = allReady;
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

        // 开始游戏逻辑
        if (flow != null) flow.HideRolePanelImmediate();
        EventBus.Publish(new GameStartedEvent());
        
        // 你可能需要在这里通知 EchoNetworkManager 锁定房间或切换场景
    }
}
