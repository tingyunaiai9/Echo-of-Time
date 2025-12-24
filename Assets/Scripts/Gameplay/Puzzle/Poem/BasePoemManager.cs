using UnityEngine;
using Events;

/*
 * 诗词谜题基类
 * 提供通用的计数、面板开关、冻结事件发布等逻辑
 * 具体的动画与完成逻辑由子类实现
 */
public abstract class BasePoemManager : PuzzleManager
{
    [Header("配置")]
    [Tooltip("需要匹配的总数")]
    public int totalNotesRequired = 5;

    [Header("提示面板")]
    [Tooltip("指南面板")]
    public TipManager tipPanel;

    protected int matchedCount = 0;

    // 静态变量（当前活动的诗词管理器单例）
    protected static BasePoemManager s_instance;
    protected static bool s_isOpen;
    protected static bool s_initialized = false;
    protected static bool s_isPuzzleCompleted = false;
    protected static bool s_tipShown = false;

    protected virtual void Awake()
    {
        s_instance = this;
        s_initialized = false;
        s_isOpen = false;
        s_isPuzzleCompleted = false;
        if (s_tipShown && tipPanel != null)
        {
            tipPanel.gameObject.SetActive(false);
        }
        s_tipShown = true;
        matchedCount = 0;

        // 让子类做额外的初始化（如关闭特定面板）
        InitializePanels();
    }

    protected virtual void Start()
    {
        // 场景加载时强制打开面板
        OpenPanel();
    }

    protected virtual void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
            s_isOpen = false;
            s_initialized = false;
            s_isPuzzleCompleted = false;
        }

        // 取消所有 LeanTween 动画
        if (PanelRoot != null)
        {
            LeanTween.cancel(PanelRoot);
        }

        // 确保在销毁时恢复玩家移动控制
        UIManager.Instance?.SetFrozen(false);
    }

    /*
     * 当有纸条匹配成功时调用
     */
    public virtual void OnNoteMatched()
    {
        matchedCount++;
        Debug.Log($"[{GetType().Name}] 已匹配: {matchedCount}/{totalNotesRequired}");

        // 检查是否完成
        if (matchedCount >= totalNotesRequired)
        {
            OnPuzzleCompleted();
        }
    }

    /*
     * 子类实现谜题完成后的具体逻辑
     */
    public abstract override void OnPuzzleCompleted();

    /*
     * 供子类在 Awake 时做额外初始化（默认无操作）
     */
    protected virtual void InitializePanels() { }

    /*
     * 标记谜题完成（供子类调用）
     */
    protected void MarkPuzzleCompleted()
    {
        s_isPuzzleCompleted = true;
    }

    // ============ 静态面板控制方法 ============

    public static void TogglePanel()
    {
        if (s_instance == null)
        {
            Debug.LogWarning("[BasePoemManager] 无法切换面板：实例为空");
            return;
        }

        if (s_isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    public static void OpenPanel()
    {
        if (s_instance == null)
        {
            Debug.LogWarning("[BasePoemManager] 无法打开面板：实例为空");
            return;
        }

        GameObject root = s_instance.PanelRoot;
        if (root == null)
        {
            Debug.LogWarning("[BasePoemManager] 无法打开面板：根对象为空");
            return;
        }

        s_isOpen = true;
        s_initialized = true;
        root.SetActive(true);

        // 如果谜题已完成，同时打开 DrawerPanel
        if (s_isPuzzleCompleted && s_instance.DrawerPanel != null)
        {
            s_instance.DrawerPanel.SetActive(true);
            Debug.Log("[BasePoemManager] 谜题已完成，同时打开 DrawerPanel");
        }

        // 禁用玩家移动
        UIManager.Instance?.SetFrozen(true);
    }

    public static void ClosePanel()
    {
        if (s_instance == null)
        {
            Debug.LogWarning("[BasePoemManager] 无法关闭面板：实例为空");
            return;
        }

        GameObject root = s_instance.PanelRoot;
        if (root == null)
        {
            Debug.LogWarning("[BasePoemManager] 无法关闭面板：根对象为空");
            return;
        }
        
        s_isOpen = false;
        root.SetActive(false);

        // 如果谜题已完成，同时关闭 DrawerPanel
        if (s_isPuzzleCompleted && s_instance.DrawerPanel != null)
        {
            s_instance.DrawerPanel.SetActive(false);
            Debug.Log("[BasePoemManager] 谜题已完成，同时关闭 DrawerPanel");
        }

        // 恢复玩家移动
        UIManager.Instance?.SetFrozen(false);
    }

    public static bool IsPuzzleCompleted()
    {
        return s_isPuzzleCompleted;
    }

    // 子类需要提供的面板引用
    protected abstract GameObject PanelRoot { get; }
    protected abstract GameObject DrawerPanel { get; }
}