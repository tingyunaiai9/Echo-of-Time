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
    [Tooltip("内圈图像对象")]
    public RectTransform InnerImage;
    [Tooltip("中圈图像对象")]
    public RectTransform MiddleImage;
    [Tooltip("外圈图像对象")]
    public RectTransform OuterImage;
    [Tooltip("提示面板")]
    public NotificationController notificationController;

    [Header("圆环参数")]
    [Tooltip("圆环内半径")]
    public float innerRadius = 165f;
    [Tooltip("圆环外半径")]
    public float outerRadius = 350f;
    [Tooltip("旋转角度")]
    public float rotationAngle = 60f;
    [Tooltip("旋转动画时长（秒）")]
    public float rotationDuration = 0.3f;
    [Tooltip("金黄色颜色值")]
    public Color goldenColor = new Color(1f, 0.84f, 0f, 1f); // 金黄色


    private int middleRotationProgress = 0;
    private int outerRotationProgress = 0;

    [Header("旋转步骤规则")]
    [Tooltip("中圈需要的旋转次数")]
    public int middleTargetRotations = 1;

    [Tooltip("外圈需要的旋转次数")]
    public int outerTargetRotations = 2;

    private bool isPuzzleCompleted = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (InnerImage == null || MiddleImage == null || OuterImage == null)
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

        // 检查点击范围并执行相应旋转
        if (distance >= innerRadius && distance <= outerRadius)
        {
            if (distance <= (innerRadius + outerRadius) / 2)
            {
                // 中圈点击
                RotateImage(MiddleImage, ref middleRotationProgress, middleTargetRotations, -rotationAngle, rotationAngle, localPoint.x);
            }
            else
            {
                // 外圈点击
                RotateImage(OuterImage, ref outerRotationProgress, outerTargetRotations, -rotationAngle, rotationAngle, localPoint.x);
            }
        }
        else
        {
            Debug.Log($"[Compass2Panel] 点击位置距离中心 {distance:F2}pt，不在圆环范围内");
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
        int middle = ((middleRotationProgress % 6) + 6) % 6;
        int outer = ((outerRotationProgress % 6) + 6) % 6;
        int middleTarget = ((middleTargetRotations % 6) + 6) % 6;
        int outerTarget = ((outerTargetRotations % 6) + 6) % 6;

        if (!isPuzzleCompleted && middle == middleTarget && outer == outerTarget)
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
    
        EventBus.LocalPublish(new ClueDiscoveredEvent
        {
            isKeyClue = true,
            playerNetId = 0,
            clueId = "compass2_clue",
            clueText = "转动三圈指南针完成后，显现出的图案。",
            clueDescription = "这个图案似乎隐藏着某种意义。",
            icon = OuterImage.GetComponent<Image>()?.sprite,
            image = OuterImage.GetComponent<Image>()?.sprite
        });
        notificationController.ShowNotification("谜题已完成！空间似乎发生了奇妙的变化");
    
        // 添加绿色透明遮罩，并在动画完成后激活 EndSceneIntro
        int completedCount = 0;
        int totalCount = 3;

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

        AddGoldenOverlay(InnerImage, onOverlayComplete);
        AddGoldenOverlay(MiddleImage, onOverlayComplete);
        AddGoldenOverlay(OuterImage, onOverlayComplete);
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
        middleRotationProgress = 0;
        outerRotationProgress = 0;
        isPuzzleCompleted = false;

        // 重置图像旋转
        if (MiddleImage != null)
        {
            MiddleImage.localEulerAngles = Vector3.zero;
        }
        if (OuterImage != null)
        {
            OuterImage.localEulerAngles = Vector3.zero;
        }

        Debug.Log("[Compass2Panel] 谜题已重置");
    }
}