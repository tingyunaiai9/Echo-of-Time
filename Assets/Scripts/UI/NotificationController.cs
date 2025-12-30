/* UI/NotificationController.cs
 * 通用通知控制器
 * 处理游戏内的浮动提示信息显示、动画效果及生命周期管理
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Game.UI
{
    /*
     * 通知控制器类，单例模式
     * 负责管理全局通知UI的显示队列和动画逻辑
     */
    public class NotificationController : MonoBehaviour
    {
        public static NotificationController Instance { get; private set; }

        // 是否正在播放通知动画
        public bool IsShowing => currentCoroutine != null;

        [Header("UI References")]
        [Tooltip("The RectTransform of the notification panel (the object with the Image background)")]
        [SerializeField] private RectTransform notificationPanel;
        
        [Tooltip("The TextMeshPro component for the message")]
        [SerializeField] private TextMeshProUGUI notificationText;
        
        [Tooltip("CanvasGroup for fading effects (optional, will try to get from panel if null)")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [Tooltip("Starting position (e.g., off-screen bottom)")]
        [SerializeField] private Vector2 startPosition = new Vector2(0, -500);
        
        [Tooltip("Target position when visible (e.g., top-right)")]
        [SerializeField] private Vector2 targetPosition = new Vector2(400, 300);
        
        [Tooltip("Time to slide in")]
        [SerializeField] private float slideDuration = 0.5f;
        
        [Tooltip("Time to stay visible")]
        [SerializeField] private float stayDuration = 2.0f;
        
        [Tooltip("Time to fade out")]
        [SerializeField] private float fadeOutDuration = 0.5f;

        private Coroutine currentCoroutine;

        /*
         * 初始化单例及组件引用
         * 设置初始UI状态为隐藏
         */
        private void Awake()
        {
            if (Instance == null) Instance = this;
            
            // Auto-assign references if missing
            if (notificationPanel == null) notificationPanel = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
            
            // Initial state: hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.blocksRaycasts = false;
            }
            
            if (notificationPanel != null)
            {
                notificationPanel.anchoredPosition = startPosition;
            }
        }

        /*
         * 显示指定内容的通知
         * message: 要显示的文本内容
         */
        public void ShowNotification(string message)
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
                currentCoroutine = null;
            }
            currentCoroutine = StartCoroutine(NotificationSequence(message));
        }

        /*
         * 通知的完整动画序列协程
         * 包含：设置内容 -> 滑入 -> 停留 -> 淡出
         */
        private IEnumerator NotificationSequence(string message)
        {
            // 1. Setup content
            if (notificationText != null)
            {
                notificationText.text = message;
            }

            // Force layout rebuild to ensure the background resizes to the text immediately
            LayoutRebuilder.ForceRebuildLayoutImmediate(notificationPanel);

            // 2. Reset state for animation
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
            }
            notificationPanel.anchoredPosition = startPosition;

            // 3. Slide In (Bottom -> Target)
            float elapsed = 0;
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                // Smooth step easing
                t = t * t * (3f - 2f * t); 
                
                notificationPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                yield return null;
            }
            notificationPanel.anchoredPosition = targetPosition;

            // 4. Stay
            yield return new WaitForSeconds(stayDuration);

            // 5. Disappear (Fade out)
            elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                }
                yield return null;
            }
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
            }

            // 动画结束，标记为空闲
            currentCoroutine = null;
        }
        
        /*
         * 测试方法，用于在Inspector中手动触发通知
         */
        [ContextMenu("Test Notification")]
        public void TestNotification()
        {
            ShowNotification("这是一个测试提示信息。\nThis is a test notification message.");
        }
    }
}
