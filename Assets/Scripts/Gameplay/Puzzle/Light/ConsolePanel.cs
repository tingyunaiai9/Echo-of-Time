using UnityEngine;
using UnityEngine.UI;

/*
 * 控制台面板管理器
 * 管理光线谜题完成后的控制台面板显示
 */
public class ConsolePanel : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("根对象（用于显示/隐藏）")]
    public GameObject PanelRoot;

    [Header("提示图像")]
    [Tooltip("提示图像对象")]
    public Image TipImage;

    // 静态变量
    private static ConsolePanel s_instance;
    private static GameObject s_root;
    private static bool s_isOpen; // 面板打开状态
    private static bool s_initialized = false;

    void Awake()
    {
        s_instance = this;

        // 如果未指定根对象，使用当前GameObject
        if (PanelRoot == null)
            PanelRoot = gameObject;

        // 记录静态根
        if (s_root == null || s_root != PanelRoot)
        {
            s_root = PanelRoot;
            s_initialized = true; // 标记为已初始化
        }
    }

    void OnDestroy()
    {
        // 清理静态引用
        if (PanelRoot != null && s_root == PanelRoot)
        {
            s_root = null;
            s_initialized = false;
            s_instance = null;
        }
        if (PanelRoot != null)
        {
            LeanTween.cancel(PanelRoot);
        }
    }

    // ============ 懒加载初始化 ============
    
    private static void EnsureInitialized()
    {
        // 如果已经初始化，直接返回
        if (s_initialized && s_instance != null && s_root != null)
            return;

        // 尝试在场景中查找 ConsolePanel（即使它是禁用状态）
        ConsolePanel[] allPanels = Resources.FindObjectsOfTypeAll<ConsolePanel>();
        
        foreach (var panel in allPanels)
        {
            // 排除预制体和其他场景的对象
            if (panel.gameObject.scene.isLoaded)
            {
                s_instance = panel;
                s_root = panel.PanelRoot != null ? panel.PanelRoot : panel.gameObject;
                s_initialized = true;
                return;
            }
        }
    }

    // ============ 静态面板控制方法 ============

    public static void TogglePanel()
    {
        EnsureInitialized(); // 确保已初始化
        
        if (s_isOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    /*
     * 打开控制台面板
     */
    public static void OpenPanel()
    {
        EnsureInitialized(); // 确保已初始化
        
        // 确保根对象被激活
        if (!s_root.activeSelf)
        {
            s_root.SetActive(true);
        }

        s_isOpen = true; // 更新面板状态

        // 播放提示图像动画
        if (s_instance != null && s_instance.TipImage != null)
        {
            RectTransform tipTransform = s_instance.TipImage.rectTransform;

            // 设置初始位置和透明度
            tipTransform.anchoredPosition = new Vector2(0, 0); // 屏幕中央
            s_instance.TipImage.color = new Color(1, 1, 1, 1); // 不透明
            s_instance.TipImage.gameObject.SetActive(true); // 确保图像激活

            // 动画：向上移动并渐变消失
            LeanTween.moveY(tipTransform, 200f, 1.5f).setEase(LeanTweenType.easeInOutQuad);
            LeanTween.alpha(tipTransform, 0f, 1.5f).setEase(LeanTweenType.easeInOutQuad).setOnComplete(() =>
            {
                // 动画完成后隐藏图像
                s_instance.TipImage.gameObject.SetActive(false);
            });
        }
    }

    /*
     * 关闭控制台面板
     */
    public static void ClosePanel()
    {
        EnsureInitialized(); // 确保已初始化

        s_root.SetActive(false);
        s_isOpen = false; 
    }
}