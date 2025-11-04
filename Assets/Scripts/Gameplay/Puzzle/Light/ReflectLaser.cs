using UnityEngine;
using UnityEngine.UI;
using Events;

public class RayReflectionSystem : MonoBehaviour
{
    public LineRenderer rayLine;
    public int maxReflections = 5;
    public LayerMask mirrorLayer;

    void Update()
    {
        DrawRayWithReflections(transform.position, transform.right, maxReflections);
    }

    void DrawRayWithReflections(Vector2 origin, Vector2 direction, int reflectionsLeft)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, Mathf.Infinity, mirrorLayer);

        if (hit.collider != null)
        {
            // 计算反射方向
            Vector2 reflectDir = Vector2.Reflect(direction, hit.normal);

            // 绘制当前线段
            rayLine.positionCount = maxReflections - reflectionsLeft + 2;
            rayLine.SetPosition(maxReflections - reflectionsLeft, origin);
            rayLine.SetPosition(maxReflections - reflectionsLeft + 1, hit.point);

            // 递归反射
            if (reflectionsLeft > 0)
            {
                DrawRayWithReflections(hit.point + reflectDir * 0.01f, reflectDir, reflectionsLeft - 1);
            }
        }
    }
}