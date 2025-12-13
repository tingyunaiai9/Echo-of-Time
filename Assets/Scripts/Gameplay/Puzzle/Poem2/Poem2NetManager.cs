using UnityEngine;
using Mirror;

namespace Game.Gameplay.Puzzle.Poem2
{
    public class Poem2NetManager : NetworkBehaviour
    {
        public static Poem2NetManager Instance { get; private set; }

        [SyncVar(hook = nameof(OnScrollPlacedChanged))]
        public bool isScrollPlacedInAncient = false;

        [SyncVar(hook = nameof(OnLockUnlockedChanged))]
        public bool isLockUnlockedInModern = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // Command to set scroll placed (called by Ancient player)
        [Command(requiresAuthority = false)]
        public void CmdSetScrollPlaced(bool value)
        {
            isScrollPlacedInAncient = value;
        }

        // Command to set lock unlocked (called by Modern player)
        [Command(requiresAuthority = false)]
        public void CmdSetLockUnlocked(bool value)
        {
            isLockUnlockedInModern = value;
        }

        // Hooks for state changes if needed (e.g., to update UI or objects immediately)
        void OnScrollPlacedChanged(bool oldVal, bool newVal)
        {
            // Optional: Trigger events or update objects locally
            Debug.Log($"Poem2: Scroll Placed changed to {newVal}");
        }

        void OnLockUnlockedChanged(bool oldVal, bool newVal)
        {
            // Optional: Trigger events
            Debug.Log($"Poem2: Lock Unlocked changed to {newVal}");
        }
    }
}
