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
        var code = roomCodeInput.text.Trim();
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
