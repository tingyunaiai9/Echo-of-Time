using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Events;
using Unity.Sync.Relay.Message;
using Game.UI;
// using Unity.UOS.COSXML.Log;
using System.Collections.Generic;
using Unity.VisualScripting; // 引入事件命名空间

// UI管理器，协调所有UI系统的显示和交互
public class UIManager : Singleton<UIManager>
{
    [Header("UI面板")]
    [Tooltip("日记界面游戏对象")]
    public GameObject DiaryPanel;
    [Tooltip("背包界面游戏对象")]
    public GameObject InventoryPanel;
    [Tooltip("指南界面游戏对象")]
    public GameObject TipPanel;
    [Tooltip("通知面板游戏对象")]
    public NotificationController notificationController;
    [Tooltip("主 UI 画布（包含日记按钮等常驻 UI），用于在谜题中隐藏")]
    public Canvas mainCanvas;
    [Header("HUD按钮")]
    [Tooltip("日记按钮游戏对象")]
    public GameObject DiaryButton;
    [Tooltip("背包按钮游戏对象")]
    public GameObject InventoryButton;
    [Tooltip("指南按钮游戏对象")]
    public GameObject TipButton;

    [Header("状态标记")]
    [Tooltip("当前是否有UI面板打开")]
    public bool UIFrozen = false;
    [Tooltip("Prune谜题提示是否解锁")]
    public bool PruneClueUnlocked = false;
    [Tooltip("Lock谜题提示是否解锁")]
    public bool LockClueUnlocked = false;
    [Tooltip("三时间线三层当中的探索进度矩阵")]
    public List<List<int>> TimelineLevelProgress = new List<List<int>>()
    {
        new List<int>() {1, 2, 3}, // 古代时间线
        new List<int>() {1, 4, 3}, // 民国时间线
        new List<int>() {1, 5, 3}  // 未来时间线
    };
    public static int s_levelProgressCount = 0;

    protected override void Awake()
    {
        base.Awake();
        // 尝试自动获取 Canvas
        if (mainCanvas == null)
        {
            mainCanvas = GetComponent<Canvas>();
            if (mainCanvas == null)
            {
                // 尝试在父级查找
                mainCanvas = GetComponentInParent<Canvas>();
            }
        }
        EventBus.Subscribe<IntroEndEvent>(OnIntroEnd);
        EventBus.Subscribe<ClueSharedEvent>(OnClueShared);
        EventBus.Subscribe<LevelProgressEvent>(OnLevelProgress);
    }

    public void OnIntroEnd(IntroEndEvent evt)
    {
        int level = TimelinePlayer.Local.currentLevel;
        // 根据当前关卡决定是否显示提示面板
        if (level == 1)
        {
            if (TipPanel != null)
            {
                TipPanel.SetActive(true);
                RefreshFrozenState();
            }
        }
    }

    public void OnClueShared(ClueSharedEvent evt)
    {
        // 如果是 Prune 谜题的线索，解锁提示
        if (!string.IsNullOrEmpty(evt.text)) // 未来第二层的文字线索
        {
            PruneClueUnlocked = true;
            Debug.Log("[UIManager] Prune 谜题线索已解锁，PruneClueUnlocked 设置为 true。");
        }
        if (evt.clueId == 2) // 罗盘线索
        {
            LockClueUnlocked = true;
            Debug.Log("[UIManager] Lock 谜题线索已解锁，LockClueUnlocked 设置为 true。");
        }
    }

    public void OnLevelProgress(LevelProgressEvent evt)
    {
        s_levelProgressCount += 1;
        Debug.Log("[UIManager] 收到 LevelProgressEvent，当前探索进度: " + s_levelProgressCount);
        int timeline = TimelinePlayer.Local.timeline;
        int level = TimelinePlayer.Local.currentLevel;
        if (s_levelProgressCount >= TimelineLevelProgress[timeline][level - 1])
        {
            Debug.Log("[UIManager] 当前时间线和层数的所有线索已发现，等待时间线场景置顶后显示通知。");
            string targetScene = SceneDirector.Instance.GetSceneName(timeline, level);
            StartCoroutine(WaitForTimelineSceneAndNotify(targetScene));
        }
    }

