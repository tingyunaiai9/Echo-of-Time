using UnityEngine;
using Events;
// using Unity.UOS.COSXML.Log;

/*
 * 诗词谜题一号管理器
 * 保留原 PoemManager 的完成动画逻辑：面板上移 2/3 高度并展示 DrawerPanel
 */
public class Poem1Manager : BasePoemManager
{
    [Header("面板配置")]
    [Tooltip("根对象（用于显示/隐藏）")]
    public GameObject panelRoot;

    [Tooltip("完成后的目标面板（DrawerPanel）")]
    public GameObject drawerPanel;

    [Header("动画配置")]
    [Tooltip("向上移动的动画时长（秒）")]
    public float animationDuration = 1f;

    [Tooltip("动画缓动类型")]
    public LeanTweenType easeType = LeanTweenType.easeInOutQuad;

    protected override GameObject PanelRoot => panelRoot;
    protected override GameObject DrawerPanel => drawerPanel;

    protected override void InitializePanels()
    {
        // 如果未指定根对象,使用当前 GameObject
        if (panelRoot == null)
            panelRoot = gameObject;

        // 确保 DrawerPanel 初始时关闭
        if (drawerPanel != null)
        {
            drawerPanel.SetActive(false);
        }
    }

    public override void OnPuzzleCompleted()
    {
        Debug.Log("[Poem1Manager] 谜题完成！开始播放上移动画");

        // 设置完成标志
        MarkPuzzleCompleted();

        EventBus.LocalPublish(new PuzzleCompletedEvent
        {
            sceneName = "Poem"
        });

        EventBus.LocalPublish(new LevelProgressEvent
        {
        });
        if (TimelinePlayer.Local != null)
        {
            Sprite sprite = Resources.Load<Sprite>("Clue_Poem1");
            int timeline = TimelinePlayer.Local.timeline;
            int level = TimelinePlayer.Local.currentLevel;
            // 压缩图片，避免过大
            byte[] spriteBytes = ImageUtils.CompressSpriteToJpegBytes(sprite, 80);
            Debug.Log($"[UIManager] 线索图片压缩成功，大小：{spriteBytes.Length} 字节");
            ClueBoard.AddClueEntry(timeline, level, spriteBytes);
        }

        RectTransform poemRect = panelRoot.GetComponent<RectTransform>();

        // 计算向上移动的距离（2/3的高度）
        float moveDistance = poemRect.rect.height * 2f / 3f;
        Vector2 targetPosition = poemRect.anchoredPosition + new Vector2(0, moveDistance);

        // 激活 DrawerPanel
        if (drawerPanel != null)
        {
            drawerPanel.SetActive(true);
        }

        // 使用 LeanTween 播放向上移动动画
        LeanTween.value(panelRoot, poemRect.anchoredPosition, targetPosition, animationDuration)
            .setOnUpdate((Vector2 val) =>
            {
                poemRect.anchoredPosition = val;
            })
            .setEase(easeType)
            .setOnComplete(() =>
            {
                Debug.Log("[Poem1Manager] 动画完成");
            });
    }
}
