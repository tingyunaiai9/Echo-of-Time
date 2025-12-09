using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResetButton : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("Pieces 父对象")]
    public GameObject piecesParent;

    [Tooltip("Words 父对象")]
    public GameObject wordsParent;

    [Tooltip("重置后的文字颜色（黑色）")]
    public Color resetColor = Color.black;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnResetClicked);
        }
        else
        {
            Debug.LogWarning("[ResetButton] 未找到 Button 组件");
        }

        // 如果未手动指定，尝试自动查找 Pieces 对象
        if (piecesParent == null)
        {
            piecesParent = GameObject.Find("Pieces");
            if (piecesParent == null)
            {
                Debug.LogWarning("[ResetButton] 未找到 Pieces 对象，请在 Inspector 中手动指定");
            }
        }
        if (wordsParent == null)
        {
            wordsParent = GameObject.Find("Words");
            if (wordsParent == null)
            {
                Debug.LogWarning("[ResetButton] 未找到 Words 对象，请在 Inspector 中手动指定");
            }
        }
    }

    private void OnResetClicked()
    {
        if (piecesParent == null)
        {
            Debug.LogError("[ResetButton] Pieces 父对象未设置");
            return;
        }

        // 激活 Pieces 下的所有子对象
        int activatedCount = 0;
        foreach (Transform child in piecesParent.transform)
        {
            if (!child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
                activatedCount++;
            }
        }
        Debug.Log($"[ResetButton] 已重置 {activatedCount} 个碎片");

        // 将 Words 下的所有单词恢复为黑色
        if (wordsParent == null)
        {
            Debug.LogError("[ResetButton] Words 父对象未设置");
            return;
        }

        foreach (Transform child in wordsParent.transform)
        {
            // 获取 TextMeshProUGUI 组件
            TextMeshProUGUI textMeshPro = child.GetComponent<TextMeshProUGUI>();
            if (textMeshPro != null)
            {
                textMeshPro.color = resetColor;
                textMeshPro.ForceMeshUpdate();
            }

            // 重置 Word 脚本的 isActivated 标记
            Word wordScript = child.GetComponent<Word>();
            if (wordScript != null)
            {
                wordScript.isActivated = false;
            }
        }
        
        Debug.Log("[ResetButton] 已重置所有单词的颜色为黑色");
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnResetClicked);
        }
    }
}