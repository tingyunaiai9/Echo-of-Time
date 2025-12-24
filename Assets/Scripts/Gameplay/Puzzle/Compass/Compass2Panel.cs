using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Events;

public class Compass2Panel : MonoBehaviour, IPointerClickHandler
{
    [Header("配置")]
    [Tooltip("内圈图像对象")]
    public RectTransform InnerImage;

    [Tooltip("中圈图像对象")]
    public RectTransform MiddleImage;

    [Tooltip("外圈图像对象")]
    public RectTransform OuterImage;

    [Tooltip("圆环内半径")]
    public float innerRadius = 165f;

    [Tooltip("圆环外半径")]
    public float outerRadius = 350f;

    [Tooltip("旋转角度")]
    public float rotationAngle = 60f;

    [Tooltip("旋转动画时长（秒）")]
    public float rotationDuration = 0.3f;

    private int middleRotationProgress = 0;
    private int outerRotationProgress = 0;

    [Header("旋转步骤规则")]
    [Tooltip("中圈需要的旋转次数")]
    public int middleTargetRotations = 1;

    [Tooltip("外圈需要的旋转次数")]
    public int outerTargetRotations = 2;

    private bool isPuzzleCompleted = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            OnPuzzleCompleted();
            Debug.Log("[Compass2Panel] 按下 P 键，触发谜题完成效果");
        }
    }

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
        if (!isPuzzleCompleted && middleRotationProgress%6 == middleTargetRotations && outerRotationProgress%6 == outerTargetRotations)
        {
            isPuzzleCompleted = true;
            OnPuzzleCompleted();
        }
    }

    private void OnPuzzleCompleted()
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
    
        // 添加绿色透明遮罩
        AddGreenOverlay(InnerImage);
        AddGreenOverlay(MiddleImage);
        AddGreenOverlay(OuterImage);


        // 查找名为 "EndSceneIntro" 的物体并激活
        var intros = Resources.FindObjectsOfTypeAll<SceneIntro>();
        foreach (var intro in intros)
        {
            if (intro.gameObject.scene.IsValid() && intro.gameObject.name == "EndSceneIntro")
            {
                Debug.Log("[Compass2Panel] 找到 EndSceneIntro，正在激活...");
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
    
    // 为指定的图像添加绿色透明遮罩
    private void AddGreenOverlay(RectTransform image)
    {
        if (image == null) return;
    
        // 获取或添加 CanvasGroup 组件
        CanvasGroup canvasGroup = image.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = image.gameObject.AddComponent<CanvasGroup>();
        }
    
        // 设置初始透明度为 0
        canvasGroup.alpha = 0f;
    
        // 添加绿色遮罩
        Image overlay = image.GetComponent<Image>();
        if (overlay == null)
        {
            overlay = image.gameObject.AddComponent<Image>();
        }
        overlay.color = new Color(0f, 1f, 0f, 0.5f); // 半透明绿色
    
        // 动画：渐显 -> 等待 -> 渐隐
        LeanTween.alphaCanvas(canvasGroup, 1f, 0.5f).setOnComplete(() =>
        {
            LeanTween.alphaCanvas(canvasGroup, 0f, 0.5f).setOnComplete(() =>
            {
                // 动画结束后移除遮罩
                Destroy(overlay);
            });
        });
    }}