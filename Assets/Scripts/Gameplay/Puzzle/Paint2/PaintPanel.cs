using UnityEngine;
using UnityEngine.EventSystems;

public class PaintPanel : MonoBehaviour, IPointerClickHandler
{
    [Header("配置")]
    [Tooltip("外圈图像对象")]
    public RectTransform OuterImage;

    [Tooltip("圆环内半径")]
    public float innerRadius = 440f;

    [Tooltip("圆环外半径")]
    public float outerRadius = 1000f;

    [Tooltip("旋转角度")]
    public float rotationAngle = 30f;

    [Tooltip("旋转动画时长（秒）")]
    public float rotationDuration = 0.3f;

    private RectTransform panelTransform;
    private Canvas canvas;

    void Awake()
    {
        panelTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        // 如果未手动设置 OuterImage，尝试自动查找
        if (OuterImage == null)
        {
            Transform outerTransform = transform.Find("OuterImage");
            if (outerTransform != null)
            {
                OuterImage = outerTransform.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogError("[PaintPanel] 未找到 OuterImage 对象！");
            }
        }
    }

    // 实现 IPointerClickHandler 接口
    public void OnPointerClick(PointerEventData eventData)
    {
        // 获取点击位置相对于 PaintPanel 中心的本地坐标
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint
        );

        // 计算点击位置距离中心的距离
        float distance = localPoint.magnitude;

        // 检查是否在圆环范围内
        if (distance >= innerRadius && distance <= outerRadius)
        {
            // 判断是在左半侧还是右半侧
            if (localPoint.x > 0)
            {
                // 右半侧：顺时针旋转（减少角度）
                RotateOuterImage(-rotationAngle);
                Debug.Log($"[PaintPanel] 右半侧点击，顺时针旋转 {rotationAngle} 度");
            }
            else
            {
                // 左半侧：逆时针旋转（增加角度）
                RotateOuterImage(rotationAngle);
                Debug.Log($"[PaintPanel] 左半侧点击，逆时针旋转 {rotationAngle} 度");
            }
        }
        else
        {
            Debug.Log($"[PaintPanel] 点击位置距离中心 {distance:F2}pt，不在圆环范围内");
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
    }

    // 可视化调试：在 Scene 视图中绘制圆环范围
    void OnDrawGizmos()
    {
        if (panelTransform == null)
            panelTransform = GetComponent<RectTransform>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        // 获取 Canvas 的缩放因子
        float scaleFactor = 1f;
        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Overlay 模式下，使用 Canvas 的 scaleFactor
                scaleFactor = canvas.scaleFactor;
            }
            else if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
            {
                // Camera 或 WorldSpace 模式下，使用 RectTransform 的 lossyScale
                scaleFactor = panelTransform.lossyScale.x;
            }
        }

        Vector3 centerPos = panelTransform.position;

        // 绘制内圆（应用缩放因子）
        Gizmos.color = Color.yellow;
        DrawCircle(centerPos, innerRadius * scaleFactor, 64);

        // 绘制外圆（应用缩放因子）
        Gizmos.color = Color.green;
        DrawCircle(centerPos, outerRadius * scaleFactor, 64);

        // 绘制中线（应用缩放因子）
        Gizmos.color = Color.red;
        Vector3 upDir = panelTransform.up;
        Gizmos.DrawLine(centerPos + upDir * outerRadius * scaleFactor, 
                       centerPos - upDir * outerRadius * scaleFactor);
    }

    /* 绘制圆形
       参数 center: 圆心位置
       参数 radius: 半径（世界坐标单位）
       参数 segments: 分段数 */
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        
        // 使用 RectTransform 的旋转来正确绘制圆环
        Quaternion rotation = panelTransform.rotation;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            // 在本地空间计算点，然后应用旋转
            Vector3 localP1 = new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * radius;
            Vector3 localP2 = new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * radius;

            Vector3 p1 = center + rotation * localP1;
            Vector3 p2 = center + rotation * localP2;

            Gizmos.DrawLine(p1, p2);
        }
    }
}