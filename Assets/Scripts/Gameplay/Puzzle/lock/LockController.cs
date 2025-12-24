using UnityEngine;
using Game.UI;
using Game.Gameplay.Puzzle.Poem2;
using Events;
using UnityEngine.UI;

public class LockController : MonoBehaviour
{
    public DialWheel[] wheels;
    public int[] correctCode;

    [Header("Unlock Animation Settings")]
    public Transform[] bodyParts;
    public Transform[] wheelParts;

    public Vector3 bodyMoveOffset = new Vector3(0f, 1.5f, 0f);
    public Vector3 wheelMoveOffset = new Vector3(-2f, 0f, 0f);
    public float animDuration = 1.0f;

    [Header("Poem2 Logic")]
    public GameObject scrollInsideBox;
    public NotificationController notificationController;
    public TipManager tipPanel;
    public GameObject clueButton;

    bool isUnlocked = false;
    public static bool s_tipShown = false;

    void Start()
    {
        if (notificationController == null)
        {
            notificationController = NotificationController.Instance;
            if (notificationController == null)
            {
                notificationController = FindFirstObjectByType<NotificationController>();
            }
        }
        if (s_tipShown == true)
        {
            tipPanel.gameObject.SetActive(false);
        }
        s_tipShown = true;
    }

    void OnEnable()
    {
        if (UIManager.Instance.LockClueUnlocked == true) 
        {
            clueButton.SetActive(true);
        }
    }

    public void OnWheelChanged()
    {
        // 组合变化时检查是否解锁

        if (isUnlocked) return;

        if (IsCorrect())
        {
            isUnlocked = true;

            // Notify Manager
            if (Poem2NetManager.Instance != null)
            {
                Poem2NetManager.Instance.CmdSetLockUnlocked(true);
            }
            EventBus.LocalPublish(new PuzzleCompletedEvent
            {
                sceneName = "Lock"
            });

            StartCoroutine(PlayUnlockAnimation());
        }
    }

    bool IsCorrect()
    {
        if (wheels.Length != correctCode.Length)
        {
            Debug.LogError("wheels.Length != correctCode.Length");
            return false;
        }

        for (int i = 0; i < wheels.Length; i++)
        {
            int cur = wheels[i].GetCurrentIndex();
            int target = correctCode[i];
            if (cur != target)
            {
                return false;
            }
        }
        return true;
    }

    System.Collections.IEnumerator PlayUnlockAnimation()
    {
        Vector3[] bodyStartPos = new Vector3[bodyParts.Length];
        Vector3[] wheelStartPos = new Vector3[wheelParts.Length];

        for (int i = 0; i < bodyParts.Length; i++)
        {
            if (bodyParts[i] != null)
                bodyStartPos[i] = bodyParts[i].localPosition;
        }

        for (int i = 0; i < wheelParts.Length; i++)
        {
            if (wheelParts[i] != null)
                wheelStartPos[i] = wheelParts[i].localPosition;
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / animDuration;
            float k = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < bodyParts.Length; i++)
            {
                if (bodyParts[i] == null) continue;
                bodyParts[i].localPosition =
                    Vector3.Lerp(bodyStartPos[i], bodyStartPos[i] + bodyMoveOffset, k);
            }

            for (int i = 0; i < wheelParts.Length; i++)
            {
                if (wheelParts[i] == null) continue;
                wheelParts[i].localPosition =
                    Vector3.Lerp(wheelStartPos[i], wheelStartPos[i] + wheelMoveOffset, k);
            }

            yield return null;
        }

        for (int i = 0; i < bodyParts.Length; i++)
        {
            if (bodyParts[i] == null) continue;
            bodyParts[i].localPosition = bodyStartPos[i] + bodyMoveOffset;
        }

        for (int i = 0; i < wheelParts.Length; i++)
        {
            if (wheelParts[i] == null) continue;
            wheelParts[i].localPosition = wheelStartPos[i] + wheelMoveOffset;
        }

        // Check Scroll State after opening
        if (Poem2NetManager.Instance != null)
        {
            bool hasScroll = Poem2NetManager.Instance.isScrollPlacedInAncient;
            
            if (scrollInsideBox != null)
            {
                scrollInsideBox.SetActive(hasScroll);
            }

            if (!hasScroll)
            {
                if (notificationController != null)
                {
                    notificationController.ShowNotification("需要古代玩家将竹简放入匣子中。\n");
                }
            }
            else
            {
                if (notificationController != null)
                {
                    notificationController.ShowNotification("现在，未来玩家可以拿到竹简了。\n");
                }
            }
        }

        // Wait a bit before auto-exiting
        yield return new WaitForSeconds(1.5f);

        // Auto exit puzzle
        if (PuzzleOverlayManager.Instance != null)
        {
            PuzzleOverlayManager.Instance.ClosePuzzle();
        }
    }
}
