using UnityEngine;
using UnityEngine.UI;

public class ExitButton : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnExitClicked);
    }

    private void OnExitClicked()
    {
        // 查找Canvas下的PoemManager并调用TogglePanel
        PoemManager poemManager = FindFirstObjectByType<PoemManager>();
        if (poemManager != null)
        {
            PoemManager.TogglePanel();
        }
        else
        {
            Debug.LogWarning("[ExitButton] 未找到PoemManager实例");
        }

        // 查找Canvas下的DrawerPanel并关闭
        DrawerPanel drawerPanel = FindFirstObjectByType<DrawerPanel>();
        if (drawerPanel != null)
        {
            drawerPanel.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[ExitButton] 未找到DrawerPanel实例");
        }
    }
}
