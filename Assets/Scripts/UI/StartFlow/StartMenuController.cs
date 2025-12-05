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
    public GameObject progressPanel; // 进度条容器（可选）
    public Slider progressSlider;    // 进度条组件
    public TMP_Text progressText;    // 进度文本组件

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
        Debug.Log("StartMenuController received GameStartedEvent, closing role panel.");
        HideRolePanelImmediate();
        // 这里还可以触发其他游戏开始的逻辑，比如加载游戏场景
    }

    private void OnRoomProgress(RoomProgressEvent e)
    {
        if (progressPanel != null) progressPanel.SetActive(e.IsVisible);
        if (progressSlider != null) progressSlider.value = e.Progress;
        if (progressText != null) progressText.text = e.Message;
    }
}
