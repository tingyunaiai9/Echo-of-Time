using UnityEngine;
using Events;

public class StartMenuController : MonoBehaviour
{
    public GameObject startPanel;   // “开始游戏”面板
    public GameObject lobbyPanel;   // “创建/加入房间”面板
    public GameObject rolePanel;    // “选择角色”面板

    void OnEnable()
    {
        EventBus.Instance.Subscribe<GameStartedEvent>(OnGameStarted);
    }

    void OnDisable()
    {
        EventBus.Instance.Unsubscribe<GameStartedEvent>(OnGameStarted);
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
}
