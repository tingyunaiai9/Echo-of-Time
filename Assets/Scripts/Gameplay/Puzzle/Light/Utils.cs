/*
 * Utils.cs
 * 光照谜题辅助工具：提供鼠标位置获取等便捷方法。
 */
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Utils 类
 * 提供静态方法，供光照谜题中获取鼠标世界坐标。
 */
public static class Utils
{
    /* 获取鼠标在世界坐标系中的位置
     * 返回值：鼠标的世界坐标（忽略 Z 轴）
     */
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