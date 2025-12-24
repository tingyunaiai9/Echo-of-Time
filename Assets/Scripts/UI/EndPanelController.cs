using UnityEngine;
using UnityEngine.UI;
using System;

/*
 * 结束面板控制器
 * 管理两张图片的切换：点击第一张切到第二张，第二张停留指定时间后触发彩蛋
 */
public class EndPanelController : MonoBehaviour
{
    [Header("图片配置")]
    [Tooltip("第一张图片")]
    public GameObject firstImage;
    [Tooltip("第二张图片")]
    public GameObject secondImage;
    [Tooltip("第二张图片停留时间（秒）")]
    public float secondImageDelay = 5f;

    public event Action OnSecondImageTimeout; // 第二张图片停留完毕的回调

    private bool _isSecondImageShown = false;

    void OnEnable()
    {
        ShowFirstImage();
    }

    void ShowFirstImage()
    {
        if (firstImage != null) firstImage.SetActive(true);
        if (secondImage != null) secondImage.SetActive(false);
        _isSecondImageShown = false;

        // 为第一张图片添加点击事件
        var button = firstImage?.GetComponent<Button>();
        if (button == null && firstImage != null)
        {
            button = firstImage.AddComponent<Button>();
        }
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(ShowSecondImage);
        }
    }

    void ShowSecondImage()
    {
        if (_isSecondImageShown) return;
        _isSecondImageShown = true;

        if (firstImage != null) firstImage.SetActive(false);
        if (secondImage != null) secondImage.SetActive(true);

        // 第二张图片不可点击，停留指定时间后触发回调
        StartCoroutine(WaitAndTriggerEasterEgg());
    }

    System.Collections.IEnumerator WaitAndTriggerEasterEgg()
    {
        yield return new WaitForSeconds(secondImageDelay);
        OnSecondImageTimeout?.Invoke();
    }
}
