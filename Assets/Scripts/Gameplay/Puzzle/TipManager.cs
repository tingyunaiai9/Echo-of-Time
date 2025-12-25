using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TipManager : MonoBehaviour
{
    [SerializeField]
    [Header("提示图片与按钮")]
    public TextMeshProUGUI tipTitleText; // 提示面板标题文本
    public string tipTitle = "提示"; // 提示面板标题
    public List<Sprite> tipImages; // 存储提示图片的列表
    public List<string> tipWords; // 存储提示文字的列表
    public Image currentImage; // 当前显示的提示图片
    public TextMeshProUGUI currentText; // 当前显示的提示文字
    public Button previousButton; // 上一张提示图片按钮
    public Button nextButton; // 下一张提示图片按钮

    private int currentTipIndex = 0; // 当前显示的提示图片索引

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        tipTitleText.text = tipTitle;
        currentImage.sprite = tipImages[0];
        currentText.text = tipWords[0];
        UpdateButtonVisibility();
    }

    public void ShowPreviousTip()
    {
        if (tipImages == null || tipImages.Count == 0)
        {
            Debug.LogWarning("TipManager: No tip images assigned.");
            return;
        }

        // 计算上一个提示图片的索引
        currentTipIndex = (currentTipIndex - 1 + tipImages.Count) % tipImages.Count;
        // 替换为当前提示图片
        currentImage.sprite = tipImages[currentTipIndex];
        currentText.text = tipWords[currentTipIndex];
        UpdateButtonVisibility();
    }

    public void ShowNextTip()
    {
        if (tipImages == null || tipImages.Count == 0)
        {
            Debug.LogWarning("TipManager: No tip images assigned.");
            return;
        }

        // 计算下一个提示图片的索引
        currentTipIndex = (currentTipIndex + 1) % tipImages.Count;
        // 替换为当前提示图片
        currentImage.sprite = tipImages[currentTipIndex];
        currentText.text = tipWords[currentTipIndex];
        UpdateButtonVisibility();
    }

    private void UpdateButtonVisibility()
    {
        // 如果是第一张图片，隐藏上一页按钮
        previousButton.gameObject.SetActive(currentTipIndex > 0);
        // 如果是最后一张图片，隐藏下一页按钮
        nextButton.gameObject.SetActive(currentTipIndex < tipImages.Count - 1);
    }

    public void CloseTipPanel()
    {
        currentTipIndex = 0;
        previousButton.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(true);
        this.gameObject.SetActive(false);
    }
}
