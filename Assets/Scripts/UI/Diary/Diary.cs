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
    private static Diary s_instance;
    private static bool s_isOpen;

    // 静态根与子面板引用（用于跨实例管理）
    protected static GameObject s_root;
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
    }

    protected virtual void Start()
    {
        if (!s_initialized && s_root != null)
        {
            s_initialized = true;
            s_root.SetActive(false);
            Debug.Log($"[DiaryController.Start] 日志面板已初始化并关闭根节点");
        }
    }

    protected virtual void OnDestroy()
    {
        // 若当前实例绑定的根等于静态引用则清理静态状态
        if (PanelRoot != null && s_root == PanelRoot)
        {
            s_root = null;
            s_isOpen = false;
            s_initialized = false;
        }
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

}