    private IEnumerator WaitForTimelineSceneAndNotify(string targetScene)
    {
        // 如果无法获取目标场景名称，则保持原有行为立即通知
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[UIManager] 目标场景名称为空，无法等待时间线场景置顶");
            yield break;
        }
        if (SceneDirector.Instance.TryGetTopNonDontDestroyScene(out var topScene) && topScene.IsValid() && topScene.name == targetScene)
        {
            // 已经在顶层，通知面板空闲后等待2s直接显示通知
            while (notificationController.IsShowing) yield return null;
            yield return new WaitForSeconds(2f);
            notificationController?.ShowNotification("所有线索已发现，请使用日记和其他玩家进行沟通！");
            yield break;
        }

        // 等待直到最上层（排除 DontDestroyOnLoad）的场景为时间线场景
        while (true)
        {
            if (SceneDirector.Instance.TryGetTopNonDontDestroyScene(out var currentTopScene)
                && currentTopScene.IsValid()
                && currentTopScene.name == targetScene)
            {
                break;
            }
            yield return null;
        }

        notificationController?.ShowNotification("所有线索已发现，请使用日记和其他玩家进行沟通！");
    }

    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<ClueSharedEvent>(OnClueShared);
        EventBus.Unsubscribe<IntroEndEvent>(OnIntroEnd);
        EventBus.Unsubscribe<LevelProgressEvent>(OnLevelProgress);
        base.OnDestroy();
    }

    // 每帧更新
    void Update()
    {
        HandleUIInput();
        TestUI();
    }

    // 统一设置冻结状态，替代 FreezeEvent 发布
    public void SetFrozen(bool isOpen)
    {
        if (UIFrozen == isOpen) return;
        UIFrozen = isOpen;
        
        // 根据冻结状态启用/禁用 HUD 按钮
        SetHUDButtonsInteractable(!isOpen);
        
        Debug.Log($"[UIManager] UIFrozen -> {UIFrozen}");
    }

    // 设置 HUD 按钮的可交互状态
    private void SetHUDButtonsInteractable(bool interactable)
    {
        if (DiaryButton != null)
        {
            var button = DiaryButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null) button.interactable = interactable;
        }
        
        if (InventoryButton != null)
        {
            var button = InventoryButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null) button.interactable = interactable;
        }
        
        if (TipButton != null)
        {
            var button = TipButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null) button.interactable = interactable;
        }
    }

    // 根据当前面板状态刷新冻结标记
    private void RefreshFrozenState()
    {
        bool anyOpen = (DiaryPanel != null && DiaryPanel.activeSelf)
                        || (InventoryPanel != null && InventoryPanel.activeSelf)
                        || (TipPanel != null && TipPanel.activeSelf);
        SetFrozen(anyOpen);
    }

    /// 设置主 UI（如日记按钮）的可见性/交互性
    public void SetMainUIActive(bool active)
    {
        if (mainCanvas != null)
        {
            mainCanvas.enabled = active;
            Debug.Log($"[UIManager] SetMainUIActive: {active}");
        }
        else
        {
            // 如果没有 Canvas，尝试禁用 DiaryPanel 的父级或者其他处理
            // 这里做一个简单的 fallback，如果 DiaryPanel 存在，禁用它的父物体（假设是 Canvas）
            if (DiaryPanel != null && DiaryPanel.transform.parent != null)
            {
                var parentCanvas = DiaryPanel.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    parentCanvas.enabled = active;
                    Debug.Log($"[UIManager] SetMainUIActive (via DiaryPanel parent): {active}");
                }
            }
        }
    }

    /// 关闭日记面板
    public void CloseDiary()
    {
        if (DiaryPanel != null && DiaryPanel.activeSelf)
        {
            DiaryPanel.SetActive(false);
            RefreshFrozenState();
            Debug.Log("[UIManager] CloseDiary called.");
        }
    }

    // 处理所有 UI 相关的按键
    private void HandleUIInput()
    {
        if (UIFrozen)
        {
            // 如果已经冻结，则不处理打开操作
            return;
        }
        // 背包开关 (B键)
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (InventoryPanel != null)
            {
                bool isActive = InventoryPanel.activeSelf;
                if (!isActive)
                {
                    InventoryPanel.SetActive(true);
                    Inventory.SwitchToProps();
                }
                else
                {
                    InventoryPanel.SetActive(false);
                }
            }
            RefreshFrozenState();
            Debug.Log("[UIManager] B键按下，切换背包。");
        }

        // 日记页面切换 (F1键)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (DiaryPanel != null)
            {
                bool isActive = DiaryPanel.activeSelf;
                DiaryPanel.SetActive(!isActive);
            }
            RefreshFrozenState();
            Debug.Log("[UIManager] F1键按下，切换日记页面。");
        }

        // 指南开关（H键）
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (TipPanel!= null)
            {
                bool isActive = TipPanel.activeSelf;
                TipPanel.SetActive(!isActive);
            }
            RefreshFrozenState();
            Debug.Log("[UIManager] H键按下，切换指南页面。");
        }
    }

    private void TestUI()
    {
        // 添加测试聊天消息 (Equals键)
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadEquals))
        {
            // 获取当前玩家的时间线
            int timeline = TimelinePlayer.Local.timeline;
            DialogPanel.AddChatMessage(
                "两只黄鹂鸣翠柳，一行白鹭上青天。",
                timeline);
            Debug.Log("[UIManager] Equals键按下，添加测试聊天消息。");
        }

        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            Sprite tymSprite = Resources.Load<Sprite>("tym");
            if (tymSprite != null)
            {
                // 获取当前玩家的时间线
                int timeline = TimelinePlayer.Local.timeline;
                // 压缩图片，避免过大
                byte[] spriteBytes = ImageUtils.CompressSpriteToJpegBytes(tymSprite, 60); // 可调整quality
                if (spriteBytes != null)
                {
                    DialogPanel.AddChatImage(spriteBytes, timeline);
                    Debug.Log("[UIManager] `键按下，添加压缩后的图片消息。");
                    Debug.Log($"[UIManager] 图片大小：{spriteBytes.Length} 字节");
                }
                else
                {
                    Debug.LogError("[UIManager] 图片压缩失败。");
                }
            }
            else
            {
                Debug.LogError("[UIManager] 无法加载 Sprite 文件 'tym'，请检查路径和文件名是否正确。");
            }
        }
        // 添加测试线索条目 (Minus键)
        if (Input.GetKeyDown(KeyCode.Minus) && Input.GetKey(KeyCode.LeftShift))
        {
            Debug.Log("[UIManager] Shift + Minus键按下，添加测试文字共享线索条目。");
            int timeline = TimelinePlayer.Local.timeline;
            int level = TimelinePlayer.Local.currentLevel;
            string clueText = "未斫之木，天干有七：东园之树，枝条载荣；生于子半，丑时初萌；桃蕊未绽，茂然至辰。飞影又逝，晌午归土。";
            ClueBoard.AddClueEntry(timeline, level, clueText);
        }
        else if (Input.GetKeyDown(KeyCode.Minus))
        {
            Debug.Log("[UIManager] Minus键按下，添加测试图片共享线索条目。");
            
            Sprite sprite = Resources.Load<Sprite>("Clue_Poem1");
            int timeline = TimelinePlayer.Local.timeline;
            int level = TimelinePlayer.Local.currentLevel;
            // 压缩图片，避免过大
            byte[] spriteBytes = ImageUtils.CompressSpriteToJpegBytes(sprite, 80);
            Debug.Log($"[UIManager] 线索图片压缩成功，大小：{spriteBytes.Length} 字节");
            ClueBoard.AddClueEntry(timeline, level, spriteBytes);
        }
    }
}
