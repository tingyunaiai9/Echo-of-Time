using UnityEngine;
using UnityEngine.UI;

/*
 * 拼画遮罩：标记遮罩位置和对应的碎片 ID
 */
public class PuzzleMask : MonoBehaviour
{
    [HideInInspector]
    public int maskId;

    [Header("高光设置")]
    [Tooltip("高光颜色")]
    public Color highlightColor = Color.yellow;

    [Tooltip("正常颜色")]
    public Color normalColor = Color.white;

    private Image image;
    private Outline outline;

    void Awake()
    {
        image = GetComponent<Image>();
        outline = GetComponent<Outline>();

        // 如果没有 Outline 组件，自动添加
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
            outline.effectColor = highlightColor;
            outline.effectDistance = new Vector2(3, 3);
            outline.enabled = false; // 默认关闭
        }
    }

    /* 显示高光 */
    public void ShowHighlight()
    {
        if (outline != null)
        {
            outline.enabled = true;
        }
    }

    /* 隐藏高光 */
    public void HideHighlight()
    {
        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    /* 移除遮罩（备用函数） */
    public void RemoveMask()
    {
        gameObject.SetActive(false);
    }
}