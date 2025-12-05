using UnityEngine;
using Events;
using UnityEngine.UI;
using TMPro;

public class StartMenuController : MonoBehaviour
{
    public GameObject startPanel;   // “开始游戏”面板
    public GameObject lobbyPanel;   // “创建/加入房间”面板
    public GameObject rolePanel;    // “选择角色”面板

    [Header("Progress UI")]
    public GameObject progressPanel; // 进度条容器
    public Slider progressSlider;    // 进度条组件
    public TMP_Text progressText;    // 进度文本组件

    [Header("开场剧情")]
    [Tooltip("开场剧情数据（直接拖入 DialogueData）")]
    public DialogueData openingDialogue;

    [Tooltip("是否在游戏开始后自动播放开场剧情")]
    public bool playOpeningPlot = true;

    void OnEnable()
    {
        EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        EventBus.Subscribe<RoomProgressEvent>(OnRoomProgress);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        EventBus.Unsubscribe<RoomProgressEvent>(OnRoomProgress);
    }

    void Start()
    {
        OpenStart();
    }

    public void OpenStart()
    {
        startPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        rolePanel.SetActive(false);
        if (progressPanel != null) progressPanel.SetActive(false);
    }

    public void OnClickStartGame()
    {
        startPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    public void OpenRolePanel()
    {
        lobbyPanel.SetActive(false);
        rolePanel.SetActive(true);
        // 进入角色选择界面时，强制关闭进度条，防止遮挡
        if (progressPanel != null) progressPanel.SetActive(false);
    }

    public void HideRolePanelImmediate()
    {
        if (rolePanel != null) rolePanel.SetActive(false);
    }

    private void OnGameStarted(GameStartedEvent e)
    {
        Debug.Log("[StartMenuController] ✅ 收到 GameStartedEvent");
        
        HideRolePanelImmediate();

        // 详细检查每个条件
        Debug.Log($"[StartMenuController] playOpeningPlot = {playOpeningPlot}");
        Debug.Log($"[StartMenuController] openingDialogue 是否为空? {(openingDialogue == null ? "是(NULL)" : "否, 名称=" + openingDialogue.name)}");

        if (playOpeningPlot && openingDialogue != null)
        {
            Debug.Log("[StartMenuController] ✅ 条件满足，准备启动协程");
            StartCoroutine(PlayOpeningPlotDelayed());
        }
        else
        {
            Debug.LogWarning("[StartMenuController] ❌ 剧情未触发！原因:");
            if (!playOpeningPlot)
                Debug.LogWarning("  - playOpeningPlot 未勾选");
            if (openingDialogue == null)
                Debug.LogWarning("  - openingDialogue 未赋值（在 Inspector 中拖入 DialogueData）");
        }
    }

    private System.Collections.IEnumerator PlayOpeningPlotDelayed()
    {
        Debug.Log("[StartMenuController] 🔄 协程已启动，等待一帧...");
        
        yield return null;

        Debug.Log($"[StartMenuController] 🎬 开始播放开场剧情，剧情名称: {openingDialogue.name}");

        EventBus.Publish(new StartDialogueEvent(openingDialogue));
        
        Debug.Log("[StartMenuController] 📤 StartDialogueEvent 已发送");
    }

    private void OnRoomProgress(RoomProgressEvent e)
    {
        if (progressPanel != null) progressPanel.SetActive(e.IsVisible);
        if (progressSlider != null) progressSlider.value = e.Progress;
        if (progressText != null) progressText.text = e.Message;
    }
}
