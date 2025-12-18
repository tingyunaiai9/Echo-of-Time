using UnityEngine;
using Mirror;

namespace Game.Gameplay.Puzzle.Light2
{
    public class Light2Manager : NetworkBehaviour
    {
        public static Light2Manager Instance { get; private set; }

        [SyncVar(hook = nameof(OnSeedPlantedChanged))]
        public bool isSeedPlanted = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        [Command(requiresAuthority = false)]
        public void CmdPlantSeed()
        {
            isSeedPlanted = true;
        }

        void OnSeedPlantedChanged(bool oldVal, bool newVal)
        {
            Debug.Log($"Light2: Seed Planted changed to {newVal}");
        }
    }
}
