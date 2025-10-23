using UnityEngine;
using UnityEngine.UI;
using Events;

/*
 控制日记页面的显示与隐藏，并通过事件禁用玩家移动
 挂载于 Canvas/DiaryPanel
*/
public class Diary : MonoBehaviour
{
    [Tooltip("日记内容文本组件")]
    public Text diaryText;

    [Tooltip("日记面板根对象（用于显示/隐藏）")]
    public GameObject panelRoot;

    // 静态引用和状态
    private static GameObject s_root;
    private static Diary s_instance;
    private static bool s_isOpen;

    protected virtual void Awake()
    {
        Debug.Log("[Diary] Awake 执行");
        if (panelRoot == null)
            panelRoot = gameObject;
        s_root = panelRoot;
        s_instance = this;
        CloseDiary();
    }

    // 静态切换方法
    public static void ToggleDiary()
    {
        if (s_isOpen)
            CloseDiary();
        else
            OpenDiary();
    }

    public static void OpenDiary()
    {
        if (s_root == null || s_instance == null) return;
        s_isOpen = true;
        s_root.SetActive(true);

        // 禁用玩家移动（通过背包事件实现，PlayerController 已支持此事件）
        EventBus.Instance.Publish(new BackpackStateChangedEvent { isOpen = true });

        // 示例内容
        if (s_instance.diaryText != null)
            s_instance.diaryText.text = "这里是你的冒险日记。\n\n- 2025/10/21：发现了时间回声的秘密。";
    }

    public static void CloseDiary()
    {
        if (s_root == null) return;
        s_isOpen = false;
        s_root.SetActive(false);

        // 恢复玩家移动
        EventBus.Instance.Publish(new BackpackStateChangedEvent { isOpen = false });
    }
}