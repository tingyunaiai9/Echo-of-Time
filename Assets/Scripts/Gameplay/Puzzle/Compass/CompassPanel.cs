using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Game.Gameplay.Puzzle.Paint2;
using Events;
using Game.UI;

public class CompassPanel : PuzzleManager, IPointerClickHandler
{
    [Header("图像配置")]
    [Tooltip("外圈图像对象")]
    public RectTransform OuterImage;
    [Tooltip("内圈图像对象（自动查找）")]
    public RectTransform InnerImage;
    [Tooltip("结果图像对象")]
    public RectTransform ResultImage;
    [Tooltip("提示面板")]
    public NotificationController notificationController;
    [Tooltip("指南面板")]
    public TipManager tipPanel;
    
    [Header("圆环参数")]
    [Tooltip("圆环内半径")]
    public float innerRadius = 220f;

    [Tooltip("圆环外半径")]
    public float outerRadius = 500f;

    [Tooltip("旋转角度")]
    public float rotationAngle = 30f;

    [Tooltip("旋转动画时长（秒）")]
    public float rotationDuration = 0.3f;

    private RectTransform panelTransform;
    private Canvas canvas;

    private Outline outerOutline;
    private Outline innerOutline;
    private bool outerOutlineOriginalEnabled;
    private bool innerOutlineOriginalEnabled;
    private Color outerOutlineOriginalColor;
    private Color innerOutlineOriginalColor;

    [Header("数字显示")]
    [Tooltip("DigitPanel 下的 6 个 Text (TMP) 组件，对应 Image1 到 Image6")]
    public TextMeshProUGUI[] digitTexts = new TextMeshProUGUI[6];

    [Header("旋转步骤规则")]
    [Tooltip("正数为顺时针次数，负数为逆时针次数（正方向为顺时针）")]
    public int[] sequence = new int[] { 3, -1, 4, -5, 6, -2 };
    private int stepIndex = 0;
    private int stepProgress = 0;
    private Coroutine flashCoroutine;
    private bool isPuzzleCompleted = false; // 标记谜题是否已完成
    private static bool s_tipShown = false;

