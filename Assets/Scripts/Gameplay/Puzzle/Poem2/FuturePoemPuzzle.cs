using UnityEngine;
using Game.UI;

namespace Game.Gameplay.Puzzle.Poem2
{
    public class FuturePoemPuzzle : Interaction
    {
        [Header("Poem2 Settings")]
        public NotificationController notificationController;
        public string puzzleSceneName = "Poem2"; // The name of the puzzle scene to open

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

            if (Poem2NetManager.Instance == null)
            {
                Debug.LogError("Poem2Manager instance not found!");
                return;
            }

            bool ancientDone = Poem2NetManager.Instance.isScrollPlacedInAncient;
            bool modernDone = Poem2NetManager.Instance.isLockUnlockedInModern;

            if (!ancientDone)
            {
                if (notificationController != null)
                    notificationController.ShowNotification("没有竹简。\nNo bamboo scroll found.");
                return;
            }

            if (!modernDone)
            {
                if (notificationController != null)
                    notificationController.ShowNotification("匣子未解锁。\nThe box is not unlocked.");
                return;
            }

            // If both conditions are met, start the puzzle
            StartPuzzle();
        }

        private void StartPuzzle()
        {
            Debug.Log("Starting Poem Puzzle...");
            
            if (PuzzleOverlayManager.Instance != null)
            {
                PuzzleOverlayManager.Instance.OpenPuzzle(puzzleSceneName);
            }
            else
            {
                Debug.LogError("PuzzleOverlayManager not found!");
            }
        }
    }
}
