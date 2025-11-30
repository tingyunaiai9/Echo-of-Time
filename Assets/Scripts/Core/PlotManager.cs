using UnityEngine;
using Mirror;
using System.Collections;
using TMPro;

public class PlotManager : NetworkBehaviour
{
    [SerializeField] private float plotDuration = 3.0f;
    [SerializeField] private TMP_Text countdownText;

    private void Start()
    {
        if (isServer)
        {
            StartCoroutine(PlotCountdown());
        }
    }

    [Server]
    private IEnumerator PlotCountdown()
    {
        float timeLeft = plotDuration;
        while (timeLeft > 0)
        {
            if (countdownText != null)
            {
                RpcUpdateCountdown(Mathf.CeilToInt(timeLeft).ToString());
            }
            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }

        // Time's up, go back to game
        if (EchoNetworkManager.singleton != null)
        {
            ((EchoNetworkManager)EchoNetworkManager.singleton).ServerFinishPlot();
        }
        else
        {
            Debug.LogError("EchoNetworkManager singleton not found!");
        }

        // Hide the plot panel
        RpcHidePlot();
    }

    [ClientRpc]
    private void RpcUpdateCountdown(string text)
    {
        if (countdownText != null)
        {
            countdownText.text = text;
        }
    }

    [ClientRpc]
    private void RpcHidePlot()
    {
        gameObject.SetActive(false);
    }
}
