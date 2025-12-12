using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Events;

public class ResultPanel : MonoBehaviour
{
    [Tooltip("结果输入组件")]
    public TMP_InputField resultContent;

    [Tooltip("确认按钮")]
    public Button confirmButton;

    [Header("正确答案配置")]
    [Tooltip("需要匹配的正确答案")]
    public string correctAnswer = "";
    [Tooltip("按层数配置的正确答案列表，第1层索引0，第2层索引1，以此类推")]
    public List<string> levelAnswers = new List<string>() { "南山", "归去" }; // 初始前两层答案

    private TMP_Text confirmButtonText;
    private bool isConfirmButtonCooldown;

    private static ResultPanel s_instance;

    void Awake()
    {
        s_instance = this;

        if (resultContent == null)
        {
            Transform inputFieldTransform = transform.Find("RightPanel/ResultPanel/InputField");
            if (inputFieldTransform != null)
            {
                resultContent = inputFieldTransform.GetComponent<TMP_InputField>();
            }
        }
        if (resultContent == null)
        {
            Debug.LogWarning("[ResultPanel] 未找到 Result 输入框");
        }

        if (confirmButton == null)
        {
            Transform confirmButtonTransform = transform.Find("RightPanel/ResultPanel/ConfirmButton");
            if (confirmButtonTransform != null)
            {
                confirmButton = confirmButtonTransform.GetComponent<Button>();
            }
        }
        if (confirmButton == null)
        {
            Debug.LogWarning("[ResultPanel] 未找到 Confirm 按钮");
        }
        else
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            confirmButtonText = confirmButton.GetComponentInChildren<TMP_Text>();
        }
    }

    void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }

        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    private void OnConfirmButtonClicked()
    {
        Debug.Log("[ResultPanel] 确认按钮被点击");

        if (isConfirmButtonCooldown)
        {
            Debug.Log("[ResultPanel] 按钮冷却中，忽略点击");
            return;
        }

        if (resultContent == null)
        {
            Debug.LogWarning("[ResultPanel] resultContent 未设置");
            return;
        }

        string userAnswer = resultContent.text.Trim();

        var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<TimelinePlayer>();
        int currentLevel = localPlayer != null ? localPlayer.currentLevel : 1;
        string expectedAnswer = GetCorrectAnswerForLevel(currentLevel);
        Debug.Log($"[ResultPanel] 当前层数: {currentLevel}, 期望答案: '{expectedAnswer}', 玩家输入: '{userAnswer}'");

        if (string.Equals(userAnswer, expectedAnswer, StringComparison.Ordinal))
        {
            Debug.Log("[ResultPanel] 答案正确！");

            if (confirmButton != null)
            {
                AddButtonOverlay(confirmButton, new Color(0f, 1f, 0f, 0.5f));
                confirmButton.interactable = false;
            }

            resultContent.text = "答案正确！";
            resultContent.interactable = false;

            if (localPlayer != null)
            {
                localPlayer.CmdReportedCorrectAnswer();
                Debug.Log("[ResultPanel] 已调用 CmdReportedCorrectAnswer() 上报服务器");
            }
            else
            {
                Debug.LogWarning("[ResultPanel] 未找到 TimelinePlayer，无法上报答案正确");
            }
        }
        else
        {
            Debug.Log($"[ResultPanel] 答案错误。输入: '{userAnswer}', 正确答案应为: '{expectedAnswer}'");
            resultContent.text = "答案错误！";
            resultContent.interactable = false;
            StartCoroutine(ErrorCooldownCoroutine());
        }
    }

    private IEnumerator ErrorCooldownCoroutine()
    {
        isConfirmButtonCooldown = true;

        if (confirmButton != null)
        {
            AddButtonOverlay(confirmButton, new Color(1f, 0f, 0f, 0.5f));
        }

        yield return new WaitForSeconds(1f);

        if (confirmButton != null)
        {
            RemoveButtonOverlay(confirmButton);
            confirmButton.interactable = true;
        }

        if (resultContent != null)
        {
            resultContent.text = string.Empty;
            resultContent.interactable = true;
        }

        isConfirmButtonCooldown = false;
        Debug.Log("[ResultPanel] 按钮遮罩已移除");
    }

    private void AddButtonOverlay(Button button, Color overlayColor)
    {
        if (button == null) return;

        Transform overlayTransform = button.transform.Find("Overlay");
        if (overlayTransform != null) return;

        GameObject overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(button.transform, false);

        RectTransform rectTransform = overlay.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = overlayColor;
        overlayImage.raycastTarget = false;
    }

    private void RemoveButtonOverlay(Button button)
    {
        if (button == null) return;

        Transform overlayTransform = button.transform.Find("Overlay");
        if (overlayTransform != null)
        {
            Destroy(overlayTransform.gameObject);
        }
    }

    private string GetCorrectAnswerForLevel(int level)
    {
        if (level <= 0)
        {
            return correctAnswer;
        }

        int index = level - 1;
        if (index < levelAnswers.Count)
        {
            string configuredAnswer = levelAnswers[index];
            if (!string.IsNullOrWhiteSpace(configuredAnswer))
            {
                return configuredAnswer.Trim();
            }
        }

        if (levelAnswers.Count > 0)
        {
            string fallbackAnswer = levelAnswers[levelAnswers.Count - 1];
            if (!string.IsNullOrWhiteSpace(fallbackAnswer))
            {
                return fallbackAnswer.Trim();
            }
        }

        return correctAnswer;
    }

    public static void Reset()
    {
        if (s_instance == null)
        {
            return;
        }

        if (s_instance.confirmButtonText != null)
        {
            s_instance.confirmButtonText.text = "提交";
            s_instance.confirmButtonText.color = Color.black;
        }

        if (s_instance.confirmButton != null)
        {
            s_instance.confirmButton.interactable = true;
            s_instance.RemoveButtonOverlay(s_instance.confirmButton);
        }

        if (s_instance.resultContent != null)
        {
            s_instance.resultContent.text = string.Empty;
            s_instance.resultContent.interactable = true;
        }

        s_instance.isConfirmButtonCooldown = false;
        Debug.Log("[ResultPanel] 已因层数提升重置提交按钮为 '提交'");
    }
}
