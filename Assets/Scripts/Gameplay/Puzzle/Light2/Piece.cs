using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Piece : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Outline outline;
    private bool outlineOriginalEnabled;
    private Color outlineOriginalColor;
    private Outline[] childOutlines;
    private bool[] childOutlinesOriginalEnabled;
    private Color[] childOutlinesOriginalColor;
    private bool useChildOutlines = false;

    void Awake()
    {
        // 获取 Outline 组件并记录初始状态
        outline = GetComponent<Outline>();
        if (outline != null)
        {
            outlineOriginalEnabled = outline.enabled;
            outlineOriginalColor = outline.effectColor;
            outline.enabled = false; // 初始不显示
        }
        else
        {
            // 如果自身没有 Outline，尝试获取子对象的 Outline
            childOutlines = GetComponentsInChildren<Outline>(true);
            if (childOutlines != null && childOutlines.Length > 0)
            {
                useChildOutlines = true;
                childOutlinesOriginalEnabled = new bool[childOutlines.Length];
                childOutlinesOriginalColor = new Color[childOutlines.Length];
                
                for (int i = 0; i < childOutlines.Length; i++)
                {
                    childOutlinesOriginalEnabled[i] = childOutlines[i].enabled;
                    childOutlinesOriginalColor[i] = childOutlines[i].effectColor;
                    childOutlines[i].enabled = false; // 初始不显示
                }
                
                Debug.Log($"[Piece] {gameObject.name} 使用子对象的 {childOutlines.Length} 个 Outline 组件");
            }
            else
            {
                Debug.LogWarning($"[Piece] {gameObject.name} 及其子对象均未找到 Outline 组件");
            }
        }
    }

    // 鼠标进入时调用
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (outline != null)
        {
            outline.enabled = true;
            outline.effectColor = new Color(1f, 0.647f, 0f); // #FFA500
        }
        else if (useChildOutlines && childOutlines != null)
        {
            foreach (var childOutline in childOutlines)
            {
                if (childOutline != null)
                {
                    childOutline.enabled = true;
                    childOutline.effectColor = new Color(1f, 0.647f, 0f); // #FFA500
                }
            }
        }
    }

    // 鼠标离开时调用
    public void OnPointerExit(PointerEventData eventData)
    {
        if (outline != null)
        {
            outline.enabled = outlineOriginalEnabled;
            outline.effectColor = outlineOriginalColor;
        }
        else if (useChildOutlines && childOutlines != null)
        {
            for (int i = 0; i < childOutlines.Length; i++)
            {
                if (childOutlines[i] != null)
                {
                    childOutlines[i].enabled = childOutlinesOriginalEnabled[i];
                    childOutlines[i].effectColor = childOutlinesOriginalColor[i];
                }
            }
        }
    }

    // 实现 IPointerClickHandler 接口
    public void OnPointerClick(PointerEventData eventData)
    {
        // 点击时隐藏自身
        OnPointerExit(eventData); // 先恢复 Outline 状态
        gameObject.SetActive(false);
        Debug.Log($"[Piece] {gameObject.name} 已被点击并隐藏");
    }
}
