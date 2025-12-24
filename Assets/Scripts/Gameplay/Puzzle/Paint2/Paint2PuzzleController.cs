using UnityEngine;
using System.Collections.Generic;
using Game.UI;
using Events;

namespace Game.Gameplay.Puzzle.Paint2
{
    public class Paint2PuzzleController : MonoBehaviour
    {
        [Header("容器")]
        public Transform maskContainer;
        public Transform pieceContainer;
        [Tooltip("Container for pieces that are initially missing")]
        public Transform missingPieceContainer;

        [Header("配置")]
        public string missingPieceItemId = "PaintFragment";
        public int requiredMissingPieceCount = 5;

        [Header("事件")]
        public UnityEngine.Events.UnityEvent onPuzzleComplete;

        [Header("Rewards")]
        public GameObject noteObject;
        [Tooltip("Name of the Note object to find in the scene if noteObject is not assigned")]
        public string noteObjectName = "Note";

        [Header("提示面板")]
        [Tooltip("指南面板")]
        public TipManager tipPanel;

        private Dictionary<int, PuzzleMask> masks = new Dictionary<int, PuzzleMask>();
        
        private int correctPieces = 0;
        private int totalPieces = 0;
        private int initialPieceCount = 0;
        
        private bool areMissingPiecesActive = false;

        private static bool s_tipShown = false;

        [Header("UI 引用")]
        [Tooltip("如果不填，会自动查找全局单例")]
        public NotificationController notificationController;

        void Awake()
        {
            if (s_tipShown && tipPanel != null)
            {
                tipPanel.gameObject.SetActive(false);
            }
            s_tipShown = true;
        }

        void Start()
        {
            // Try to find Note object by name if not assigned
            if (noteObject == null && !string.IsNullOrEmpty(noteObjectName))
            {
                noteObject = GameObject.Find(noteObjectName);
                if (noteObject == null)
                {
                    Debug.LogWarning($"[Paint2] Could not find Note object with name '{noteObjectName}' in loaded scenes.");
                }
            }

            if (notificationController == null)
            {
                // 优先查找当前场景中的 NotificationController
                var controllers = FindObjectsByType<NotificationController>(FindObjectsSortMode.None);
                foreach (var controller in controllers)
                {
                    if (controller.gameObject.scene == gameObject.scene)
                    {
                        notificationController = controller;
                        break;
                    }
                }

                // 如果当前场景没有，则使用全局单例
                if (notificationController == null)
                {
                    notificationController = NotificationController.Instance;
                }

                // 最后尝试任意查找
                if (notificationController == null) 
                {
                    notificationController = FindFirstObjectByType<NotificationController>();
                }
            }

            InitializeMasks();
            InitializePieces();
            
            // Check inventory and activate missing pieces if needed
            CheckMissingPieces();
        }

        void OnEnable()
        {
            CheckMissingPieces();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("[Paint2] P键按下，触发拼图完成效果");
                // Simulate completing all pieces
                while (correctPieces < totalPieces)
                {
                    OnPieceCorrect(correctPieces + 1);
                }
            }
        }

        void InitializeMasks()
        {
            PuzzleMask[] maskArray = maskContainer.GetComponentsInChildren<PuzzleMask>(true);
            foreach (PuzzleMask mask in maskArray)
            {
                string name = mask.gameObject.name;
                if (int.TryParse(name.Replace("PuzzleMask", ""), out int id))
                {
                    mask.maskId = id;
                    masks[id] = mask;
                }
            }
            totalPieces = masks.Count;
            Debug.Log($"[Paint2] Initialized {totalPieces} masks.");
        }

        void InitializePieces()
        {
            initialPieceCount = 0;

            // Initialize standard pieces
            if (pieceContainer != null)
            {
                SetupPiecesInContainer(pieceContainer, false);
            }

            // Initialize missing pieces
            if (missingPieceContainer != null)
            {
                SetupPiecesInContainer(missingPieceContainer, true);
            }
            
            Debug.Log($"[Paint2] Initialized pieces. Initial count: {initialPieceCount}.");
        }

        void SetupPiecesInContainer(Transform container, bool isMissingGroup)
        {
            PuzzlePiece[] pieceArray = container.GetComponentsInChildren<PuzzlePiece>(true);
            foreach (PuzzlePiece piece in pieceArray)
            {
                string name = piece.gameObject.name;
                if (int.TryParse(name.Replace("PuzzlePiece", ""), out int id))
                {
                    piece.pieceId = id;
                    if (masks.TryGetValue(id, out PuzzleMask mask))
                    {
                        piece.targetMask = mask;
                    }
                    
                    if (!isMissingGroup)
                    {
                        initialPieceCount++;
                    }
                    else
                    {
                        // Ensure they are hidden initially if not yet unlocked
                        if (!areMissingPiecesActive)
                        {
                            piece.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        void CheckMissingPieces()
        {
            if (areMissingPiecesActive) return;

            int currentCount = PropBackpack.GetPropCount(missingPieceItemId);
            Debug.Log($"[Paint2] Checking missing pieces. ItemID: '{missingPieceItemId}', CurrentCount: {currentCount}, Required: {requiredMissingPieceCount}");

            // Check if player has enough fragments
            if (currentCount >= requiredMissingPieceCount)
            {
                ActivateMissingPieces();
            }
        }

        void ActivateMissingPieces()
        {
            if (areMissingPiecesActive) return;
            
            areMissingPiecesActive = true;
            if (missingPieceContainer != null)
            {
                PuzzlePiece[] pieces = missingPieceContainer.GetComponentsInChildren<PuzzlePiece>(true);
                foreach (var p in pieces)
                {
                    p.gameObject.SetActive(true);
                }
            }
            Debug.Log("[Paint2] Missing pieces activated.");
        }

        public void OnPieceCorrect(int pieceId)
        {
            correctPieces++;
            Debug.Log($"[Paint2] Piece {pieceId} correct. Progress: {correctPieces}/{totalPieces}");

            // Check if we finished the INITIAL set but missing pieces are not yet active
            if (!areMissingPiecesActive)
            {
                if (correctPieces >= initialPieceCount)
                {
                    Debug.Log("[Paint2] Initial set complete. Trying to show notification...");
                    // Show notification
                    if (notificationController != null)
                    {
                        notificationController.ShowNotification("所余碎片，待罗盘所指\nRemaining fragments await the compass's direction");
                        Debug.Log("[Paint2] Notification '所余碎片...' triggered.");
                    }
                    else
                    {
                        Debug.LogError("[Paint2] NotificationController is missing! Cannot show notification.");
                    }
                }
            }
            else
            {
                // Missing pieces are active, check full completion
                if (correctPieces >= totalPieces)
                {
                    if (noteObject != null)
                    {
                        noteObject.SetActive(true);
                        // Also activate all children to ensure visibility if the user used the "Active Root, Inactive Child" setup
                        foreach (Transform child in noteObject.transform)
                        {
                            child.gameObject.SetActive(true);
                        }
                    }

                    onPuzzleComplete?.Invoke();
                    EventBus.LocalPublish(new PuzzleCompletedEvent
                    {
                        sceneName = "Paint2"
                    });
                }
            }
        }
    }
}             