/* UI/HUDController.cs
 * 平视显示器控制器，管理游戏主界面的状态显示
 * 包括血量、能量、任务提示等实时信息
 */

/*
 * 游戏HUD控制器，管理抬头显示信息
 */
public class HUDController : MonoBehaviour
{
    /* 更新玩家状态显示 */
    public void UpdatePlayerStatus(PlayerStatus status)
    {
        // 更新生命值/能量条
        // 显示当前时间线
        // 刷新玩家标识
    }

    /* 显示解谜提示信息 */
    public void ShowPuzzleHint(string hintText)
    {
        // 解析提示内容
        // 显示提示面板
        // 设置自动隐藏计时
    }

    /* 管理跨时间线通信显示 */
    public void DisplayCrossTimelineMessage(string message, int sourceTimeline)
    {
        // 格式化消息显示
        // 应用时间线颜色编码
        // 播放接收动画效果
    }

    /* 处理紧急事件警告 */
    public void ShowEmergencyAlert(AlertType alertType)
    {
        // 根据类型选择警告样式
        // 播放警告动画
        // 触发声音反馈
    }
}