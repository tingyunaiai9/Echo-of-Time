using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Events;

/* 镜槽（MirrorSlot)结构体
 * 用于存储镜槽信息
 */
[System.Serializable]
public struct MirrorSlot
{
    public int xindex;
    public int yindex;
    public int number;
    public enum Direction
    {
        TOP_LEFT,
        BOTTOM_LEFT,
        BOTTOM_RIGHT,
        TOP_RIGHT,
    }

    public Direction direction; // Add this field to store the direction value
}

/*
 * 光线谜题管理器
 * 管理游戏进度和完成检测,支持静态方法控制面板开关
 */
public class LightPanel : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("根对象（用于显示/隐藏）")]
    public GameObject PanelRoot;
    [Tooltip("指南面板")]
    public TipManager tipPanel;

    [Header("镜槽预制体")]
    public GameObject MirrorSlotPrefab;

    [Header("镜槽配置（索引从1开始）")]
    [Tooltip("镜槽数组配置，xindex 和 yindex 从 1 开始计数")]
    public MirrorSlot[] mirrorSlots = new MirrorSlot[10];

    [Tooltip("镜槽答案数组，填写 mirrorSlots 中的序号（从 1 开始）")]
    public int[] mirrorSlotAnswers = new int[5];

    // 静态变量
    private static LightPanel s_instance;
    private static bool s_isOpen;
    private static GameObject s_root;
    private static bool s_initialized = false;

    // 谜题完成标志
    private static bool s_isPuzzleCompleted = false;
    private static bool s_tipShown = false;
    
    private const int GRID_COLS = 11; // 列数
    private const int GRID_ROWS = 5;  // 行数
    private const float GRID_START_X = 60f;   // 第一个格子左上角 X 坐标
    private const float GRID_START_Y = -140f; // 第一个格子左上角 Y 坐标
    private const float GRID_END_X = 1739f;   // 最后一个格子右下角 X 坐标
    private const float GRID_END_Y = -887f;   // 最后一个格子右下角 Y 坐标
    
    private Transform backgroundTransform; // Background 容器
    
    // 记录生成的镜槽游戏对象列表，顺序与 mirrorSlots 数组对应
    private GameObject[] generatedMirrorSlots;
    
    void Awake()
    {
        s_instance = this;
    
        // 如果未指定根对象,使用当前GameObject
        if (PanelRoot == null)
            PanelRoot = gameObject;
    
        // 记录静态根,如果根发生变化则重置初始化状态
        if (s_root == null || s_root != PanelRoot)
        {
            s_root = PanelRoot;
            s_initialized = false;
            s_isOpen = false;
            s_isPuzzleCompleted = false;
        }
        if (s_tipShown == true)
        {
            tipPanel.gameObject.SetActive(false);
        }
        s_tipShown = true;
        // 获取 Background 容器
        backgroundTransform = transform.Find("Background");
        if (backgroundTransform == null)
        {
            Debug.LogError("[LightPanel] 未找到 Background 对象");
            return;
        }
    
        // 生成镜槽
        DrawMirrorSlots();
    }

    void Start()
    {
        // 初始化时关闭面板
        if (!s_initialized && s_root != null)
        {
            s_initialized = true;
            s_root.SetActive(true);
            Debug.Log("[LightPanel.Start] 光线面板已初始化并打开");
        }
    }

    void OnDestroy()
    {
        // 若当前实例绑定的根等于静态引用则清理静态状态
        if (PanelRoot != null && s_root == PanelRoot)
        {
            s_root = null;
            s_isOpen = false;
            s_initialized = false;
            s_instance = null;
            s_isPuzzleCompleted = false; // 清理完成标志
        }
    }

    /*
    * 绘制镜槽
    */
    private void DrawMirrorSlots()
    {
        if (MirrorSlotPrefab == null)
        {
            Debug.LogError("[LightPanel] MirrorSlotPrefab 未设置");
            return;
        }

        if (backgroundTransform == null)
        {
            Debug.LogError("[LightPanel] Background 容器未找到");
            return;
        }

        // 计算每个格子的宽度和高度
        float cellWidth = (GRID_END_X - GRID_START_X) / GRID_COLS;
        float cellHeight = (GRID_END_Y - GRID_START_Y) / GRID_ROWS;

        // 初始化生成镜槽数组
        generatedMirrorSlots = new GameObject[mirrorSlots.Length];

        // 遍历镜槽数组并绘制
        for (int i = 0; i < mirrorSlots.Length; i++)
        {
            var slot = mirrorSlots[i];
            if (slot.xindex == 0 && slot.yindex == 0) continue; // 跳过未配置的槽
            // 将 Inspector 中的索引（从 1 开始）转换为数组索引（从 0 开始）
            int arrayXIndex = slot.xindex - 1;
            int arrayYIndex = slot.yindex - 1;

            // 检查索引是否在有效范围内
            if (arrayXIndex < 0 || arrayXIndex >= GRID_COLS ||
                arrayYIndex < 0 || arrayYIndex >= GRID_ROWS)
            {
                Debug.LogWarning($"[LightPanel] 镜槽索引越界: ({slot.xindex}, {slot.yindex})，有效范围为 (1-{GRID_COLS}, 1-{GRID_ROWS})");
                continue;
            }

            // 计算格子中心坐标（相对于 Background）
            float centerX = GRID_START_X + (arrayXIndex + 0.5f) * cellWidth;
            float centerY = GRID_START_Y + (arrayYIndex + 0.5f) * cellHeight;

            // 实例化镜槽预制体，直接作为 Background 的子对象
            GameObject mirrorSlot = Instantiate(MirrorSlotPrefab, backgroundTransform);
            RectTransform rectTransform = mirrorSlot.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                // 设置锚点为左上角
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);

                // 设置位置（相对于 Background 的格子中心）
                rectTransform.anchoredPosition = new Vector2(centerX, centerY);

                // 设置大小
                rectTransform.sizeDelta = new Vector2(140f, 30f);

                // 设置旋转角度
                float rotationAngle = 0f;
                switch (slot.direction)
                {
                    case MirrorSlot.Direction.TOP_LEFT:
                        rotationAngle = 45f;
                        break;
                    case MirrorSlot.Direction.BOTTOM_LEFT:
                        rotationAngle = 135f;
                        break;
                    case MirrorSlot.Direction.BOTTOM_RIGHT:
                        rotationAngle = -135f;
                        break;
                    case MirrorSlot.Direction.TOP_RIGHT:
                        rotationAngle = -45f;
                        break;
                }
                rectTransform.localRotation = Quaternion.Euler(0, 0, rotationAngle);

                // 初始化时禁用 BoxCollider2D
                BoxCollider2D collider = mirrorSlot.GetComponent<BoxCollider2D>();
                if (collider != null)
                    collider.enabled = false;

                // 设置图像为灰度，透明度 50%
                Image image = mirrorSlot.GetComponent<Image>();
                if (image != null)
                {
                    Color grayColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    image.color = grayColor;
                }

                // 在镜槽中央添加数字文本
                GameObject textObj = new GameObject("NumberText");
                textObj.transform.SetParent(mirrorSlot.transform, false);
                
                TextMeshProUGUI numberText = textObj.AddComponent<TextMeshProUGUI>();
                numberText.text = slot.number.ToString();
                numberText.fontSize = 60;
                numberText.alignment = TextAlignmentOptions.Center;
                numberText.color = Color.black;
                
                // 设置 RectTransform 使其填充整个父对象
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
                
                // 反向旋转文本，使其始终保持正向显示
                textRect.localRotation = Quaternion.Euler(0, 0, -rotationAngle);

                // 保存到数组中
                generatedMirrorSlots[i] = mirrorSlot;

                Debug.Log($"[LightPanel] 绘制镜槽[{i}]: 索引({slot.xindex}, {slot.yindex}), " +
                         $"位置({centerX:F2}, {centerY:F2}), 旋转{rotationAngle}°, 数字{slot.number}");
            }
            else
            {
                Debug.LogError("[LightPanel] MirrorSlotPrefab 缺少 RectTransform 组件");
            }
        }
    }


    /*
     * 谜题完成时调用
     */
    public static void OnPuzzleCompleted()
    {
        Debug.Log("[LightPanel] 谜题完成！");
    
        // 设置完成标志
        s_isPuzzleCompleted = true;
    
        // 关闭 Laser 对象
        if (s_instance != null)
        {
            Transform laser = s_instance.backgroundTransform.Find("Laser");
            if (laser != null)
            {
                laser.gameObject.SetActive(false);
                Debug.Log("[LightPanel] Laser 已关闭");
            }
            else
            {
                Debug.LogWarning("[LightPanel] 未找到 Background/Laser");
            }
        }
    
        // 获取 ConsolePanel 下的 ConsoleImage
        Transform consolePanelTransform = s_instance.transform.parent.Find("ConsolePanel");
        Image consoleImage = consolePanelTransform.Find("ConsoleImage")?.GetComponent<Image>();    
        Sprite icon = consoleImage.sprite;
    
        EventBus.LocalPublish(new PuzzleCompletedEvent
        {
            sceneName = "Light"
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

        // 打开控制台面板
        ConsolePanel.TogglePanel();

        // 同步线索到日记
        if (TimelinePlayer.Local != null)
        {
            Sprite sprite = Resources.Load<Sprite>("Clue_Light1");
            int timeline = TimelinePlayer.Local.timeline;
            int level = TimelinePlayer.Local.currentLevel;
            // 压缩图片，避免过大
            byte[] spriteBytes = ImageUtils.CompressSpriteToJpegBytes(sprite, 80);
            ClueBoard.AddClueEntry(timeline, level, spriteBytes);
        }
    }
    
    void Update()
    {
        // 检查谜题是否完成
        if (!s_isPuzzleCompleted)
        {
            CheckPuzzleCompletion();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ConsolePanel.TogglePanel();
            Debug.Log("[LightPanel] P键按下，切换控制台面板。");
        }
    }

    /*
    * 检查谜题是否完成
    */
    private float puzzleCompletionTimer = 0f; // 谜题完成计时器
    private bool isWaitingForCompletion = false; // 是否正在等待完成

    private void CheckPuzzleCompletion()
    {
        if (mirrorSlotAnswers == null || mirrorSlotAnswers.Length == 0)
            return;

        if (generatedMirrorSlots == null)
            return;

        // 检查所有答案镜槽是否都被激活
        bool allActivated = true;
        foreach (int answerIndex in mirrorSlotAnswers)
        {
            // 将 Inspector 中的索引（从 1 开始）转换为数组索引（从 0 开始）
            int arrayIndex = answerIndex - 1;

            // 检查索引是否有效
            if (arrayIndex < 0 || arrayIndex >= generatedMirrorSlots.Length)
            {
                Debug.LogWarning($"[LightPanel] 答案索引 {answerIndex} 超出范围，有效范围为 (1-{generatedMirrorSlots.Length})");
                allActivated = false;
                break;
            }

            GameObject mirrorSlot = generatedMirrorSlots[arrayIndex];
            if (mirrorSlot == null)
            {
                allActivated = false;
                break;
            }

            // 检查镜槽是否被激活（通过 BoxCollider2D.enabled 判断）
            BoxCollider2D collider = mirrorSlot.GetComponent<BoxCollider2D>();
            if (collider == null || !collider.enabled)
            {
                allActivated = false;
                break;
            }
        }

        // 如果所有镜槽都已激活
        if (allActivated)
        {
            if (!isWaitingForCompletion)
            {
                // 第一次检测到所有镜槽激活，开始计时
                isWaitingForCompletion = true;
                puzzleCompletionTimer = 0f;
                Debug.Log("[LightPanel] 所有答案镜槽已激活，1秒后完成谜题");
            }
            else
            {
                // 继续计时
                puzzleCompletionTimer += Time.deltaTime;
                if (puzzleCompletionTimer >= 0.5f)
                {
                    // 0.5秒后调用完成函数
                    OnPuzzleCompleted();
                }
            }
        }
        else
        {
            // 如果有镜槽未激活，重置计时器
            if (isWaitingForCompletion)
            {
                isWaitingForCompletion = false;
                puzzleCompletionTimer = 0f;
                Debug.Log("[LightPanel] 有镜槽未激活，重置计时器");
            }
        }
    }
}