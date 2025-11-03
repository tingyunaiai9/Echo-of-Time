using UnityEngine;
using TMPro;
using Events;

/*
 * 诗词谜题管理器
 * 管理游戏进度和完成检测，支持静态方法控制面板开关
 */
public class PoemManager : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("需要匹配的总数")]
    public int totalNotesRequired = 5;

    [Tooltip("根对象（用于显示/隐藏）")]
    public GameObject PanelRoot;

    [Header("UI引用")]
    public TMP_Text hintText;

    private int matchedCount = 0;

    // 静态变量
    private static PoemManager s_instance;
    private static bool s_isOpen;
    private static GameObject s_root;
    private static bool s_initialized = false;

    void Awake()
    {
        s_instance = this;
        
        // 如果未指定根对象,使用当前GameObject
        if (PanelRoot == null)
            PanelRoot = gameObject;

        // 记录静态根,如果根发生变化则重置初始化状态
        if (s_root == null || s_root != PanelRoot)
        {
            s_root = PanelRoot;
            s_initialized = false;
            s_isOpen = false;
        }
    }

    void Start()
    {
        // 初始化时关闭面板
        if (!s_initialized && s_root != null)
        {
            s_initialized = true;
            s_root.SetActive(false);
            Debug.Log("[PoemManager.Start] 诗词面板已初始化并关闭");
        }
    }

    void OnDestroy()
    {
        // 若当前实例绑定的根等于静态引用则清理静态状态
        if (PanelRoot != null && s_root == PanelRoot)
        {
            s_root = null;
            s_isOpen = false;
            s_initialized = false;
            s_instance = null;
        }
    }

    /*
     * 当有纸条匹配成功时调用
     */
    public void OnNoteMatched()
    {
        matchedCount++;
        Debug.Log($"[PoemManager] 已匹配: {matchedCount}/{totalNotesRequired}");

        // 检查是否完成
        if (matchedCount >= totalNotesRequired)
        {
            OnPuzzleCompleted();
        }
    }

    /*
     * 谜题完成时调用
     */
    private void OnPuzzleCompleted()
    {
        Debug.Log("[PoemManager] 谜题完成！");
        
        if (hintText != null)
        {
            hintText.text = "恭喜！诗词拼接完成！";
            hintText.color = Color.green;
        }

        // 触发完成事件
        // EventBus.Publish(new PuzzleCompletedEvent { puzzleId = "poem_puzzle" });
    }

    // ============ 静态面板控制方法 ============

    /*
     * 切换面板开关状态
     */
    public static void TogglePanel()
    {
        if (s_isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    /*
     * 打开诗词谜题面板
     */
    public static void OpenPanel()
    {
        if (s_root == null)
        {
            Debug.LogWarning("[PoemManager] 无法打开面板：根对象为空");
            return;
        }

        s_isOpen = true;
        s_root.SetActive(true);
        Debug.Log("[PoemManager] 诗词面板已打开");

        // 禁用玩家移动
        EventBus.LocalPublish(new FreezeEvent { isOpen = true });
    }

    /*
     * 关闭诗词谜题面板
     */
    public static void ClosePanel()
    {
        if (s_root == null)
        {
            Debug.LogWarning("[PoemManager] 无法关闭面板：根对象为空");
            return;
        }

        s_isOpen = false;
        s_root.SetActive(false);
        Debug.Log("[PoemManager] 诗词面板已关闭");

        // 恢复玩家移动
        EventBus.LocalPublish(new FreezeEvent { isOpen = false });
    }

    /*
     * 获取面板开启状态
     */
    public static bool IsOpen()
    {
        return s_isOpen;
    }

    /*
     * 重置谜题进度（用于重新开始）
     */
    public static void ResetPuzzle()
    {
        if (s_instance != null)
        {
            s_instance.matchedCount = 0;
            Debug.Log("[PoemManager] 谜题已重置");
        }
    }
}