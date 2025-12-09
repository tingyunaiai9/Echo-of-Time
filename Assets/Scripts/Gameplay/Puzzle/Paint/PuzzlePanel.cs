using UnityEngine;
using Events;

/*
 * 拼画面板管理器：处理面板开关和成功反馈
 */
public class PuzzlePanel : MonoBehaviour
{
    [Tooltip("拼画面板 GameObject")]
    public GameObject puzzlePanel;

    [Header("成功反馈")]
    [Tooltip("拼图成功后显示的文字面板（包含剧情文字等）")]
    public GameObject successTextPanel;

    private static PuzzlePanel s_instance;
    private static bool s_isOpen = false;

    void Awake()
    {
        s_instance = this;
        Debug.Log("拼画面板管理器已初始化");

        // 初始化时确保文字面板是隐藏的
        if (successTextPanel != null)
        {
            successTextPanel.SetActive(false);
        }

        if (puzzlePanel != null)
        {
            puzzlePanel.SetActive(true);
            s_isOpen = true;
            
            // 初始冻结玩家
            EventBus.LocalPublish(new FreezeEvent { isOpen = true });
            
            Debug.Log("拼画面板已初始化为开启状态");
        }
    }

    void OnDestroy() 
    {
        // 销毁时解冻玩家
        EventBus.LocalPublish(new FreezeEvent { isOpen = false });

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

        // 发布冻结事件，背包打开时禁用玩家移动
        EventBus.LocalPublish(new FreezeEvent { isOpen = s_isOpen });

        Debug.Log($"拼画面板: {(s_isOpen ? "打开" : "关闭")}");
    }

    /* 供外部调用的方法，显示成功文字面板 */
    public void ShowSuccessPanel()
    {
        if (successTextPanel != null)
        {
            successTextPanel.SetActive(true);
            Debug.Log("拼图完成，显示文字面板");
        }
        else
        {
            Debug.LogWarning("PuzzlePanel: 未设置 Success Text Panel！");
        }
    }
}