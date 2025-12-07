using UnityEngine;
using Mirror;

namespace Game.Gameplay.Puzzle.Paint2
{
    public class Paint2Manager : NetworkBehaviour
    {
        public static Paint2Manager Instance { get; private set; }

        [SyncVar(hook = nameof(OnCompassSolvedChanged))]
        public bool isCompassSolved = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        [Command(requiresAuthority = false)]
        public void CmdSetCompassSolved(bool value)
        {
            isCompassSolved = value;
        }

        void OnCompassSolvedChanged(bool oldVal, bool newVal)
        {
            Debug.Log($"Paint2: Compass Solved changed to {newVal}");
            // Here you might want to enable the scene objects for the fragments if they are managed by this manager
            // Or let the fragments themselves listen to this state or an event.
        }
    }
}
