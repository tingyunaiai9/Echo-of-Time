using UnityEngine;
using Game.UI;

namespace Game.Gameplay.Puzzle.Light2
{
    public class Light2Screen : Interaction
    {
        [Header("UI Settings")]
        [Tooltip("The Canvas or UI Panel to show when interacting with the screen")]
        public GameObject screenCanvas;

        protected override void Start()
        {
            base.Start();
            
            // Ensure canvas is hidden at start
            if (screenCanvas != null)
            {
                screenCanvas.SetActive(false);
            }
        }

        public override void OnInteract(PlayerController player)
        {
            base.OnInteract(player);

            if (screenCanvas != null)
            {
                screenCanvas.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Light2Screen: Screen Canvas is not assigned!");
            }
        }

        /// <summary>
        /// Call this method from a Close Button on the Canvas to hide the screen UI.
        /// </summary>
        public void CloseScreen()
        {
            if (screenCanvas != null)
            {
                screenCanvas.SetActive(false);
            }
        }
    }
}
