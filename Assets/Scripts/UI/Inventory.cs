/* Gameplay/Player/PlayerInventory.cs
 * 玩家背包/面板基类：提供开关面板、切换栏目、静态添加接口
 */
using System.Collections.Generic;
using UnityEngine;
using Events;

public class InventoryItem
{
    public string itemId;
    public string itemName;
    public string description;
    public int quantity;
}

/*
 * 背包 UI 基类：管理面板开关与标签切换
 * 子类：PropBackpack（道具）、CluePanel（线索）
 */
public abstract class Inventory : MonoBehaviour
{
    [Header("Backpack Root (两个子面板的共同父节点)")]
    [SerializeField] protected GameObject backpackRoot;

    protected static GameObject s_root;
    protected static PropBackpack s_propPanel;
    protected static CluePanel s_cluePanel;
    protected static bool s_isOpen;

    protected virtual void Awake()
    {
        // 只要有一个背包面板 Awake，就初始化静态根节点和面板引用
        if (backpackRoot != null && s_root == null)
        {
            s_root = backpackRoot;
            if (s_root.activeSelf) s_root.SetActive(false);
        }
        if (this is PropBackpack prop && s_propPanel == null) s_propPanel = prop;
        if (this is CluePanel clue && s_cluePanel == null) s_cluePanel = clue;
    }

    // 开关背包
    public static void ToggleBackpack()
    {
        if (s_root == null)
        {
            Debug.LogWarning("Inventory: 背包根节点未初始化，请确保场景中有激活的背包面板对象。");
            return;
        }
        s_isOpen = !s_isOpen;
        s_root.SetActive(s_isOpen);
        if (s_isOpen) SwitchToProps();

        // 使用 EventBus 发布事件（替代原有的事件触发）
        EventBus.Instance.Publish(new BackpackStateChangedEvent { isOpen = s_isOpen });
    }

    public static void OpenBackpack()
    {
        if (s_root == null) return;
        s_isOpen = true;
        s_root.SetActive(true);
        SwitchToProps();
        
        EventBus.Instance.Publish(new BackpackStateChangedEvent { isOpen = true });
    }

    public static void CloseBackpack()
    {
        if (s_root == null) return;
        s_isOpen = false;
        s_root.SetActive(false);

        EventBus.Instance.Publish(new BackpackStateChangedEvent { isOpen = false });
    }

    // 切换背包栏目
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

    // 静态接口：添加物品和线索
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

    // 按钮回调
    public void OnClickPropTab() => SwitchToProps();
    public void OnClickClueTab() => SwitchToClues();
}