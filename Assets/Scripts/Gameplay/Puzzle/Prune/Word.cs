using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Word : MonoBehaviour
{
    [Tooltip("遮挡该 Word 的 Group 对象父对象")]
    public GameObject Pieces;

    [Tooltip("该 Word 对应的 Group 索引列表")]
    public List<int> coverGroups = new List<int> {1}; // 在 Unity Inspector 中指定遮挡文字的 Group 索引列表

    [Tooltip("金黄色颜色值")]
    public Color goldenColor = new Color(1f, 0.84f, 0f, 1f); // 金黄色

    [Tooltip("Outline 组件加粗的宽度")]
    public float outlineWidth = 1.5f;

    private TextMeshProUGUI textMeshPro; // 改为 TextMeshProUGUI
    private Outline outline;
    public bool isActivated = false; // 用于外部检查该 Word 是否已被激活为金黄色

    void Awake()
    {
        // 获取 TextMeshProUGUI 组件（UI 版本）
        textMeshPro = GetComponent<TextMeshProUGUI>();
        
        // 获取 Outline 组件
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            Debug.LogWarning($"[Word] {gameObject.name} 缺少 Outline 组件（非必需）");
        }
        
        // 检查 Pieces 是否已设置
        if (Pieces == null)
        {
            Debug.LogError($"[Word] {gameObject.name} 的 Pieces 未设置！");
        }
    }

    void Update()
    {
        // 检查所有对应的 Group 是否都被禁用
        if (!isActivated && AreAllGroupsInactive())
        {
            SetWordToGolden();
        }
    }

    // 检查所有 Group 是否都被禁用
    private bool AreAllGroupsInactive()
    {
        if (Pieces == null || coverGroups == null || coverGroups.Count == 0)
        {
            return false;
        }

        foreach (int groupIndex in coverGroups)
        {
            // 查找 Pieces 下的 Group{groupIndex}
            Transform groupTransform = Pieces.transform.Find($"Group{groupIndex}");
            
            if (groupTransform == null)
            {
                Debug.LogWarning($"[Word] {gameObject.name} 找不到 Pieces/Group{groupIndex}");
                return false;
            }

            // 如果该 Group 是激活的，则返回 false
            if (groupTransform.gameObject.activeSelf)
            {
                return false;
            }
        }

        // 所有指定的 Group 都被禁用
        Debug.Log($"[Word] {gameObject.name} 的所有遮挡 Group 都已禁用");
        return true;
    }

    // 设置 Word 为金黄色并加粗 Outline
    private void SetWordToGolden()
    {
        if (textMeshPro != null)
        {
            // 设置文本颜色为金黄色
            textMeshPro.color = goldenColor;
            
            // 强制更新
            textMeshPro.ForceMeshUpdate();
            
            Debug.Log($"[Word] {gameObject.name} 已设置为金黄色，当前颜色: {textMeshPro.color}");
        }

        if (outline != null)
        {
            outline.effectDistance = new Vector2(outlineWidth, outlineWidth); // 加粗 Outline
            Debug.Log($"[Word] {gameObject.name} Outline 已加粗至 {outlineWidth}");
        }

        isActivated = true; // 标记已经变为金黄色，避免重复设置
    }
}