using UnityEngine;

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

    bool isUnlocked = false;

    public void OnWheelChanged()
    {
        // 先打印当前组合
        string combo = "";
        for (int i = 0; i < wheels.Length; i++)
        {
            combo += wheels[i].GetCurrentIndex();
        }
        Debug.Log("Current combo: " + combo);

        if (isUnlocked) return;

        if (IsCorrect())
        {
            Debug.Log("Unlocked! combo = " + combo);
            isUnlocked = true;

            StartCoroutine(PlayUnlockAnimation());
        }
        else
        {
            Debug.Log("Not correct yet.");
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
                Debug.Log($"Wheel {i} mismatch: current={cur}, target={target}");
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
    }
}
