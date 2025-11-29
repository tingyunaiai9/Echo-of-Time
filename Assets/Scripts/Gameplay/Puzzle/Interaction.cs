/* Gameplay/Puzzle/Interaction.cs
 * 交互系统基类，定义游戏内可交互对象的通用行为
 * 处理玩家与场景物体的互动逻辑
 */
using UnityEngine;

/*
 * 交互系统基类，定义可交互对象的通用行为
 */
public class Interaction : MonoBehaviour
{
    /* 交互配置类定义 */
    public class InteractionConfig
    {
        // 在此添加交互配置参数，例如 public int someParameter;
    }

    /* 初始化交互配置 */
    public void InitializeInteraction(InteractionConfig config)
    {
        // 设置交互参数
        // 绑定触发条件
        // 初始化状态
    }

    protected virtual void Start()
    {
        // 游戏开始时强制关闭高亮，防止材质球状态残留
        SetHighlight(false);
    }

    /* 处理玩家交互触发 */
    public virtual void OnInteract(PlayerController player)
    {
        // 验证交互条件
        // 执行交互逻辑
        // 反馈交互结果
    }

    /* 验证解谜条件 */
    public bool CheckPuzzleConditions()
    {
        // 检查前置条件
        // 验证物品需求
        // 返回验证结果
        return true;
    }

    /* 缓存渲染器 */
    protected Renderer[] _renderers;
    protected bool _initialized = false;

    protected virtual void InitializeHighlighter()
    {
        if (_initialized) return;
        _renderers = GetComponentsInChildren<Renderer>();
        _initialized = true;
    }

    /* 设置高亮状态 (仅 Shader 描边) */
    public virtual void SetHighlight(bool isActive)
    {
        if (!_initialized) InitializeHighlighter();

        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                // 直接设置 Shader 属性
                if (_renderers[i] != null && _renderers[i].material.HasProperty("_OutlineEnabled"))
                {
                    _renderers[i].material.SetFloat("_OutlineEnabled", isActive ? 1.0f : 0.0f);
                }
            }
        }
    }

    /* 重置交互状态 */
    public void ResetInteraction()
    {
        // 恢复初始状态
        // 清除临时数据
        // 重置动画效果
        gameObject.SetActive(true); // 示例：重置时重新激活对象
    }
}