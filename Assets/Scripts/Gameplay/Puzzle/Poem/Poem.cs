using UnityEngine;
using TMPro;

/// <summary>
/// 诗词拼图谜题管理器
/// 管理游戏进度和完成检测
/// </summary>
public class PoemPuzzleManager : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("需要匹配的总数")]
    public int totalNotesRequired = 5;

    [Header("UI引用")]
    public TMP_Text hintText;

    private int matchedCount = 0;

    void Start()
    {
        UpdateHintText();
    }

    /// <summary>
    /// 当有纸条匹配成功时调用
    /// </summary>
    public void OnNoteMatched()
    {
        matchedCount++;
        Debug.Log($"[PoemPuzzleManager] 已匹配: {matchedCount}/{totalNotesRequired}");

        UpdateHintText();

        // 检查是否完成
        if (matchedCount >= totalNotesRequired)
        {
            OnPuzzleCompleted();
        }
    }

    /// <summary>
    /// 谜题完成时调用
    /// </summary>
    private void OnPuzzleCompleted()
    {
        Debug.Log("[PoemPuzzleManager] 谜题完成！");
        
        if (hintText != null)
        {
            hintText.text = "恭喜！诗词拼接完成！";
            hintText.color = Color.green;
        }

        // 触发完成事件
        // EventBus.Publish(new PuzzleCompletedEvent { puzzleId = "poem_puzzle" });
    }

    /// <summary>
    /// 更新提示文本
    /// </summary>
    private void UpdateHintText()
    {
        if (hintText != null)
        {
            hintText.text = $"请将诗句拖拽到正确位置 ({matchedCount}/{totalNotesRequired})";
        }
    }
}