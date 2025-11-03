using UnityEngine;
using TMPro;
using UnityEngine.UI;

/*
 * 大纸条插槽组件
 * 接收小纸条并显示诗句
 */
public class LargeNoteSlot : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("该大纸条的唯一ID（LargeNote1, LargeNote2等）")]
    public string noteId;

    [Header("引用")]
    [Tooltip("用于显示诗句的文本组件")]
    public TMP_Text poemText;

    private bool isFilled = false;

    void Awake()
    {
        // 自动查找文本组件
        if (poemText == null)
        {
            poemText = GetComponentInChildren<TMP_Text>();
        }

        // 初始化为空白
        if (poemText != null)
        {
            poemText.text = "";
        }
    }

    /*
     * 设置诗句文本
     */
    public void SetPoemText(string text)
    {
        if (poemText != null && !isFilled)
        {
            poemText.text = text;
            isFilled = true;
            Debug.Log($"[LargeNoteSlot] {noteId} 已填充: {text}");
        }
    }

    /*
     * 检查是否已填充
     */
    public bool IsFilled()
    {
        return isFilled;
    }
}