using Mirror;
using UnityEngine;
using Events;
using TMPro;

public class RoleSelectPanel : MonoBehaviour
{
    public StartMenuController flow;
    public UnityEngine.UI.Button startButton;
    
    [Header("角色选择按钮")]
    public UnityEngine.UI.Button poetButton;
    public UnityEngine.UI.Button artistButton;
    public UnityEngine.UI.Button analystButton;
    
    [Header("准备系统")]
    public UnityEngine.UI.Button readyButton;
    public TMP_Text readyButtonText;

    private PlayerRole localRole;
    private TimelinePlayer localTimeline;
    private bool isRoleSelected = false;
    private bool hasSelectedRole = false; // 是否已选择过角色（不是"未选择"状态）

    void Awake() { TryBindLocal(); }
    
    void OnEnable() 
    { 
        TryBindLocal(); 
        StartCoroutine(WaitAndBind());
        UpdateReadyButton();
        UpdateRoleButtonsInteractable();
        UpdateStartButton();
        
        // 如果是房主，开始监听玩家准备状态
        if (NetworkServer.active)
        {
            InvokeRepeating(nameof(CheckAndUpdateStartButton), 0.5f, 0.5f);
        }
    }

    void OnDisable()
    {
        // 停止监听
        CancelInvoke(nameof(CheckAndUpdateStartButton));
    }

    void TryBindLocal()
    {
        if (NetworkClient.active && NetworkClient.localPlayer != null)
        {
            if (localRole == null)
                localRole = NetworkClient.localPlayer.GetComponent<PlayerRole>();
            if (localTimeline == null)
                localTimeline = NetworkClient.localPlayer.GetComponent<TimelinePlayer>();

            if (localRole != null || localTimeline != null)
                Debug.Log("[RoleSelect] 绑定本地玩家成功");
        }
    }

    System.Collections.IEnumerator WaitAndBind()
    {
        // 最多等 3 秒，每帧尝试一次
        float t = 0f;
        while (localRole == null && t < 3f)
        {
            TryBindLocal();
            t += Time.deltaTime;
            yield return null;
        }
        if (localRole == null) Debug.LogWarning("[RoleSelect] 本地玩家仍未就绪，稍后再点按钮会再次尝试绑定");
    }

    public void OnClickRole_Poet() => Choose(RoleType.Ancient);
    public void OnClickRole_Artist() => Choose(RoleType.Modern);
    public void OnClickRole_Analyst() => Choose(RoleType.Future);

    void Choose(RoleType r)
    {
        if (isRoleSelected)
        {
            Debug.LogWarning("已确认角色选择，无法切换角色。请先取消选择。");
            return;
        }

        if (localRole == null) { TryBindLocal(); }
        if (localRole == null) { Debug.LogWarning("本地玩家未就绪，稍等 NetworkClient 生成"); return; }

        if (localRole != null)
        {
            localRole.ChooseRole(r);
        }
        int tl = r == RoleType.Ancient ? 0 : r == RoleType.Modern ? 1 : 2;
        localTimeline.CmdChooseRole(tl);
        
        // 标记已选择过角色
        hasSelectedRole = true;
        UpdateReadyButton();
    }

    /// <summary>
    /// 确认/取消角色选择按钮点击事件
    /// </summary>
    public void OnClickReady()
    {
        if (localRole == null) 
        { 
            TryBindLocal();
            if (localRole == null)
            {
                Debug.LogWarning("本地玩家未就绪");
                return;
            }
        }

        // 如果还没选择角色，不允许确认
        if (!hasSelectedRole && !isRoleSelected)
        {
            Debug.LogWarning("请先选择一个角色");
            return;
        }

        // 切换选择确认状态
        isRoleSelected = !isRoleSelected;
        localRole.SetRoleSelected(isRoleSelected);

        // 更新UI
        UpdateReadyButton();
        UpdateRoleButtonsInteractable();
        
        // 如果是房主，立即检查开始按钮状态
        if (NetworkServer.active)
        {
            UpdateStartButton();
        }

        Debug.Log($"角色选择状态: {(isRoleSelected ? "已确认" : "已取消")}");
    }

    /// <summary>
    /// 更新准备按钮的文字和样式
    /// </summary>
    void UpdateReadyButton()
    {
        if (readyButton == null || readyButtonText == null) return;

        // 如果还没选择角色，按钮显示 "Select" 但禁用
        if (!hasSelectedRole)
        {
            readyButtonText.text = "Select";
            readyButton.interactable = false;
            return;
        }

        // 已选择角色，根据确认状态显示
        if (isRoleSelected)
        {
            readyButtonText.text = "Selected";
        }
        else
        {
            readyButtonText.text = "Select";
        }
        
        readyButton.interactable = true;
    }

    /// <summary>
    /// 更新角色选择按钮的可交互状态
    /// </summary>
    void UpdateRoleButtonsInteractable()
    {
        bool canChooseRole = !isRoleSelected;
        
        if (poetButton != null) poetButton.interactable = canChooseRole;
        if (artistButton != null) artistButton.interactable = canChooseRole;
        if (analystButton != null) analystButton.interactable = canChooseRole;
    }

    /// <summary>
    /// 更新开始游戏按钮的状态
    /// </summary>
    void UpdateStartButton()
    {
        if (startButton == null) return;

        // Client：始终禁用开始按钮
        if (!NetworkServer.active)
        {
            startButton.interactable = false;
            return;
        }

        // Host：根据所有玩家准备状态决定是否可点击
        var nm = FindFirstObjectByType<EchoNetworkManager>();
        bool allReady = nm != null && nm.CheckAllPlayersReady();
        startButton.interactable = allReady;
    }

    /// <summary>
    /// 定期检查并更新开始按钮状态（仅房主调用）
    /// </summary>
    void CheckAndUpdateStartButton()
    {
        if (!NetworkServer.active)
        {
            CancelInvoke(nameof(CheckAndUpdateStartButton));
            return;
        }

        UpdateStartButton();
    }

    /// <summary>
    /// 开始游戏按钮点击事件（仅房主可见/可用）
    /// </summary>
    public void OnClickStartGame()
    {
        // 检查是否是房主
        if (!NetworkServer.active)
        {
            Debug.LogWarning("只有房主可以开始游戏");
            return;
        }

        // 检查所有玩家是否准备就绪
        var nm = FindFirstObjectByType<EchoNetworkManager>();
        if (nm != null && !nm.CheckAllPlayersReady())
        {
            ShowNotReadyWarning();
            return;
        }

        Debug.Log("所有玩家已确认角色选择，游戏开始！");

        // 1) 先本地把面板关掉（保证 UI 一定关闭）
        if (flow != null) flow.HideRolePanelImmediate();

        // 2) 再广播事件（让其他系统/客户端有机会响应）
        EventBus.Instance.Publish(new GameStartedEvent());
    }

    /// <summary>
    /// 显示"有玩家未准备"的警告提示
    /// </summary>
    void ShowNotReadyWarning()
    {
        Debug.LogWarning("⚠️ 有玩家未确认角色选择！请等待所有玩家选择并确认角色。");
    }
}
