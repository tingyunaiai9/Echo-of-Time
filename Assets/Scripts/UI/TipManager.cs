using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TipManager : MonoBehaviour
{
    [SerializeField]
    [Header("提示图片与按钮")]
    public List<Sprite> tipImages; // 存储提示图片的列表
    public List<string> tipWords; // 存储提示文字的列表
    public Image currentImage; // 当前显示的提示图片
    public TextMeshProUGUI currentText; // 当前显示的提示文字

    private int currentTipIndex = 0; // 当前显示的提示图片索引

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentImage.sprite = tipImages[0];
        currentText.text = tipWords[0];
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
    }
}
