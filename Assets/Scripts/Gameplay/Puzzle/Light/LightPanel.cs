using UnityEngine;
using Events;

/*
 * 光线谜题管理器
 * 管理游戏进度和完成检测，支持静态方法控制面板开关
 */
public class LightPanel : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("根对象（用于显示/隐藏）")]
    public GameObject PanelRoot;

    // 静态变量
    private static LightPanel s_instance;
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
    }

    void Start()
    {
        // 初始化时关闭面板
        if (!s_initialized && s_root != null)
        {
            s_initialized = true;
            s_root.SetActive(false);
            Debug.Log("[LightPanel.Start] 光线面板已初始化并关闭");
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
            s_isPuzzleCompleted = false; // 清理完成标志
        }
    }

    /*
     * 谜题完成时调用
     */
    public void OnPuzzleCompleted()
    {
        Debug.Log("[LightPanel] 谜题完成！");

        // 设置完成标志
        s_isPuzzleCompleted = true;

        // 在这里添加完成后的逻辑（例如播放动画、显示提示等）
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
     * 打开光线谜题面板
     */
    public static void OpenPanel()
    {
        if (s_root == null)
        {
            Debug.LogWarning("[LightPanel] 无法打开面板：根对象为空");
            return;
        }

        s_isOpen = true;
        s_root.SetActive(true);
        Debug.Log("[LightPanel] 面板已打开");

        // 禁用玩家移动
        EventBus.LocalPublish(new FreezeEvent { isOpen = true });
    }

    /*
     * 关闭光线谜题面板
     */
    public static void ClosePanel()
    {
        if (s_root == null)
        {
            Debug.LogWarning("[LightPanel] 无法关闭面板：根对象为空");
            return;
        }
        
        s_isOpen = false;
        s_root.SetActive(false);
        Debug.Log("[LightPanel] 面板已关闭");

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

    /*
     * 获取面板是否打开
     */
    public static bool IsOpen()
    {
        return s_isOpen;
    }
}