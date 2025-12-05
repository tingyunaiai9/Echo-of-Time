/* UI/Diary/Diary.cs
 * 日记总面板控制脚本
 * 管理根节点的开关，以及 Shared/Clue 子页签的切换
 */
using UnityEngine;
using TMPro;

public class Diary : MonoBehaviour
{
    [Tooltip("根对象（用于显示/隐藏）")]
    public GameObject PanelRoot;
    
    [Tooltip("时间线类型文本")]
    public TMP_Text TypeText;

    private static Diary s_instance;
    private static GameObject s_root;
    private static bool s_isOpen;

    void Awake()
    {
        s_instance = this;
        
        if (PanelRoot == null)
            PanelRoot = gameObject;

        s_root = PanelRoot;
        
        InitializeDiary();
    }

    void Start()
    {
        // 设置时间线文本
        if (TypeText != null && TimelinePlayer.Local != null)
        {
            TypeText.text = TimelinePlayer.Local.timeline switch
            {
                0 => "鲲之诗篇",
                1 => "梦之画卷",
                2 => "JS?N",
                _ => "时间的回声"
            };
        } else
        {
            Debug.LogWarning("[Diary] 未能设置时间线文本，TypeText 或 TimelinePlayer.Local 为空");
        }
    }

    private void InitializeDiary()
    {
        // 确保面板激活以便访问子组件
        if (!s_root.activeSelf)
            s_root.SetActive(true);
        
        
        // 初始化完成后关闭面板
        s_root.SetActive(false);
        s_isOpen = false;
        
        Debug.Log("[Diary] 日记面板已初始化并关闭");
    }

    void OnDestroy()
    {
        if (s_root == PanelRoot)
        {
            s_root = null;
            s_instance = null;
            s_isOpen = false;
        }
    }
}