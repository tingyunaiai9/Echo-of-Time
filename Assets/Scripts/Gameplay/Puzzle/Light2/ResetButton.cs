using UnityEngine;
using UnityEngine.UI;

public class ResetButton : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("Pieces 父对象")]
    public GameObject piecesParent;

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
    }
}
