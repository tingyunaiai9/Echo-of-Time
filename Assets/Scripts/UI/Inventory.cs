/* Gameplay/Player/PlayerInventory.cs
 * 玩家背包/面板基类：提供开关面板、切换栏目、静态添加接口
 */
using System.Collections.Generic;
using UnityEngine;

public class InventoryItem
{
    public string itemId;
    public string itemName;
    public string description;
    public int quantity;
    // 其他物品属性
}

/*
 * 背包 UI 基类：管理面板开关与标签切换
 * 子类：PropBackpack（道具）、CluePanel（线索）
 */
public abstract class Inventory : MonoBehaviour
{
    [Header("Backpack Root (两个子面板的共同父节点)")]
    [SerializeField] protected GameObject backpackRoot;

    // 静态引用：用于集中管理开关与切页
    protected static GameObject s_root;
    protected static PropBackpack s_propPanel;
    protected static CluePanel s_cluePanel;
    protected static bool s_isOpen;

    protected virtual void Awake()
    {
        // 初始化根节点（两个子类的任意一个都会在 Awake 中注入）
        if (backpackRoot != null)
        {
            s_root = backpackRoot;
            if (s_root.activeSelf) s_root.SetActive(false); // 初始关闭
        }

        // 注册具体子面板引用
        if (this is PropBackpack prop) s_propPanel = prop;
        if (this is CluePanel clue) s_cluePanel = clue;
    }

    // 在场景加载后预热，避免第一次按 B 时卡顿
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RuntimeInit()
    {
        var t0 = Time.realtimeSinceStartup;
        EnsureRoot();
        if (s_root != null)
        {
            Debug.Log($"Inventory: warmup 完成，用时 {(Time.realtimeSinceStartup - t0) * 1000f:F1} ms");
        }
    }

    // 开关背包（由 PlayerController B 键调用）
    public static void ToggleBackpack()
    {
        EnsureRoot();
        if (s_root == null)
        {
            Debug.LogWarning("Inventory.ToggleBackpack: s_root 仍未初始化，检查是否已在任一面板组件上设置 backpackRoot，且该对象在场景中（即使未激活也可）。");
            return;
        }

        s_isOpen = !s_isOpen;
        s_root.SetActive(s_isOpen);

        // 打开时默认显示道具页
        if (s_isOpen) SwitchToProps();
    }

    public static void OpenBackpack()
    {
        EnsureRoot();
        if (s_root == null) return;
        s_isOpen = true;
        s_root.SetActive(true);
        SwitchToProps();
    }

    public static void CloseBackpack()
    {
        if (s_root == null) return;
        s_isOpen = false;
        s_root.SetActive(false);
    }

    public static void SwitchToProps()
    {
        if (s_propPanel != null) s_propPanel.gameObject.SetActive(true);
        if (s_cluePanel != null) s_cluePanel.gameObject.SetActive(false);
    }

    public static void SwitchToClues()
    {
        if (s_propPanel != null) s_propPanel.gameObject.SetActive(false);
        if (s_cluePanel != null) s_cluePanel.gameObject.SetActive(true);
    }

    // 静态添加入口：供事件回调调用
    public static void AddPropItem(InventoryItem item)
    {
        if (s_propPanel == null) return;
        s_propPanel.AddOrUpdateItem(item);
    }

    public static void AddClue(string clueId, string clueText)
    {
        if (s_cluePanel == null) return;
        s_cluePanel.AddClue(clueId, clueText);
    }

    // 供 UI Button 绑定的回调
    public void OnClickPropTab() => SwitchToProps();
    public void OnClickClueTab() => SwitchToClues();

    // 若尚未通过 Inspector 注入，尝试在场景中查找一次（包含未激活对象）
    static void EnsureRoot()
    {
        if (s_root != null) return;

        // Unity 2022+ 使用 FindObjectsByType，包含未激活
#if UNITY_2023_1_OR_NEWER
        var all = Object.FindObjectsByType<Inventory>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        // 兼容老版本：包含未激活与资源里的对象，需要过滤场景物体
        var all = Resources.FindObjectsOfTypeAll<Inventory>();
#endif
        foreach (var inv in all)
        {
            if (inv == null || inv.backpackRoot == null) continue;

            // 仅接受场景对象（排除 Project 资源）
            if (!inv.gameObject.scene.IsValid()) continue;

            s_root = inv.backpackRoot;

            if (inv is PropBackpack prop) s_propPanel = prop;
            if (inv is CluePanel clue) s_cluePanel = clue;

            if (s_root.activeSelf) s_root.SetActive(false); // 确保初始关闭
            break;
        }

        if (s_root == null)
        {
            Debug.LogWarning("Inventory: 未能定位背包根节点，请在任一子面板脚本上设置 backpackRoot（允许未激活），并确保该对象在当前场景中。");
        }
    }
}