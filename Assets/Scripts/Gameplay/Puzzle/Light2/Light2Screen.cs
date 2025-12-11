using UnityEngine;
using Game.UI;

namespace Game.Gameplay.Puzzle.Light2
{
    public class Light2Screen : Interaction
    {
        [Header("Content")]
        [TextArea(3, 10)]
        public string screenContent = "屏幕上显示着一些字样...\nSome words are displayed on the screen...";

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

            if (notificationController != null)
            {
                notificationController.ShowNotification(screenContent);
            }
        }
    }
}
