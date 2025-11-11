using UnityEngine;
using System.Collections.Generic;

public class LaserBeam : MonoBehaviour
{
    [SerializeField] public float thickness = 5;
    [SerializeField] private float noiseScale = 3.14f;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private GameObject startVFX;
    [SerializeField] private GameObject reflectVFX;
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

    // 存储所有反射点的特效实例
    private List<GameObject> reflectionVFXList = new List<GameObject>();

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
        {
            UpdateLaserPosition(startPosition, Utils.GetMousePosition(), 0); // 假设Utils.GetMousePosition()存在
        }
        else
        {
            // 即使不跟随鼠标，也需要持续更新激光位置以检测碰撞器变化
            UpdateLaserPosition(startPosition, endPosition, 0);
        }
    }

    public void UpdateLaserPosition(Vector2 startPos, Vector2 endPos, float nouse)
    {
        Vector2 direction = (endPos - startPos).normalized;
        float rotationZ = Mathf.Atan2(direction.y, direction.x);
    
        int i = 0;
        int vfxIndex = 0; // 用于追踪当前使用的特效索引
        Vector2 currentPosition = startPos;
    
        // 初始化LineRenderer
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(i, currentPosition);
    
        // 设置起始特效
        startVFX.transform.SetPositionAndRotation(currentPosition, Quaternion.Euler(0, 0, rotationZ * Mathf.Rad2Deg));
    
        // 第一次射线检测
        RaycastHit2D hit = Physics2D.Raycast(currentPosition, direction, MAX_LENGTH, layerMask);
    
        // 反射循环
        while (hit.collider != null && i < 5)
        {
            currentPosition = hit.point;
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(++i, currentPosition);
    
            // 复用或创建反射点特效
            if (reflectVFX != null)
            {
                GameObject vfx;
                if (vfxIndex < reflectionVFXList.Count)
                {
                    // 复用已存在的特效
                    vfx = reflectionVFXList[vfxIndex];
                    vfx.SetActive(true);
                }
                else
                {
                    // 创建新特效
                    vfx = Instantiate(reflectVFX, currentPosition, Quaternion.identity);
                    reflectionVFXList.Add(vfx);
                }
    
                // 更新特效位置和旋转
                float vfxRotation = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg;
                vfx.transform.SetPositionAndRotation(currentPosition, Quaternion.Euler(0, 0, vfxRotation));
                vfxIndex++;
            }
    
            // 计算反射
            direction = Vector2.Reflect(direction, hit.normal);
            currentPosition = currentPosition + OFFSET * direction;
            hit = Physics2D.Raycast(currentPosition, direction, MAX_LENGTH, layerMask);
        }
    
        // 隐藏多余的特效（而不是销毁）
        for (int j = vfxIndex; j < reflectionVFXList.Count; j++)
        {
            if (reflectionVFXList[j] != null)
                reflectionVFXList[j].SetActive(false);
        }
    
        // 处理最后一段
        currentPosition = currentPosition + MAX_LENGTH * direction;
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(++i, currentPosition);
    }

    // 修改清理方法（仅在销毁时调用）
    private void ClearReflectionVFX()
    {
        foreach (GameObject vfx in reflectionVFXList)
        {
            if (vfx != null)
                Destroy(vfx);
        }
        reflectionVFXList.Clear();
    }
    
    private void OnDestroy()
    {
        // 清理所有特效
        ClearReflectionVFX();
    }
}