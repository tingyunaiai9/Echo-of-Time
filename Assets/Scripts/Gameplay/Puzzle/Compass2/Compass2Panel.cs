using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Events;
using Game.UI;

public class Compass2Panel : PuzzleManager, IPointerClickHandler
{
    [Header("图像配置")]
    [Tooltip("中心图像对象")]
    public RectTransform InnerImage;
    [Tooltip("指针图像对象")]
    public RectTransform PointerImage;
    [Tooltip("内圈图像对象")]
    public RectTransform Circle1;
    [Tooltip("中圈图像对象")]
    public RectTransform Circle2;
    [Tooltip("外圈图像对象")]
    public RectTransform Circle3;
    [Tooltip("提示面板")]
    public NotificationController notificationController;
    [Tooltip("指南面板")]
    public TipManager tipPanel;

    [Header("圆环参数")]
    [Tooltip("第一圈内半径")]
    public float radius1 = 150f;
    [Tooltip("第二圈内半径")]
    public float radius2 = 220f;
    [Tooltip("第三圈内半径")]
    public float radius3 = 335f;
    [Tooltip("第三圈外半径")]
    public float radius4 = 490f;
    [Tooltip("旋转角度")]
    public float rotationAngle = -60f;
    [Tooltip("旋转动画时长（秒）")]
    public float rotationDuration = 0.3f;
    [Tooltip("金黄色颜色值")]
    public Color goldenColor = new Color(1f, 0.84f, 0f, 1f); // 金黄色

    private int innerRotationProgress = 0;
    private int middleRotationProgress = 0;
    private int outerRotationProgress = 0;
    [Header("旋转步骤规则")]
    [Tooltip("内圈需要的旋转次数")]
    public int innerTargetRotations = 4;
    [Tooltip("中圈需要的旋转次数")]
    public int middleTargetRotations = 2;

    [Tooltip("外圈需要的旋转次数")]
    public int outerTargetRotations = 1;

    private bool isPuzzleCompleted = false;
    private static bool s_tipShown = false;

    void Awake()
    {
        if (Circle1 == null) Debug.LogError("[Compass2Panel] InnerImage 未设置！请在 Inspector 中分配该引用.");
        if (Circle2 == null) Debug.LogError("[Compass2Panel] MiddleImage 未设置！请在 Inspector 中分配该引用.");
        if (Circle3 == null) Debug.LogError("[Compass2Panel] OuterImage 未设置！请在 Inspector 中分配该引用.");
        if (notificationController == null) Debug.LogError("[Compass2Panel] NotificationController 未设置！请在 Inspector 中分配该引用.");
        if (tipPanel == null) Debug.LogError("[Compass2Panel] TipPanel 未设置！请在 Inspector 中分配该引用.");
        if (s_tipShown == true)
        {
            tipPanel.gameObject.SetActive(false);
        }
        s_tipShown = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Circle1 == null || Circle2 == null || Circle3 == null)
        {
            Debug.LogError("[Compass2Panel] 必须设置 InnerImage, MiddleImage 和 OuterImage！");
            return;
        }

