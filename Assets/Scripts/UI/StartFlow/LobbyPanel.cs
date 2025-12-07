using UnityEngine;
using TMPro;

public class LobbyPanel : MonoBehaviour
{
    public StartMenuController flow;
    public TMP_InputField roomNameInput;
    public TMP_InputField roomCodeInput;

    private EchoNetworkManager nm;

    void Awake()
    {
        nm = FindFirstObjectByType<EchoNetworkManager>();
    }

    public void OnClickCreate()
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

    public void OnClickJoinByCode()
    {
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

    public void OnClickBack()
    {
        flow.OpenStart();
    }
}
