using UnityEngine;

/*
 * 调查类：调查线索，仅反馈信息，不会消失
 */
public class Clue : Interaction
{
    [Header("线索内容")]
    [TextArea]
    public string clueText;

    [Tooltip("是否已被调查过")]
    public bool discovered;

    // 按 F 调查，不消失
    public override void OnInteract(PlayerController player)
    {
        if (!CheckPuzzleConditions()) return;

        discovered = true;
        string who = player != null ? player.gameObject.name : "Unknown";
        Debug.Log($"调查线索 -> 对象: {gameObject.name}, 玩家: {who}\n内容: {clueText}");

        // 可在此播放调查动画/音效，或高亮展示 UI
        // 示例：播放动画/音效占位
        // GetComponent<Animator>()?.SetTrigger("Inspect");
        // GetComponent<AudioSource>()?.Play();
    }
}