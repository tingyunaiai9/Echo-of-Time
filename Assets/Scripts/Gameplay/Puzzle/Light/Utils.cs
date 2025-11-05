using UnityEngine;
using UnityEngine.InputSystem;

public static class Utils
{
    /// <summary>
    /// 获取鼠标在世界坐标系中的位置
    /// </summary>
    /// <returns>鼠标的世界坐标位置</returns>
    public static Vector2 GetMousePosition()
    {
        // 使用新的 Input System 获取鼠标在屏幕上的位置
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        
        // 将屏幕坐标转换为世界坐标
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, 0));
        
        // 返回二维坐标（忽略Z轴）
        return new Vector2(mouseWorldPosition.x, mouseWorldPosition.y);
    }
}