/* UI/Diary/Diary.cs
 * 日记总面板控制脚本
 * 管理根节点的开关，以及 Shared/Clue 子页签的切换
 */
using System;
using UnityEngine;
using UnityEngine.UI;
using Events;

public class Diary : MonoBehaviour
{
    [Tooltip("根对象（用于显示/隐藏）")]
    public GameObject PanelRoot;

    [Header("子面板（挂在 DiaryPanel 下）")]
    public GameObject sharedPanel;   // SharedPanel
    public GameObject cluePanel;     // CluePanel

    [Header("控制按钮")]
    public Button sharedButton;      // SharedButton
    public Button clueButton;        // ClueButton
    public Button closeButton;       // CloseButton

    private static Diary s_instance;
    private static bool s_isOpen;

    // 静态根与子面板引用（用于跨实例管理）
    protected static GameObject s_root;
    protected static GameObject s_sharedPanel;
    protected static GameObject s_cluePanel;
    protected static Button s_sharedButton;
    protected static Button s_clueButton;
    protected static Button s_closeButton;
    protected static bool s_initialized = false;

    protected void Awake()
    {
        s_instance = this;
        if (PanelRoot == null)
            PanelRoot = gameObject;

        // 记录静态根，如果根发生变化则重置初始化状态（类似 Inventory.cs 行为）
        if (s_root == null || s_root != PanelRoot)
        {
            s_root = PanelRoot;
            s_initialized = false;
            s_isOpen = false;
        }

        // 绑定子面板与按钮的静态引用
        if (sharedPanel != null) s_sharedPanel = sharedPanel;
        if (cluePanel != null) s_cluePanel = cluePanel;
        if (sharedButton != null) s_sharedButton = sharedButton;
        if (clueButton != null) s_clueButton = clueButton;
        if (closeButton != null) s_closeButton = closeButton;

        // 按钮回调绑定（避免重复绑定）
        if (s_sharedButton != null)
        {
            s_sharedButton.onClick.RemoveListener(OnClickSharedButton);
            s_sharedButton.onClick.AddListener(OnClickSharedButton);
        }
        if (s_clueButton != null)
        {
            s_clueButton.onClick.RemoveListener(OnClickClueButton);
            s_clueButton.onClick.AddListener(OnClickClueButton);
        }
        if (s_closeButton != null)
        {
            s_closeButton.onClick.RemoveListener(ClosePanel);
            s_closeButton.onClick.AddListener(ClosePanel);
        }

        // 初始关闭面板，Start 会在所有实例就绪后真正关闭 root
        // ClosePanel();
    }

    protected virtual void Start()
    {
        if (!s_initialized && s_root != null && s_sharedPanel != null && s_cluePanel != null)
        {
            s_initialized = true;
            s_root.SetActive(false);
            Debug.Log($"[DiaryController.Start] 日志面板已初始化并关闭根节点");
        }
    }

    protected virtual void OnDestroy()
    {
        // 清理按钮回调
        if (s_sharedButton != null) s_sharedButton.onClick.RemoveListener(OnClickSharedButton);
        if (s_clueButton != null) s_clueButton.onClick.RemoveListener(OnClickClueButton);
        if (s_closeButton != null) s_closeButton.onClick.RemoveListener(ClosePanel);

        // 若当前实例绑定的根等于静态引用则清理静态状态
        if (PanelRoot != null && s_root == PanelRoot)
        {
            s_root = null;
            s_sharedPanel = null;
            s_cluePanel = null;
            s_sharedButton = null;
            s_clueButton = null;
            s_closeButton = null;
            s_isOpen = false;
            s_initialized = false;
        }
    }

    /* 按钮回调：打开 Shared 页（并显示根） */
    public void OnClickSharedButton()
    {
        OpenPanel();
        SwitchToShared();
    }

    /* 按钮回调：打开 Clue 页（并显示根） */
    public void OnClickClueButton()
    {
        OpenPanel();
        SwitchToClue();
    }

    public static void TogglePanel()
    {
        if (s_isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    public static void OpenPanel()
    {
        if (s_root == null) return;
        s_isOpen = true;
        s_root.SetActive(true);
        SwitchToShared();
        // 禁用玩家移动
        EventBus.LocalPublish(new FreezeEvent { isOpen = true });
    }

    public static void ClosePanel()
    {
        if (s_root == null) return;
        s_isOpen = false;
        s_root.SetActive(false);
        // 恢复玩家移动
        EventBus.LocalPublish(new FreezeEvent { isOpen = false });
    }

    /* 切换到 Shared 子面板（显示 sharedPanel，隐藏 cluePanel） */
    public static void SwitchToShared()
    {
        if (s_sharedPanel != null) s_sharedPanel.SetActive(true);
        if (s_cluePanel != null) s_cluePanel.SetActive(false);
    }

    /* 切换到 Clue 子面板（显示 cluePanel，隐藏 sharedPanel） */
    public static void SwitchToClue()
    {
        if (s_sharedPanel != null) s_sharedPanel.SetActive(false);
        if (s_cluePanel != null) s_cluePanel.SetActive(true);
    }
}