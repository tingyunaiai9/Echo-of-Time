using UnityEngine;
using System.Collections.Generic;

namespace Game.Gameplay.Puzzle.Paint2
{
    public class Paint2SceneFragments : MonoBehaviour
    {
        [Tooltip("List of fragment objects in the scene to enable when compass is solved")]
        public List<GameObject> fragments;

        [Header("Debug")]
        public bool testMode = false;

        private bool activated = false;

        void Start()
        {
            if (testMode)
            {
                activated = true;
                foreach (var f in fragments)
                {
                    if (f != null) f.SetActive(true);
                }
                return;
            }

            // Ensure they are hidden initially
            foreach (var f in fragments)
            {
                if (f != null) f.SetActive(false);
            }
        }

        void Update()
        {
            if (!activated && Paint2Manager.Instance != null)
            {
                // Fragments become visible only when BOTH the compass puzzle is solved AND the clue is found
                if (Paint2Manager.Instance.isCompassSolved && Paint2Manager.Instance.isClueFound)
                {
                    activated = true;
                    foreach (var f in fragments)
                    {
                        if (f != null) f.SetActive(true);
                    }
                    Debug.Log("[Paint2SceneFragments] Fragments activated (Compass Solved + Clue Found).");
                }
            }
        }
    }
}
