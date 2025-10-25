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
        string roomName = string.IsNullOrEmpty(roomNameInput.text) ? "Room-" + Random.Range(1000, 9999) : roomNameInput.text;
        nm.CreateRoom(roomName, (ok, msg) =>
        {
            Debug.Log(msg);
            if (ok) flow.OpenRolePanel();
        });
    }

    public void OnClickQuickJoin()
    {
        nm.JoinRoom((ok, msg) =>
        {
            Debug.Log(msg);
            if (ok) flow.OpenRolePanel();
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
        });
    }

    public void OnClickBack()
    {
        flow.OpenStart();
    }
}
