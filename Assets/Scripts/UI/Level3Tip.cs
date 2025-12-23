using Events;
using UnityEngine;
using UnityEngine.UI;

public class Level3Tip : MonoBehaviour
{
    public TipManager tipManager; // 引用TipManager脚本

    void Start()
    {
        if (tipManager == null) Debug.LogError("Level3Tip: TipManager reference is not set.");
        EventBus.Subscribe<IntroEndEvent>(OnIntroEnd);
    }

    private void OnIntroEnd(IntroEndEvent evt)
    {
        int level = TimelinePlayer.Local.currentLevel;
        if (level == 3)
        {
            // 启用提示面板
            tipManager.gameObject.SetActive(true);
            
            // 查找所有场景中的 UIManager 并调用 SetFrozen(true)
            UIManager uiManager = FindFirstObjectByType<UIManager>();
            uiManager.SetFrozen(true);
            Debug.Log("[Level3Tip] 已调用 UIManager.SetFrozen(true)");
        }
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<IntroEndEvent>(OnIntroEnd);
    }
}