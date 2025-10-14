/* Gameplay/Combat/Enemy.cs
 * 敌人基类，定义敌人行为、AI逻辑、状态管理等
 * 包含敌人特有的属性和战斗能力
 */

using UnityEngine;

public class EnemyConfig
{
    public string enemyName;
    public int health;
    public float moveSpeed;
    public float detectionRange;
    // 其他敌人属性
}

/*
 * 敌人基类，定义敌人行为和AI逻辑
 */
public class Enemy : MonoBehaviour
{
    /* 初始化敌人配置 */
    public void InitializeEnemy(EnemyConfig config)
    {
        // 加载敌人属性
        // 设置AI行为树
        // 初始化巡逻路径
    }

    /* 执行AI决策 */
    public void ExecuteAIBehavior()
    {
        // 感知玩家位置
        // 选择行为策略
        // 执行对应动作
    }

    /* 处理玩家交互 */
    public void OnPlayerInteraction(PlayerController player)
    {
        // 检测交互条件
        // 触发对话或战斗
        // 更新关系状态
    }

    /* 敌人死亡处理 */
    public void OnDeath()
    {
        // 播放死亡动画
        // 掉落物品奖励
        // 触发相关事件
    }
}