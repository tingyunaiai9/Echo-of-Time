using UnityEngine;

/*
 * 拼画遮罩：标记遮罩位置和对应的碎片 ID
 */
public class PuzzleMask : MonoBehaviour
{
    [Tooltip("遮罩 ID（与碎片 ID 对应，0-99）")]
    public int maskId;

    /* 移除遮罩，备用函数 */
    public void RemoveMask()
    {
        gameObject.SetActive(false);
    }
}