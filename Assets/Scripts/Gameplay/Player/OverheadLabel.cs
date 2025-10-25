using UnityEngine;
using Mirror;
using TMPro;

/// <summary>
/// 玩家头顶标签：显示玩家名称与角色，并始终朝向主摄像机。
/// </summary>
public class OverheadLabel : NetworkBehaviour
{
    [Header("引用组件")]
    [Tooltip("头顶锚点（骨骼或空物体）")]
    public Transform followHead;

    [Tooltip("World Space Canvas（用于承载文本显示）")]
    public Canvas worldCanvas;

    [Tooltip("用于显示内容的文本组件（Text 或 TMP_Text）")]
    public TMP_Text text;

    TimelinePlayer timelinePlayer;
    PlayerRole playerRole;

    void Awake()
    {
        timelinePlayer = GetComponent<TimelinePlayer>();
        playerRole = GetComponent<PlayerRole>();
    }

    void Start()
    {
        Refresh();
    }

    void LateUpdate()
    {
        if (followHead == null || worldCanvas == null) return;

        worldCanvas.transform.position = followHead.position;

        if (Camera.main != null)
        {
            worldCanvas.transform.LookAt(
                worldCanvas.transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }
    }

    /// <summary>
    /// 更新头顶文本内容（显示玩家名称与角色）。
    /// </summary>
    public void Refresh()
    {
        if (text == null) return;

        string nameStr = timelinePlayer != null ? timelinePlayer.playerName : "Player";
        string roleStr = playerRole != null ? playerRole.role.ToString() : "Role";
        text.text = $"{nameStr}\n[{roleStr}]";
    }
}
