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

    [Header("Warning UI")]
    public WarningPanelManager warningManager;

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
        Debug.Log("[StartMenuController] 收到 GameStartedEvent");
        HideRolePanelImmediate();

        // 显示加载进度条
        if (progressPanel != null)
        {
            progressPanel.SetActive(true);
            if (progressSlider != null) progressSlider.value = 0.5f;
            if (progressText != null) progressText.text = "加载中...";
        }

        // 确保 UI 在场景切换时保留（直到加载完成）
        DontDestroyOnLoad(transform.root.gameObject);
    }

    private void OnRoomProgress(RoomProgressEvent e)
    {
        if (progressPanel != null) progressPanel.SetActive(e.IsVisible);
        if (progressSlider != null) progressSlider.value = e.Progress;
        if (progressText != null) progressText.text = e.Message;
    }

    public void ShowWarning(string message)
    {
        if (warningManager != null)
        {
            warningManager.ShowWarning(message);
        }
        else
        {
            Debug.LogWarning($"[StartMenuController] Warning: {message} (Manager not assigned)");
        }
    }
}
