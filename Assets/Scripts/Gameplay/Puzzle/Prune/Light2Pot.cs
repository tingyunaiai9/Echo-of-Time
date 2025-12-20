using UnityEngine;
using Game.UI;

namespace Game.Gameplay.Puzzle.Light2
{
    public class Light2Pot : Interaction
    {
        public enum TimelineType
        {
            Ancient,
            Republic,
            Future
        }

        [Header("Settings")]
        public TimelineType timeline;
        public string seedItemId = "Light2Seed";
        public GameObject treeObject; // The tree object to enable in Republic/Future

        [Header("Notification")]
        public NotificationController notificationController;

        [Header("Debug")]
        public bool testMode = false;

        private bool localIsPlanted = false;
        private Collider m_Collider;

        protected override void Start()
        {
            base.Start();
            m_Collider = GetComponent<Collider>();

            if (notificationController == null)
            {
                notificationController = NotificationController.Instance;
                if (notificationController == null)
                {
                    notificationController = FindFirstObjectByType<NotificationController>();
                }
            }
        }

        private void Update()
        {
            if (Light2Manager.Instance == null) return;

            bool isPlanted = Light2Manager.Instance.isSeedPlanted;

            // Handle Tree Visibility for Republic and Future
            if (timeline == TimelineType.Republic || timeline == TimelineType.Future)
            {
                bool shouldShow = false;
                if (treeObject != null)
                {
                    shouldShow = isPlanted || testMode;
                    if (treeObject.activeSelf != shouldShow)
                    {
                        treeObject.SetActive(shouldShow);
                    }
                }

                // If planted (tree shown), disable this interaction so player interacts with the tree instead
                if (m_Collider != null)
                {
                    m_Collider.enabled = !shouldShow;
                }
            }
        }

        public override void OnInteract(PlayerController player)
        {
            if (Light2Manager.Instance == null) return;

            bool isPlanted = Light2Manager.Instance.isSeedPlanted;

            if (timeline == TimelineType.Ancient)
            {
                if (isPlanted)
                {
                    if (notificationController != null)
                        notificationController.ShowNotification("种子已经种下了。\nThe seed is already planted.");
                    return;
                }

                if (Inventory.HasPropItem(seedItemId))
                {
                    // Plant the seed
                    Light2Manager.Instance.CmdPlantSeed();
                    if (notificationController != null)
                        notificationController.ShowNotification("种下了种子。\nPlanted the seed.");
                    
                    // Optional: Remove seed from inventory?
                    // Inventory.RemoveItem(seedItemId); // If such method exists
                }
                else
                {
                    if (notificationController != null)
                        notificationController.ShowNotification("这里似乎可以种点什么。\nSeems like something could be planted here.");
                }
            }
            else // Republic or Future
            {
                if (isPlanted)
                {
                    // If tree is active, we shouldn't be hitting the pot interaction usually, 
                    // but if we do, we can direct them to the tree or just do nothing.
                    // Or maybe the tree is small and we still hit the pot?
                    // User said: "Republic interact with tree... Future interact with tree".
                    // So we assume the tree has its own interaction.
                    return; 
                }
                else
                {
                    // Before Ancient plants seed
                    if (notificationController != null)
                        notificationController.ShowNotification("一个空花盆。\nAn empty flower pot.");
                }
            }
        }
    }
}
