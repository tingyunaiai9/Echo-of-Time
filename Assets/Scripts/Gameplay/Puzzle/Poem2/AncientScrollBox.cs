/* Gameplay/Puzzle/Poem2/AncientScrollBox.cs
 * 古代竹简匣交互逻辑
 * 处理玩家将竹简放入匣子的交互，并同步状态到网络管理器
 */

using UnityEngine;
using Game.UI;

namespace Game.Gameplay.Puzzle.Poem2
{
    /*
     * 古代竹简匣类
     * 继承自Interaction，用于检测玩家背包物品并触发谜题状态更新
     */
    public class AncientScrollBox : Interaction
    {
        [Header("Poem2 Settings")]
        public NotificationController notificationController;
        //public GameObject scrollVisualInBox; // 匣子内的竹简模型（初始隐藏）
        public string requiredItemId = "BambooScroll"; // 所需物品ID

        private bool hasPlacedScroll = false;

        /*
         * 初始化，获取通知控制器引用
         */
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

        /*
         * 玩家交互处理
         * 检查背包是否有竹简，若有则放入并通知服务器
         */
        public override void OnInteract(PlayerController player)
        {
            base.OnInteract(player);

            if (hasPlacedScroll)
            {
                if (notificationController != null)
                    notificationController.ShowNotification("竹简已经放好了。\nThe scroll is already placed.");
                return;
            }

            // 检查背包中是否有竹简
            if (PropBackpack.GetPropCount(requiredItemId) <= 0)
            {
                if (notificationController != null)
                    notificationController.ShowNotification("你需要先找到竹简。\nYou need to find the bamboo scroll first.");
                return;
            }
            
            // 放置竹简
            hasPlacedScroll = true;
            
            //if (scrollVisualInBox != null)
            //    scrollVisualInBox.SetActive(true);

            // 更新网络管理器状态
            if (Poem2NetManager.Instance != null)
            {
                Poem2NetManager.Instance.CmdSetScrollPlaced(true);
            }

            if (notificationController != null)
                notificationController.ShowNotification("已将竹简放入匣子。\nPlaced the bamboo scroll into the box.");
        }
    }
}
