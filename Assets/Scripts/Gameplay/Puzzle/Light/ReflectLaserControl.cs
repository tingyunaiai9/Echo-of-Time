using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    [SerializeField] public float thickness = 5;
    [SerializeField] private float noiseScale = 3.14f;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private GameObject startVFX;
    [SerializeField] private GameObject endVFX;
    [SerializeField] private bool followMouse = true; // 补充的变量，用于控制是否跟随鼠标

    // 补充的变量，用于控制激光颜色和强度
    [SerializeField] private Color color = Color.white;
    [SerializeField] private float colorIntensity = 1f;
    [SerializeField] private float beamColorEnhance = 2f;

    // 补充的变量，定义激光的默认起点和终点（当不跟随鼠标时使用）
    [SerializeField] private Vector2 startPosition = Vector2.zero;
    [SerializeField] private Vector2 endPosition = Vector2.zero;

    private const float MAX_LENGTH = 1000;
    private const float OFFSET = 1f;
    [SerializeField] private string[] layerMasks;
    private LayerMask layerMask;

    private void Awake()
    {
        // 设置LineRenderer材质属性
        lineRenderer.material.color = color * colorIntensity;
        lineRenderer.material.SetFloat("_LaserThickness", thickness);
        lineRenderer.material.SetFloat("_LaserScale", noiseScale);

        // 设置子物体粒子系统的颜色
        ParticleSystem[] particles = transform.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem p in particles)
        {
            Renderer r = p.GetComponent<Renderer>();
            r.material.SetColor("_EmissionColor", color * (colorIntensity + beamColorEnhance));
        }

        // 根据字符串数组生成LayerMask
        foreach (string layer in layerMasks)
        {
            layerMask |= LayerMask.GetMask(layer);
        }
    }

    private void Start()
    {
        UpdateLaserPosition(startPosition, endPosition, 0);
    }

    private void Update()
    {
        if (followMouse)
            UpdateLaserPosition(startPosition, Utils.GetMousePosition(), 0); // 假设Utils.GetMousePosition()存在
    }

    public void UpdateLaserPosition(Vector2 startPos, Vector2 endPos, float nouse)
    {
        Vector2 direction = (endPos - startPos).normalized;
        float rotationZ = Mathf.Atan2(direction.y, direction.x); // 计算弧度

        // 设置起始视觉特效的位置和旋转
        startVFX.transform.SetPositionAndRotation(startPos, Quaternion.Euler(0, 0, rotationZ * Mathf.Rad2Deg));

        int i = 0;
        Vector2 currentPosition = startPos;

        // 初始化LineRenderer，从起点开始
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(i, currentPosition);

        // 第一次射线检测
        RaycastHit2D hit = Physics2D.Raycast(currentPosition, direction, MAX_LENGTH, layerMask);

        // 当射线碰到物体且反射次数小于5次时，进行反射
        while (hit.collider != null && i < 5)
        {
            currentPosition = hit.point;
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(++i, currentPosition);

            // 计算反射方向
            direction = Vector2.Reflect(direction, hit.normal);
            // 将当前位置稍微向反射方向移动一点，避免与碰撞体重复碰撞
            currentPosition = currentPosition + OFFSET * direction;
            // 进行下一次射线检测
            hit = Physics2D.Raycast(currentPosition, direction, MAX_LENGTH, layerMask);
        }

        // 处理最后一次（未碰撞或达到最大反射次数）的线段终点
        currentPosition = currentPosition + MAX_LENGTH * direction;
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(++i, currentPosition);
    }
}