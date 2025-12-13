using UnityEngine;
using Game.UI;

namespace Game.Gameplay.Puzzle.Poem2
{
    public class AncientScrollBox : Interaction
    {
        [Header("Poem2 Settings")]
        public NotificationController notificationController;
        //public GameObject scrollVisualInBox; // The scroll visual inside the box (initially hidden)
        public string requiredItemId = "BambooScroll"; // The ID of the item required

        // Assuming we might need to check if player has the item, but for now we'll just simulate the action
        // or assume the player has it if they are at this stage.
        // If you have an Inventory system, check it here.

        private bool hasPlacedScroll = false;

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

            if (hasPlacedScroll)
            {
                if (notificationController != null)
                    notificationController.ShowNotification("竹简已经放好了。\nThe scroll is already placed.");
                return;
            }

            // Logic to place scroll
            // Check inventory for the required item
            if (PropBackpack.GetPropCount(requiredItemId) <= 0)
            {
                if (notificationController != null)
                    notificationController.ShowNotification("你需要先找到竹简。\nYou need to find the bamboo scroll first.");
                return;
            }
            
            // 2. Place scroll
            hasPlacedScroll = true;
            
            //if (scrollVisualInBox != null)
            //    scrollVisualInBox.SetActive(true);

            // 3. Update Manager
            if (Poem2NetManager.Instance != null)
            {
                Poem2NetManager.Instance.CmdSetScrollPlaced(true);
            }

            if (notificationController != null)
                notificationController.ShowNotification("已将竹简放入匣子。\nPlaced the bamboo scroll into the box.");
        }
    }
}
