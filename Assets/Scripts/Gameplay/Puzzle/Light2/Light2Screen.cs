using UnityEngine;
using Game.UI;

namespace Game.Gameplay.Puzzle.Light2
{
    public class Light2Screen : Interaction
    {
        [Header("UI Settings")]
        [Tooltip("The name of the Canvas object in GameBase scene")]
        public string screenCanvasName = "ScreenCanvas";

        private GameObject screenCanvas;

        protected override void Start()
        {
            base.Start();
            
            FindCanvas();

            // Ensure canvas is hidden at start
            if (screenCanvas != null)
            {
                screenCanvas.SetActive(false);
            }
        }

        private void FindCanvas()
        {
            if (screenCanvas != null) return;

            // 1. Try finding active object
            screenCanvas = GameObject.Find(screenCanvasName);

            // 2. If not found, try finding inactive object (slower but necessary if it starts hidden)
            if (screenCanvas == null)
            {
                Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                foreach (Canvas c in canvases)
                {
                    // Check if it's a scene object (not an asset) and matches name
                    if (c.gameObject.scene.IsValid() && c.name == screenCanvasName)
                    {
                        screenCanvas = c.gameObject;
                        break;
                    }
                }
            }
        }

        public override void OnInteract(PlayerController player)
        {
            base.OnInteract(player);

            if (screenCanvas == null) FindCanvas();

            if (screenCanvas != null)
            {
                screenCanvas.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"Light2Screen: Screen Canvas '{screenCanvasName}' not found in any scene!");
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