        // 获取点击位置相对于 InnerImage 中心的本地坐标
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            InnerImage,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        // 计算点击位置距离 InnerImage 中心的距离
        float distance = localPoint.magnitude;
        Debug.Log($"[Compass2Panel] 点击位置距离中心的距离: {distance}");
        if (distance >= radius1 && distance < radius2)
        {
            // 点击在第一圈范围内
            RotateImage(Circle1, ref innerRotationProgress, innerTargetRotations, rotationAngle, -rotationAngle, localPoint.x);
        }
        else if (distance >= radius2 && distance < radius3)
        {
            // 点击在第二圈范围内
            RotateImage(Circle2, ref middleRotationProgress, middleTargetRotations, rotationAngle, -rotationAngle, localPoint.x);
        }
        else if (distance >= radius3 && distance < radius4)
        {
            // 点击在第三圈范围内
            RotateImage(Circle3, ref outerRotationProgress, outerTargetRotations, rotationAngle, -rotationAngle, localPoint.x);
        }
        else
        {
            // 点击在其他区域，忽略
            Debug.Log("[Compass2Panel] 点击位置不在可旋转区域内，忽略该点击。");
        }

    }

    private void RotateImage(RectTransform image, ref int progress, int target, float clockwiseAngle, float counterClockwiseAngle, float clickX)
    {
        if (image == null) return;

        float angle = clickX > 0 ? clockwiseAngle : counterClockwiseAngle;
        LeanTween.rotateZ(image.gameObject, image.localEulerAngles.z + angle, rotationDuration)
            .setEase(LeanTweenType.easeInOutQuad);

        // 更新旋转进度
        progress += clickX > 0 ? 1 : -1;
        Debug.Log($"[Compass2Panel] 当前进度: {progress}/{target}");

        // 检查是否完成
        CheckPuzzleCompletion();
    }

    private void CheckPuzzleCompletion()
    {
        int inner = ((innerRotationProgress % 6) + 6) % 6;
        int middle = ((middleRotationProgress % 6) + 6) % 6;
        int outer = ((outerRotationProgress % 6) + 6) % 6;
        int innerTarget = ((innerTargetRotations % 6) + 6) % 6;
        int middleTarget = ((middleTargetRotations % 6) + 6) % 6;
        int outerTarget = ((outerTargetRotations % 6) + 6) % 6;

        if (!isPuzzleCompleted && inner == innerTarget && middle == middleTarget && outer == outerTarget)
        {
            isPuzzleCompleted = true;
            OnPuzzleCompleted();
        }
    }

    public override void OnPuzzleCompleted()
    {
        Debug.Log("[Compass2Panel] 谜题完成！");

        EventBus.LocalPublish(new PuzzleCompletedEvent
        {
            sceneName = "Compass2"
        });
        EventBus.LocalPublish(new LevelProgressEvent
        {
        });
    
        EventBus.LocalPublish(new ClueDiscoveredEvent
        {
            isKeyClue = true,
            playerNetId = 0,
            clueId = "compass2_clue",
            clueText = "转动三圈指南针完成后，显现出的图案。",
            clueDescription = "这个图案似乎隐藏着某种意义。",
            icon = Circle3.GetComponent<Image>()?.sprite,
            image = Circle3.GetComponent<Image>()?.sprite
        });
        notificationController.ShowNotification("谜题已完成！空间似乎发生了奇妙的变化");
    
        // 添加绿色透明遮罩，并在动画完成后激活 EndSceneIntro
        int completedCount = 0;
        int totalCount = 5; // 1个指针动画 + 4个金色遮罩动画

        System.Action onOverlayComplete = () =>
        {
            completedCount++;
            if (completedCount >= totalCount)
            {
                // 所有遮罩动画播放完毕后，激活 EndSceneIntro
                var intros = Resources.FindObjectsOfTypeAll<SceneIntro>();
                foreach (var intro in intros)
                {
                    if (intro.gameObject.scene.IsValid() && intro.gameObject.name == "EndSceneIntro")
                    {
                        Debug.Log("[Compass2Panel] 所有遮罩动画完成，找到 EndSceneIntro，正在激活...");
                        intro.gameObject.SetActive(true);
                        break;
                    }
                }

                // 自动退出谜题场景
                if (PuzzleOverlayManager.Instance != null)
                {
                    PuzzleOverlayManager.Instance.ClosePuzzle();
                }
            }
        };
        
        DisplayRotation(PointerImage, onOverlayComplete);
        AddGoldenOverlay(InnerImage, onOverlayComplete);
        AddGoldenOverlay(Circle1, onOverlayComplete);
        AddGoldenOverlay(Circle2, onOverlayComplete);
        AddGoldenOverlay(Circle3, onOverlayComplete);
    }

    private void DisplayRotation(RectTransform image, System.Action onComplete)
    {
        if (image == null)
        {
            onComplete?.Invoke();
            return;
        }
    
        // 获取或添加 CanvasGroup 组件
        CanvasGroup canvasGroup = image.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = image.gameObject.AddComponent<CanvasGroup>();
        }
    
        // 设置初始透明度为 0
        canvasGroup.alpha = 0f;

        // 动画：渐显（与加速旋转同时进行）
        LeanTween.alphaCanvas(canvasGroup, 1f, 0.5f);

        // 加速旋转动画（旋转1080度）
        LeanTween.rotateAroundLocal(image.gameObject, Vector3.forward, 1080f, 1f)
            .setEase(LeanTweenType.easeInQuad)
            .setOnComplete(() =>
            {
                // 旋转完成后开始渐隐
                LeanTween.alphaCanvas(canvasGroup, 0f, 0.5f).setOnComplete(() =>
                {
                    // 动画结束后通知完成
                    onComplete?.Invoke();
                });
            });
    }
    
    // 为指定的图像添加金色透明遮罩
    private void AddGoldenOverlay(RectTransform image, System.Action onComplete)
    {
        if (image == null)
        {
            onComplete?.Invoke();
            return;
        }
    
        // 获取或添加 CanvasGroup 组件
        CanvasGroup canvasGroup = image.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = image.gameObject.AddComponent<CanvasGroup>();
        }
    
        // 设置初始透明度为 0
        canvasGroup.alpha = 0f;
    
        // 添加金色遮罩
        Image overlay = image.GetComponent<Image>();
        if (overlay == null)
        {
            overlay = image.gameObject.AddComponent<Image>();
        }
        overlay.color = goldenColor;
    
        // 动画：渐显 -> 等待 -> 渐隐
        LeanTween.alphaCanvas(canvasGroup, 1f, 0.5f).setOnComplete(() =>
        {
            LeanTween.alphaCanvas(canvasGroup, 0f, 0.5f).setOnComplete(() =>
            {
                // 动画结束后移除遮罩
                Destroy(overlay);
                // 通知动画完成
                onComplete?.Invoke();
            });
        });
    }

    public void ResetPuzzle()
    {
        // 重置旋转进度
        innerRotationProgress = 0;
        middleRotationProgress = 0;
        outerRotationProgress = 0;
        isPuzzleCompleted = false;

        // 重置图像旋转
        if (Circle1 != null)
        {
            Circle1.localEulerAngles = Vector3.zero;
        }
        if (Circle2 != null)
        {
            Circle2.localEulerAngles = Vector3.zero;
        }
        if (Circle3 != null)
        {
            Circle3.localEulerAngles = Vector3.zero;
        }

        Debug.Log("[Compass2Panel] 谜题已重置");
    }
}