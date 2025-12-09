using UnityEngine;
using TMPro;
using System.Collections;

public class WarningPanelManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TMP_Text warningText;
    
    [Header("Settings")]
    [SerializeField] private float displayDuration = 4f;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (warningPanel != null) 
            warningPanel.SetActive(false);
    }

    public void ShowWarning(string message)
    {
        if (warningPanel == null)
        {
            Debug.LogWarning("[WarningPanelManager] Warning Panel is not assigned!");
            return;
        }

        if (warningText != null)
        {
            warningText.text = message;
        }

        warningPanel.SetActive(true);

        // 如果之前有正在进行的倒计时，先停止，重置时间
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        warningPanel.SetActive(false);
        hideCoroutine = null;
    }
}
