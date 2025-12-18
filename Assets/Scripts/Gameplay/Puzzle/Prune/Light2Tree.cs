using UnityEngine;
using Game.UI;

namespace Game.Gameplay.Puzzle.Light2
{
    public class Light2Tree : Interaction
    {
        public enum TimelineType
        {
            Republic,
            Future
        }

        [Header("Settings")]
        public TimelineType timeline;
        public string puzzleSceneName = "Light2"; // For Republic
        public GameObject screenObject; // For Future

        [Header("Notification")]
        public NotificationController notificationController;

        protected override void Start()
        {
            base.Start();
            if (notificationController == null)
            {
                notificationController = NotificationController.Instance;
                if (notificationController == null)
                {
                    notificationController = FindFirstObjectByType<NotificationController>();
                }
            }
        }

        public override void OnInteract(PlayerController player)
        {
            base.OnInteract(player);

            if (timeline == TimelineType.Republic)
            {
                // Enter Light2 Puzzle
                Debug.Log("Entering Light2 Puzzle...");
                if (PuzzleOverlayManager.Instance != null)
                {
                    PuzzleOverlayManager.Instance.OpenPuzzle(puzzleSceneName);
                }
                else
                {
                    Debug.LogError("PuzzleOverlayManager not found!");
                }
            }
            else if (timeline == TimelineType.Future)
            {
                // Activate Screen
                if (screenObject != null)
                {
                    if (!screenObject.activeSelf)
                    {
                        screenObject.SetActive(true);
                        if (notificationController != null)
                            notificationController.ShowNotification("屏幕亮了起来。\nThe screen lit up.");
                    }
                    else
                    {
                        if (notificationController != null)
                            notificationController.ShowNotification("屏幕已经亮着了。\nThe screen is already on.");
                    }
                }
            }
        }
    }
}
