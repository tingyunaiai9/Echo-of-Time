using UnityEngine;
using UnityEngine.UI;
using Events;

/*
 * 诗词谜题二号管理器
 * 完成时让奖励面板淡入出现
 */
public class Poem2Manager : BasePoemManager
{
    [Header("面板配置")]
    [Tooltip("根对象（用于显示/隐藏）")]
    public GameObject panelRoot;

    [Tooltip("完成后显示的控制台面板")]
    public GameObject consolePanel;

    protected override GameObject PanelRoot => panelRoot;
    protected override GameObject DrawerPanel => consolePanel;

    protected override void InitializePanels()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (consolePanel != null)
        {
            // 确保控制台面板初始时隐藏且透明
            consolePanel.SetActive(false);
        }
    }

    public override void OnPuzzleCompleted()
    {

        // 获取 ConsolePanel 下的 ConsoleImage
        Transform consolePanelTransform = s_instance.transform.parent.Find("ConsolePanel");
        Image consoleImage = consolePanelTransform.Find("ConsoleImage")?.GetComponent<Image>();
        Sprite icon = consoleImage.sprite;

        EventBus.LocalPublish(new PuzzleCompletedEvent
        {
            sceneName = "Poem2"
        });
        EventBus.LocalPublish(new LevelProgressEvent
        {
        });

        // 发布 ClueDiscoveredEvent 事件
        EventBus.LocalPublish(new ClueDiscoveredEvent
        {
            isKeyClue = true,
            playerNetId = 0,
            clueId = "console_clue",
            clueText = "拼好5首诗句后抽屉中的一幅画。",
            clueDescription = "这幅画可能隐藏着重要的线索。",
            icon = icon,
            image = icon // 假设 image 和 icon 是相同的
        });

        if (TimelinePlayer.Local != null)
        {
            Sprite sprite = Resources.Load<Sprite>("Clue_Poem2");
            int timeline = TimelinePlayer.Local.timeline;
            int level = TimelinePlayer.Local.currentLevel;
            // 压缩图片，避免过大
            byte[] spriteBytes = ImageUtils.CompressSpriteToJpegBytes(sprite, 80);
            Debug.Log($"[UIManager] 线索图片压缩成功，大小：{spriteBytes.Length} 字节");
            ClueBoard.AddClueEntry(timeline, level,spriteBytes);
        }
        
        // 打开控制台面板
        ConsolePanel.TogglePanel();
    }
}
