using UnityEngine;
using TMPro;
using Events;
using System.Collections;

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

    [Header("动画配置")]
    [Tooltip("完成后的目标面板（DrawerPanel）")]
    public GameObject DrawerPanel;

    [Tooltip("向上移动的动画时长（秒）")]
    public float animationDuration = 1f;

    [Tooltip("动画缓动类型")]
    public LeanTweenType easeType = LeanTweenType.easeInOutQuad;

    private int matchedCount = 0;

    // 静态变量
    private static PoemManager s_instance;
    private static bool s_isOpen;
    private static GameObject s_root;
    private static bool s_initialized = false;
    
    // 谜题完成标志
    private static bool s_isPuzzleCompleted = false;

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
            s_isPuzzleCompleted = false; // 重置完成标志
        }

        // 确保 DrawerPanel 初始时关闭
        if (DrawerPanel != null)
        {
            DrawerPanel.SetActive(false);
        }
    }

    void Start()
    {
        // 场景加载时强制打开面板
        // 无论之前状态如何，只要作为谜题场景加载，就应该显示
        if (s_root != null)
        {
            s_root.SetActive(true);
            s_isOpen = true;
            s_initialized = true;
            Debug.Log("[PoemManager.Start] 场景加载完成，强制打开面板");
            
            // 确保发布冻结事件（以防万一）
            EventBus.LocalPublish(new FreezeEvent { isOpen = true });
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
            s_isPuzzleCompleted = false; 
        }

        // 取消所有LeanTween动画
        LeanTween.cancel(PanelRoot);
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
        Debug.Log("[PoemManager] 谜题完成！开始播放动画");

        // 设置完成标志
        s_isPuzzleCompleted = true;

        // 获取 PoemPanel 的 RectTransform
        RectTransform poemRect = PanelRoot.GetComponent<RectTransform>();

        // 计算向上移动的距离（2/3的高度）
        float moveDistance = poemRect.rect.height * 2f / 3f;
        Vector2 targetPosition = poemRect.anchoredPosition + new Vector2(0, moveDistance);

        // 激活 DrawerPanel
        if (DrawerPanel != null)
        {
            DrawerPanel.SetActive(true);
        }

        // 使用 LeanTween 播放向上移动动画
        LeanTween.value(PanelRoot, poemRect.anchoredPosition, targetPosition, animationDuration)
            .setOnUpdate((Vector2 val) =>
            {
                poemRect.anchoredPosition = val;
            })
            .setEase(easeType)
            .setOnComplete(() =>
            {
                Debug.Log("[PoemManager] 动画完成");
            });
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

        // 如果谜题已完成，同时打开 DrawerPanel
        if (s_isPuzzleCompleted && s_instance != null && s_instance.DrawerPanel != null)
        {
            s_instance.DrawerPanel.SetActive(true);
            Debug.Log("[PoemManager] 谜题已完成，同时打开 DrawerPanel");
        }

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

        // 如果谜题已完成，同时关闭 DrawerPanel
        if (s_isPuzzleCompleted && s_instance != null && s_instance.DrawerPanel != null)
        {
            s_instance.DrawerPanel.SetActive(false);
            Debug.Log("[PoemManager] 谜题已完成，同时关闭 DrawerPanel");
        }

        // 恢复玩家移动
        EventBus.LocalPublish(new FreezeEvent { isOpen = false });
    }

    /*
     * 获取谜题是否已完成
     */
    public static bool IsPuzzleCompleted()
    {
        return s_isPuzzleCompleted;
    }
}