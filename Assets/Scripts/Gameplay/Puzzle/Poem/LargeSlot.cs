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
    [Tooltip("该大纸条的唯一ID")]
    public string noteId;

    [Header("引用")]
    [Tooltip("用于显示诗句的文本组件")]
    public TMP_Text poemText;

    [Tooltip("诗句颜色")]
    public Color poemColor = Color.white;

    [Tooltip("边框图片组件（可以是自身的 Outline 或独立的 Image）")]
    public Outline borderOutline;

    [Header("高亮配置")]
    [Tooltip("正常状态边框宽度")]
    public Vector2 normalBorderWidth = new Vector2(2, 2);

    [Tooltip("高亮状态边框宽度")]
    public Vector2 highlightBorderWidth = new Vector2(5, 5);

    [Tooltip("正常状态边框颜色")]
    public Color normalBorderColor = Color.white;

    [Tooltip("高亮状态边框颜色")]
    public Color highlightBorderColor = Color.yellow;

    [Tooltip("匹配成功边框颜色")]
    public Color matchBorderColor = Color.green;

    private bool isFilled = false;
    private bool isHighlighted = false;

    void Awake()
    {
        // 初始化文本组件
        poemText = GetComponentInChildren<TMP_Text>();
        poemText.text = "";
        // 边框组件
        borderOutline = GetComponent<Outline>();
        // 设置初始边框样式
        ResetBorder();
    }

    /*
     * 设置诗句文本
     */
    public void SetPoemText(string text)
    {
        if (poemText != null && !isFilled)
        {
            poemText.text = text;
            poemText.color = poemColor;
            isFilled = true;
            Debug.Log($"[LargeNoteSlot] {noteId} 已填充: {text}");
        }
    }

    /*
     * 高亮边框（拖拽检测时）
     */
    public void HighlightBorder()
    {
        if (isFilled || isHighlighted) return;

        isHighlighted = true;
        
        if (borderOutline != null)
        {
            borderOutline.effectDistance = highlightBorderWidth;
            borderOutline.effectColor = highlightBorderColor;
        }

        Debug.Log($"[LargeNoteSlot] {noteId} 边框高亮");
    }

    /*
     * 重置边框为正常状态
     */
    public void ResetBorder()
    {
        if (isFilled) return;

        isHighlighted = false;

        if (borderOutline != null)
        {
            borderOutline.effectDistance = normalBorderWidth;
            borderOutline.effectColor = normalBorderColor;
        }
    }

    /*
     * 设置为匹配成功状态（绿色加粗边框）
     */
    public void SetMatchedBorder()
    {
        if (borderOutline != null)
        {
            borderOutline.effectDistance = highlightBorderWidth;
            borderOutline.effectColor = matchBorderColor;
        }

        Debug.Log($"[LargeNoteSlot] {noteId} 匹配成功，边框变绿");
    }

    /*
     * 检查是否已填充
     */
    public bool IsFilled()
    {
        return isFilled;
    }
}