using Mirror;
using UnityEngine;
using Events;

public class RoleSelectPanel : MonoBehaviour
{
    public StartMenuController flow;
    public UnityEngine.UI.Button startButton;

    private PlayerRole localRole;

    void Awake() { TryBindLocal(); }
    void OnEnable() { TryBindLocal(); StartCoroutine(WaitAndBind()); }

    void TryBindLocal()
    {
        if (localRole != null) return;
        if (NetworkClient.active && NetworkClient.localPlayer != null)
        {
            localRole = NetworkClient.localPlayer.GetComponent<PlayerRole>();
            if (localRole != null) Debug.Log("[RoleSelect] 绑定本地玩家成功");
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
        if (localRole == null) { TryBindLocal(); }
        if (localRole == null) { Debug.LogWarning("本地玩家未就绪，稍等 NetworkClient 生成"); return; }

        localRole.ChooseRole(r);
        localRole.SetReady(true);
        Debug.Log("已选择角色：" + r);
    }

    public void OnClickStartGame()
    {
        // 1) 先本地把面板关掉（保证 UI 一定关闭）
        if (flow != null) flow.HideRolePanelImmediate();

        // 2) 再广播事件（让其他系统/客户端有机会响应）
        EventBus.Instance.Publish(new GameStartedEvent());
        Debug.Log("GameStartedEvent published");
    }
}