    public void Awake()
    {
        panelTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        // 检查必要组件是否已分配
        if (OuterImage == null) Debug.LogError("[CompassPanel] OuterImage 未设置！请在 Inspector 中分配该引用。");
        if (InnerImage == null) Debug.LogError("[CompassPanel] InnerImage 未设置！请在 Inspector 中分配该引用.");
        if (ResultImage == null) Debug.LogError("[CompassPanel] ResultImage 未设置！请在 Inspector 中分配该引用.");
        if (notificationController == null) Debug.LogError("[CompassPanel] NotificationController 未设置！请在 Inspector 中分配该引用.");
        if (tipPanel == null) Debug.LogError("[CompassPanel] TipPanel 未设置！请在 Inspector 中分配该引用.");
        if (s_tipShown == true)
        {
            tipPanel.gameObject.SetActive(false);
        }
        s_tipShown = true;

        // 获取 Outline 组件并记录初始状态
        if (OuterImage != null)
        {
            outerOutline = OuterImage.GetComponent<Outline>();
            if (outerOutline != null)
            {
                outerOutlineOriginalEnabled = outerOutline.enabled;
                outerOutlineOriginalColor = outerOutline.effectColor;
                outerOutline.enabled = false; // 初始不显示
            }
        }

        if (InnerImage != null)
        {
            innerOutline = InnerImage.GetComponent<Outline>();
            if (innerOutline != null)
            {
                innerOutlineOriginalEnabled = innerOutline.enabled;
                innerOutlineOriginalColor = innerOutline.effectColor;
                innerOutline.enabled = false; // 初始不显示
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (InnerImage == null)
        {
            Debug.LogError("[CompassPanel] InnerImage 未设置！");
            return;
        }
    
        // 获取点击位置相对于 InnerImage 中心的本地坐标
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            InnerImage, // 将中心点设置为 InnerImage 的中心
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
    
        // 计算点击位置距离 InnerImage 中心的距离
        float distance = localPoint.magnitude;
    
        // 检查是否在圆环范围内
        if (distance >= innerRadius && distance <= outerRadius)
        {
            // 判断是在左半侧还是右半侧
            if (localPoint.x > 0)
            {
                // 右半侧：顺时针旋转（减少角度）
                RotateOuterImage(-rotationAngle);
                Debug.Log($"[CompassPanel] 右半侧点击，顺时针旋转 {rotationAngle} 度");
            }
            else
            {
                // 左半侧：逆时针旋转（增加角度）
                RotateOuterImage(rotationAngle);
                Debug.Log($"[CompassPanel] 左半侧点击，逆时针旋转 {rotationAngle} 度");
            }
        }
        else
        {
            Debug.Log($"[CompassPanel] 点击位置距离中心 {distance:F2}pt，不在圆环范围内");
        }
    }   

    /* 旋转外圈图像
       参数 angle: 旋转角度（正数为逆时针，负数为顺时针） */
    private void RotateOuterImage(float angle)
    {
        if (OuterImage == null)
        {
            Debug.LogError("[PaintPanel] OuterImage 未设置！");
            return;
        }

        // 使用 LeanTween 添加平滑旋转动画
        float targetRotation = OuterImage.localEulerAngles.z + angle;
        LeanTween.rotateZ(OuterImage.gameObject, targetRotation, rotationDuration)
            .setEase(LeanTweenType.easeInOutQuad);

        // 检查旋转是否满足当前步骤（按一次点击计一次旋转）
        CheckCorrectRotation(angle);
    }

    // 检查当前旋转是否符合步骤序列
    private void CheckCorrectRotation(float angle)
    {
        // 按题意：正方向为顺时针。当前函数接受的 angle 中，负值表示顺时针点击，正值表示逆时针点击。
        int dir = angle < 0 ? 1 : -1; // dir == 1 表示顺时针，dir == -1 表示逆时针

        int required = sequence[stepIndex];
        int requiredSign = required > 0 ? 1 : -1;

        if (dir == requiredSign)
        {
            // 方向正确，增加进度
            stepProgress++;
            if (Mathf.Abs(required) <= stepProgress)
            {
                // 当前步骤完成
                OnCorrectRotation(true);
            }
            // 否则继续等待更多相同方向的旋转
        }
        else
        {
            // 方向错误，闪红并重置当前步骤进度
            OnCorrectRotation(false);
            stepProgress = 0;
        }
    }

    // 当步骤完成或方向错误时调用
    // success 为 true 时显示绿色并前进到下一步骤；为 false 时显示红色并重置所有步骤进度
    private void OnCorrectRotation(bool success)
    {
        // 取消已有闪烁协程，避免颜色冲突
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        if (success)
        {
            flashCoroutine = StartCoroutine(FlashOutlines(Color.green, 0.5f));
            
            // 更新对应的数字显示
            UpdateDigitText(stepIndex);
            
            // 前进到下一步
            stepIndex++;
            stepProgress = 0;
            
            // 检查是否完成所有步骤
            if (stepIndex >= sequence.Length && !isPuzzleCompleted)
            {
                Debug.Log($"[CompassPanel] 所有步骤完成！");
                isPuzzleCompleted = true; // 标记为已完成
                
                if (Paint2Manager.Instance != null)
                {
                    Paint2Manager.Instance.CmdSetCompassSolved(true);
                }
                
                // 调用谜题完成函数
                OnPuzzleCompleted();
            }
            else
            {
                Debug.Log($"[CompassPanel] 步骤完成，进入第 {stepIndex} 步");
            }
        }
        else
        {
            flashCoroutine = StartCoroutine(FlashOutlines(Color.red, 0.5f));
            ResetPuzzle();
            Debug.Log("[PaintPanel] 方向错误，已闪红并重置所有步骤进度");
        }
    }

    // 更新指定索引的数字文本
    private void UpdateDigitText(int index)
    {
        if (index < 0 || index >= digitTexts.Length)
        {
            Debug.LogWarning($"[CompassPanel] 索引 {index} 超出范围");
            return;
        }

        if (digitTexts[index] != null)
        {
            int rotationCount = Mathf.Abs(sequence[index]);
            digitTexts[index].text = rotationCount.ToString();
            Debug.Log($"[CompassPanel] 更新 Image{index + 1} 的文本为 {rotationCount}");
        }
    }

    // 闪烁两个 Outline（Outer 与 Inner），持续 duration 秒，结束后恢复初始状态
    private IEnumerator FlashOutlines(Color flashColor, float duration)
    {
        // 记录并设置
        if (outerOutline != null)
        {
            outerOutline.enabled = true;
            outerOutline.effectColor = flashColor;
        }
        if (innerOutline != null)
        {
            innerOutline.enabled = true;
            innerOutline.effectColor = flashColor;
        }

        yield return new WaitForSeconds(duration);

        // 恢复原始状态
        if (outerOutline != null)
        {
            outerOutline.effectColor = outerOutlineOriginalColor;
            outerOutline.enabled = outerOutlineOriginalEnabled;
        }
        if (innerOutline != null)
        {
            innerOutline.effectColor = innerOutlineOriginalColor;
            innerOutline.enabled = innerOutlineOriginalEnabled;
        }

        flashCoroutine = null;
    }

    public override void OnPuzzleCompleted()
    {
        // 确保 InnerImage 和 OuterImage 存在
        if (InnerImage != null)
        {
            // InnerImage 渐渐透明直至消失
            CanvasGroup innerCanvasGroup = InnerImage.GetComponent<CanvasGroup>();
            if (innerCanvasGroup == null)
            {
                innerCanvasGroup = InnerImage.gameObject.AddComponent<CanvasGroup>();
            }
            LeanTween.alphaCanvas(innerCanvasGroup, 0f, 0.5f).setOnComplete(() =>
            {
                InnerImage.gameObject.SetActive(false); // 动画完成后禁用对象
            });
        }
    
        if (OuterImage != null)
        {
            // OuterImage 渐渐透明直至消失
            CanvasGroup outerCanvasGroup = OuterImage.GetComponent<CanvasGroup>();
            if (outerCanvasGroup == null)
            {
                outerCanvasGroup = OuterImage.gameObject.AddComponent<CanvasGroup>();
            }
            LeanTween.alphaCanvas(outerCanvasGroup, 0f, 0.5f).setOnComplete(() =>
            {
                OuterImage.gameObject.SetActive(false); // 动画完成后禁用对象
            });
        }
    
        // 确保 ResultImage 存在
        if (ResultImage != null)
        {
            ResultImage.gameObject.SetActive(true); // 确保对象激活
            // ResultImage 渐渐出现
            CanvasGroup resultCanvasGroup = ResultImage.GetComponent<CanvasGroup>();
            if (resultCanvasGroup == null)
            {
                resultCanvasGroup = ResultImage.gameObject.AddComponent<CanvasGroup>();
                resultCanvasGroup.alpha = 0f; // 确保初始透明
            }
            LeanTween.alphaCanvas(resultCanvasGroup, 1f, 0.5f);
        }

        EventBus.LocalPublish(new PuzzleCompletedEvent
        {
            sceneName = "Compass"
        });

        EventBus.LocalPublish(new LevelProgressEvent
        {
        });

        EventBus.LocalPublish(new ClueDiscoveredEvent
        {
            isKeyClue = true,
            playerNetId = 0,
            clueId = "compass_clue",
            clueText = "转动指南针完成后，显现出的图案。",
            clueDescription = "这个图案似乎隐藏着某种意义。",
            icon = ResultImage.GetComponent<Image>()?.sprite,
            image = ResultImage.GetComponent<Image>()?.sprite
        });

        if (TimelinePlayer.Local != null)
        {
            Sprite sprite = Resources.Load<Sprite>("Clue_Compass1");
            int timeline = TimelinePlayer.Local.timeline;
            int level = TimelinePlayer.Local.currentLevel;
            // 压缩图片，避免过大
            byte[] spriteBytes = ImageUtils.CompressSpriteToJpegBytes(sprite, 80);
            Debug.Log($"[UIManager] 线索图片压缩成功，大小：{spriteBytes.Length} 字节");
            ClueBoard.AddClueEntry(timeline, level, spriteBytes);
        }
        notificationController.ShowNotification("谜题已完成！新线索已添加到日记及背包当中。");
    }

    /* 重置旋转状态，清空计数并重置所有数字显示 */
    public void ResetPuzzle()
    {
        // 重置当前步骤索引
        stepIndex = 0;
        stepProgress = 0;
        isPuzzleCompleted = false; // 重置完成标志
    
        // 重置所有数字文本为 "?"
        for (int i = 0; i < digitTexts.Length; i++)
        {
            if (digitTexts[i] != null)
            {
                digitTexts[i].text = "?";
            }
        }
    
        // 激活 InnerImage 和 OuterImage，取消激活 ResultImage
        if (InnerImage != null)
        {
            InnerImage.gameObject.SetActive(true);
            // 重置 InnerImage 的透明度
            CanvasGroup innerCanvasGroup = InnerImage.GetComponent<CanvasGroup>();
            if (innerCanvasGroup != null)
            {
                innerCanvasGroup.alpha = 1f;
            }
            // 重置 InnerImage 的旋转
            InnerImage.localEulerAngles = Vector3.zero;
        }
    
        if (OuterImage != null)
        {
            OuterImage.gameObject.SetActive(true);
            // 重置 OuterImage 的透明度
            CanvasGroup outerCanvasGroup = OuterImage.GetComponent<CanvasGroup>();
            if (outerCanvasGroup != null)
            {
                outerCanvasGroup.alpha = 1f;
            }
            // 重置 OuterImage 的旋转
            OuterImage.localEulerAngles = Vector3.zero;
        }
    
        if (ResultImage != null)
        {
            ResultImage.gameObject.SetActive(false);
            // 重置 ResultImage 的透明度（以备下次使用）
            CanvasGroup resultCanvasGroup = ResultImage.GetComponent<CanvasGroup>();
            if (resultCanvasGroup != null)
            {
                resultCanvasGroup.alpha = 0f;
            }
        }
    
        Debug.Log("[CompassPanel] 旋转状态、图像激活状态和旋转角度已重置");
    }}
