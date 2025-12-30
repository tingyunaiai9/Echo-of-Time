using UnityEngine;
using TMPro;

public class LobbyPanel : MonoBehaviour
{
    public StartMenuController flow;
    public TMP_InputField roomNameInput;
    public TMP_InputField roomCodeInput;

    [Header("UI Panels")]
    public GameObject roomNamePanel; // 包含 RoomNameInput 和 确认按钮
    public GameObject roomCodePanel; // 包含 RoomCodeInput 和 确认按钮

    private EchoNetworkManager nm;

    void Awake()
    {
        nm = FindFirstObjectByType<EchoNetworkManager>();
    }

    void Start()
    {
        // 初始隐藏
        if (roomNamePanel != null) roomNamePanel.SetActive(false);
        if (roomCodePanel != null) roomCodePanel.SetActive(false);

        // 绑定回车确认
        if (roomNameInput != null) roomNameInput.onSubmit.AddListener((str) => OnConfirmCreate());
        if (roomCodeInput != null) roomCodeInput.onSubmit.AddListener((str) => OnConfirmJoin());
    }

    // 点击创建房间按钮（呼出输入框）
    public void OnClickCreate()
    {
        if (roomNamePanel != null)
        {
            roomNamePanel.SetActive(true);
            if (roomCodePanel != null) roomCodePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[LobbyPanel] roomNamePanel 未赋值，无法显示输入框");
        }
    }

    // 点击确认创建
    public void OnConfirmCreate()
    {
        Debug.Log($"[LobbyPanel] flow={flow}, nm={nm}, roomNameInput={(roomNameInput ? roomNameInput.name : "null")}");

        if (flow == null) { Debug.LogError("[LobbyPanel] StartMenuController (flow) 未赋值"); return; }
        if (nm == null) { Debug.LogError("[LobbyPanel] EchoNetworkManager 未找到（Boot里是否挂了 EchoNetworkManager 组件？）"); return; }
        if (roomNameInput == null) { Debug.LogError("[LobbyPanel] roomNameInput 未赋值"); return; }

        var roomName = string.IsNullOrWhiteSpace(roomNameInput.text) ? "Room" : roomNameInput.text.Trim();
        nm.CreateRoom(roomName, (ok, msg) =>
        {
            Debug.Log(msg);
            if (ok) flow.OpenRolePanel();
            else flow.ShowWarning(msg);
        });
    }

    public void OnClickQuickJoin()
    {
        nm.JoinRoom((ok, msg) =>
        {
            Debug.Log(msg);
            if (ok) flow.OpenRolePanel();
            else flow.ShowWarning(msg);
        });
    }

    // 点击加入房间按钮（呼出输入框）
    public void OnClickJoinByCode()
    {
        if (roomCodePanel != null)
        {
            roomCodePanel.SetActive(true);
            if (roomNamePanel != null) roomNamePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[LobbyPanel] roomCodePanel 未赋值，无法显示输入框");
        }
    }

    // 点击确认加入
    public void OnConfirmJoin()
    {
        if (roomCodeInput == null) { Debug.LogError("[LobbyPanel] roomCodeInput 未赋值"); return; }

        string originalInput = roomCodeInput.text;
        Debug.Log($"[Debug] Original input from roomCodeInput: '{originalInput}' (Length: {originalInput.Length})");

        var code = originalInput.Trim();
        Debug.Log($"[Debug] Input after Trim(): '{code}' (Length: {code.Length})");
        
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("房间码为空");
            return;
        }
        nm.JoinRoomByCode(code, (ok, msg) =>
        {
            Debug.Log(msg);
            if (ok) flow.OpenRolePanel();
            else flow.ShowWarning(msg);
        });
    }

    // 点击取消/关闭输入面板
    public void OnCancelInput()
    {
        if (roomNamePanel != null) roomNamePanel.SetActive(false);
        if (roomCodePanel != null) roomCodePanel.SetActive(false);
    }

    public void OnClickBack()
    {
        flow.OpenStart();
    }
}
