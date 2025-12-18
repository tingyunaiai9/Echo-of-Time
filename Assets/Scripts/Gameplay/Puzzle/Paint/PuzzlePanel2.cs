using UnityEngine;
using Events;

/*
 * 拼画面板管理器：处理面板开关
 */
public class PuzzlePanel2 : MonoBehaviour
{
    [Tooltip("拼画面板 GameObject")]
    public GameObject puzzlePanel;

    private static PuzzlePanel2 s_instance;
    private static bool s_isOpen = false;

    void Awake()
    {
        s_instance = this;
        Debug.Log("拼画面板管理器已初始化");

        if (puzzlePanel != null)
        {
            puzzlePanel.SetActive(true);
            s_isOpen = true;
            
            // 初始冻结玩家
            UIManager.Instance?.SetFrozen(true);
            
            Debug.Log("拼画面板已初始化为开启状态");
        }
    }

    void OnDestroy() 
    {
        // 销毁时解冻玩家
        UIManager.Instance?.SetFrozen(false);

        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    /* 切换面板 */
    public static void TogglePanel()
    {
        if (s_instance == null || s_instance.puzzlePanel == null) return;

        s_isOpen = !s_isOpen;
        s_instance.puzzlePanel.SetActive(s_isOpen);

        // 更新冻结状态
        UIManager.Instance?.SetFrozen(s_isOpen);

        Debug.Log($"拼画面板: {(s_isOpen ? "打开" : "关闭")}");
    }
}