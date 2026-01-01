/* Gameplay/Player/PlayerRole.cs
 * 玩家角色管理组件
 * 处理角色选择、状态同步及服务器端冲突校验
 */

using UnityEngine;
using Mirror;

// 角色枚举
public enum RoleType { Ancient, Modern, Future }

/*
 * 玩家角色管理类：禁止重复选择同一角色（服务器校验）。
 * 客户端调用 ChooseRole -> 服务端 CmdChooseRole 校验 -> 成功则更新 SyncVar，失败则通过 TargetRpc 通知客户端。
 */
public class PlayerRole : NetworkBehaviour
{
    [Header("角色状态")]
    [Tooltip("当前玩家的角色（服务器同步）")]
    [SyncVar(hook = nameof(OnRoleChanged))]
    public RoleType role = RoleType.Ancient;

    [Tooltip("当前准备状态（服务器同步）")]
    [SyncVar]
    public bool isReady = false;

    [Tooltip("是否已选择并确认角色（服务器同步）")]
    [SyncVar]
    public bool isRoleSelected = false;

    /*
     * 客户端请求选择角色（发往服务器）。
     */
    public void ChooseRole(RoleType newRole)
    {
        if (!isLocalPlayer) return;
        CmdChooseRole(newRole);
    }

    /*
     * 客户端请求切换准备状态。
     * ready: 目标准备状态
     */
    public void SetReady(bool ready)
    {
        if (!isLocalPlayer) return;
        CmdSetReady(ready);
    }

    /*
     * 客户端请求切换角色选择确认状态。
     * selected: 目标选择状态
     */
    public void SetRoleSelected(bool selected)
    {
        if (!isLocalPlayer) return;
        CmdSetRoleSelected(selected);
    }

    /*
     * 服务端命令：尝试为该玩家设置角色。
     * 服务端在所有连接上检查是否已有玩家占用该角色，若占用则拒绝并通知请求者。
     */
    [Command]
    void CmdChooseRole(RoleType newRole)
    {
        // 在服务器端进行冲突检查：查看所有 PlayerRole 实例是否已有该角色
        foreach (var conn in NetworkServer.connections)
        {
            if (conn.Value == null) continue;
            var go = conn.Value.identity?.gameObject;
            if (go == null) continue;
            var pr = go.GetComponent<PlayerRole>();
            if (pr == null || pr == this) continue;
            if (pr.role == newRole)
            {
                // 该角色已被占用，通知请求者选择失败
                TargetChooseFailed(connectionToClient, newRole, "角色已被占用");
                return;
            }
        }

        // 没有占用，允许设置
        role = newRole;
    }

    [Command]
    void CmdSetReady(bool ready)
    {
        isReady = ready;
    }

    [Command]
    void CmdSetRoleSelected(bool selected)
    {
        isRoleSelected = selected;
    }

    /*
     * 目标RPC：单独通知请求客户端选择失败的原因。
     * target: 目标连接
     * attemptedRole: 尝试选择的角色
     * reason: 失败原因
     */
    [TargetRpc]
    void TargetChooseFailed(NetworkConnection target, RoleType attemptedRole, string reason)
    {
        // 客户端可以在这里弹出提示或更新 UI（此处简单打印日志）
        Debug.LogWarning($"ChooseRole failed for {attemptedRole}: {reason}");
        // 触发 UI 更新（例如 OverheadLabel）
        var label = GetComponent<OverheadLabel>();
        if (label != null)
        {
            // 如果 OverheadLabel 提供临时提示方法可以调用，否则刷新显示
            // label.ShowTempMessage(reason);
            label.Refresh();
        }
    }

    void OnRoleChanged(RoleType oldRole, RoleType newRole)
    {
        // 这里可以发事件或更新 UI
        var label = GetComponent<OverheadLabel>();
        if (label != null) label.Refresh();
    }
}
