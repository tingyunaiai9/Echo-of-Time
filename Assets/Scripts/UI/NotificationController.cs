using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Game.UI
{
    public class NotificationController : MonoBehaviour
    {
        public static NotificationController Instance { get; private set; }

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

        /// <summary>
        /// Shows the notification with the specified message.
        /// </summary>
        /// <param name="message">The text to display.</param>
        public void ShowNotification(string message)
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }
            currentCoroutine = StartCoroutine(NotificationSequence(message));
        }

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
        }
        
        // Test method to trigger from Inspector context menu
        [ContextMenu("Test Notification")]
        public void TestNotification()
        {
            ShowNotification("这是一个测试提示信息。\nThis is a test notification message.");
        }
    }
}
