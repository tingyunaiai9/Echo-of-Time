/* Gameplay/Player/OverheadLabel.cs
 * 玩家头顶标签控制器
 * 负责显示玩家名称与角色信息，并处理UI跟随与朝向逻辑
 */

using UnityEngine;
using Mirror;
using TMPro;

/*
 * 玩家头顶标签类：显示玩家名称与角色，并始终朝向主摄像机。
 */
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

    /*
     * 初始化，获取相关组件引用
     */
    void Awake()
    {
        timelinePlayer = GetComponent<TimelinePlayer>();
        playerRole = GetComponent<PlayerRole>();
    }

    /*
     * 启动时刷新显示内容
     */
    void Start()
    {
        Refresh();
    }

    /*
     * 每帧更新UI位置和朝向
     * 确保UI始终位于头顶并面向摄像机
     */
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

    /*
     * 更新头顶文本内容（显示玩家名称与角色）。
     */
    public void Refresh()
    {
        if (text == null) return;

        string nameStr = timelinePlayer != null ? timelinePlayer.playerName : "Player";
        string roleStr = playerRole != null ? playerRole.role.ToString() : "Role";
        text.text = $"{nameStr}\n[{roleStr}]";
    }
}
