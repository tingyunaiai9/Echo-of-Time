using System;
using UnityEngine;
using Events;

public class DiaryController : MonoBehaviour
{
    [Tooltip("线索面板根对象（用于显示/隐藏）")]
    public GameObject cluePanelRoot;

    private static DiaryController s_instance;
    private static bool s_isOpen;

    protected void Awake()
    {
        s_instance = this;
        if (cluePanelRoot == null)
            cluePanelRoot = gameObject;
        ClosePanel();
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
        if (s_instance == null || s_instance.cluePanelRoot == null) return;
        s_isOpen = true;
        s_instance.cluePanelRoot.SetActive(true);
        // 禁用玩家移动
        EventBus.Instance.LocalPublish(new FreezeEvent { isOpen = true });
    }

    public static void ClosePanel()
    {
        if (s_instance == null || s_instance.cluePanelRoot == null) return;
        s_isOpen = false;
        s_instance.cluePanelRoot.SetActive(false);
        // 恢复玩家移动
        EventBus.Instance.LocalPublish(new FreezeEvent { isOpen = false });
    }
}