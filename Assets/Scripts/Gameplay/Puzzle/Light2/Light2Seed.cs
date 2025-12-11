using UnityEngine;
using Game.UI;

namespace Game.Gameplay.Puzzle.Light2
{
    public class Light2Seed : Interaction
    {
        [Header("Item Settings")]
        public string itemId = "Light2Seed";
        public string itemName = "神秘种子";
        public string itemDescription = "一颗看起来很古老的种子。";
        public Sprite itemIcon;

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

            // Add to inventory
            InventoryItem newItem = new InventoryItem
            {
                itemId = this.itemId,
                itemName = this.itemName,
                description = this.itemDescription,
                quantity = 1,
                icon = this.itemIcon
            };

            Inventory.AddPropItem(newItem);

            if (notificationController != null)
            {
                notificationController.ShowNotification($"获得了 {itemName}。");
            }

            // Disable self
            gameObject.SetActive(false);
        }
    }
}
