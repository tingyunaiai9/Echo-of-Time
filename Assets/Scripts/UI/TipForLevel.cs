using Events;
using UnityEngine;
using UnityEngine.UI;

public class TipForLevel : MonoBehaviour
{
    [Header("关卡编号")]
    public int levelNumber; // 关卡编号
    [Header("提示面板")]
    public TipManager tipManager; // 引用TipManager脚本
    public UIManager uiManager; // 引用UIManager脚本
    public Button closeButton; // 关闭按钮引用
    public bool tipShown = false; // 提示是否已显示

    void Start()
    {
        if (tipManager == null) Debug.LogError("Level3Tip: TipManager reference is not set.");
        EventBus.Subscribe<IntroEndEvent>(OnIntroEnd);
        uiManager = FindFirstObjectByType<UIManager>();
        closeButton.onClick.AddListener(CloseTipPanel);
    }

    private void OnIntroEnd(IntroEndEvent evt)
    {
        int level = TimelinePlayer.Local.currentLevel;
        if (level == levelNumber && !tipShown)
        {
            // 启用提示面板
            tipManager.gameObject.SetActive(true);
            uiManager.SetFrozen(true);
            Debug.Log("[Level3Tip] 已调用 UIManager.SetFrozen(true)");
            tipShown = true;
        }
    }
    public void CloseTipPanel()
    {
        uiManager.SetFrozen(false);
        Debug.Log("[Level3Tip] 已调用 UIManager.SetFrozen(false)");
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<IntroEndEvent>(OnIntroEnd);
    }
}