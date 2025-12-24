using UnityEngine;
using System.Collections.Generic;

public abstract class PuzzleManager : MonoBehaviour
{
    [Header("谜题设置")]
    [Tooltip("该谜题的唯一标识符")]
    public string puzzleID;

    [Tooltip("谜题名称")]
    public string puzzleName;

    [Header("谜题完成状态")]
    [Tooltip("Indicates whether the puzzle is completed.")]
    public bool isCompleted = false;

    // 所有谜题内置作弊功能，按 P 键直接完成谜题
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"[Puzzle] Checking if puzzle '{puzzleName}' is solved.");
            OnPuzzleCompleted();
        }
    }

    // 处理谜题完成的抽象方法
    public abstract void OnPuzzleCompleted();

}
        