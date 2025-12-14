using UnityEngine;
using Mirror;
using Events;

namespace Game.Gameplay.Puzzle.Paint2
{
    public class Paint2Manager : NetworkBehaviour
    {
        public static Paint2Manager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("The Clue ID of the compass in the Modern timeline")]
        public string modernCompassClueId = "ModernCompass";

        [SyncVar(hook = nameof(OnCompassSolvedChanged))]
        public bool isCompassSolved = false;

        [SyncVar(hook = nameof(OnClueFoundChanged))]
        public bool isClueFound = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ClueDiscoveredEvent>(OnClueDiscovered);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ClueDiscoveredEvent>(OnClueDiscovered);
        }

        private void OnClueDiscovered(ClueDiscoveredEvent evt)
        {
            if (evt.clueId == modernCompassClueId)
            {
                if (isServer)
                {
                    isClueFound = true;
                }
                else
                {
                    CmdSetClueFound(true);
                }
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdSetCompassSolved(bool value)
        {
            isCompassSolved = value;
        }

        [Command(requiresAuthority = false)]
        public void CmdSetClueFound(bool value)
        {
            isClueFound = value;
        }

        void OnCompassSolvedChanged(bool oldVal, bool newVal)
        {
            Debug.Log($"Paint2: Compass Solved changed to {newVal}");
        }

        void OnClueFoundChanged(bool oldVal, bool newVal)
        {
            Debug.Log($"Paint2: Clue Found changed to {newVal}");
        }
    }
}
