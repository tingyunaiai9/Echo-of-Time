/* Gameplay/Player/PlayerInventory.cs
 * 玩家背包/面板基类：提供开关面板、切换栏目、静态添加接口
 */
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Events;
using TMPro;

public class InventoryItem
{
    public string itemId;
    public string itemName;
    public string description;
    public int quantity;
    public Sprite icon;
}

/*
 * 背包 UI 基类：管理面板开关与标签切换
 * 子类：PropBackpack（道具）、CluePanel（线索）
 */
public abstract class Inventory : MonoBehaviour
{
    [Header("Backpack Root (两个子面板的共同父节点)")]
    [SerializeField] protected GameObject backpackRoot;

    [Header("UI 显示设置")]
    [Tooltip("物品条目的预制体，需包含 Image(Icon) 和 Text(Name) 组件")]
    [SerializeField] protected GameObject itemEntryPrefab;

    [Tooltip("物品列表的容器（如 ScrollView 的 Content）")]
    [SerializeField] protected Transform itemListContainer;

    // 静态引用
    protected static GameObject s_root;
    protected static PropBackpack s_propPanel;
    protected static CluePanel s_cluePanel;
    protected static bool s_isOpen;
    protected static bool s_initialized = false;

    // 存储 UI 条目的字典（itemId -> GameObject）
    protected Dictionary<string, GameObject> itemEntries = new Dictionary<string, GameObject>();

    /* 初始化：设置静态引用 */
    protected virtual void Awake()
    {
        
        // 记录静态引用
        if (this is PropBackpack prop) s_propPanel = prop;
        if (this is CluePanel clue) s_cluePanel = clue;

        if (backpackRoot != null)
        {
            // 如果是新的根（切场后新 UI 实例），重绑并同步为“关闭”
            if (s_root != backpackRoot)
            {
                s_root = backpackRoot;
                s_initialized = false;       // 让 Start 有机会走初始化流程
                s_isOpen = false;   // 实物状态 = 关闭

                //EventBus.Instance?.LocalPublish(new FreezeEvent { isOpen = false });
            }
        }
    }

    /* 所有面板初始化完成后关闭根节点 */
    protected virtual void Start()
    {
        // 然后再做你原来“两个子面板就绪后”的初始化标记
        if (!s_initialized && s_root != null && s_propPanel != null && s_cluePanel != null)
        {
            s_initialized = true;
            s_root.SetActive(false);
            Debug.Log($"[{GetType().Name}.Start] 两个面板都已初始化，关闭背包");
        }
    }

    /* 创建或更新 UI 条目 */
    protected void CreateOrUpdateItemUI(InventoryItem item)
    {
        if (itemListContainer == null || itemEntryPrefab == null)
        {
            Debug.LogWarning($"{GetType().Name}: itemListContainer 或 itemEntryPrefab 未设置，无法显示 UI");
            return;
        }

        GameObject entry;
        if (itemEntries.TryGetValue(item.itemId, out entry))
        {
            // Item 已存在，更新数量等信息
            Debug.Log($"[{GetType().Name}.CreateOrUpdateItemUI] 更新现有物品条目: {item.itemName}");
            // 更新现有条目
            UpdateEntryUI(entry, item);
        }
        else
        {
            // Item不存在，创建新条目
            entry = Instantiate(itemEntryPrefab, itemListContainer);
            entry.name = $"Item_{item.itemId}";
            itemEntries[item.itemId] = entry;
            UpdateEntryUI(entry, item);
        }
    }

    /* 更新条目 UI */
    protected virtual void UpdateEntryUI(GameObject entry, InventoryItem item)
    {
        // 严格查找 Icon（只查直接子对象）
        Transform iconTransform = entry.transform.Find("Icon");
        if (iconTransform != null)
        {
            var icon = iconTransform.GetComponent<Image>();
            if (icon != null && item.icon != null)
            {
                icon.sprite = item.icon;
                icon.enabled = true;
                Debug.Log($"[{GetType().Name}.UpdateEntryUI] 图标已设置: {item.icon.name}");
            }
            else if (icon != null)
            {
                icon.enabled = false;
                Debug.Log($"[{GetType().Name}.UpdateEntryUI] 无图标，隐藏 Image");
            }
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}.UpdateEntryUI] 未找到名为 'Icon' 的子对象！");
        }

        // 严格查找 Name（只查直接子对象）
        Transform nameTransform = entry.transform.Find("Name");
        if (nameTransform != null)
        {
            var nameText = nameTransform.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = item.itemName;
                Debug.Log($"[{GetType().Name}.UpdateEntryUI] 名称已设置: {item.itemName}");
            }
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}.UpdateEntryUI] 未找到名为 'Name' 的子对象！");
        }

        // 查找 Quantity（可选）
        Transform quantityTransform = entry.transform.Find("Quantity");
        if (quantityTransform != null)
        {
            var quantityText = quantityTransform.GetComponent<Text>();
            if (quantityText != null && item.quantity > 1)
            {
                quantityText.text = $"x{item.quantity}";
                quantityText.enabled = true;
            }
            else if (quantityText != null)
            {
                quantityText.enabled = false;
            }
        }
    }

    /* 清空所有 UI 条目 */
    protected void ClearAllEntries()
    {
        foreach (var entry in itemEntries.Values)
        {
            if (entry != null) Destroy(entry);
        }
        itemEntries.Clear();
    }

    /* 开关背包 */
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

        // 发布事件
        EventBus.Instance?.LocalPublish(new FreezeEvent { isOpen = s_isOpen });
    }

    /* 切换背包栏目 */
    public static void SwitchToProps()
    {
        if (s_propPanel != null) s_propPanel.gameObject.SetActive(true);
        if (s_cluePanel != null) s_cluePanel.gameObject.SetActive(false);
    }

    /* 切换到线索栏目 */
    public static void SwitchToClues()
    {
        if (s_propPanel != null) s_propPanel.gameObject.SetActive(false);
        if (s_cluePanel != null) s_cluePanel.gameObject.SetActive(true);
    }

    /* 静态接口：添加物品和线索 */
    public static void AddPropItem(InventoryItem item)
    {
        if (s_propPanel == null) return;
        s_propPanel.AddOrUpdateItem(item);
    }

    /* 静态接口：添加线索 */
    public static void AddClue(string clueId, string clueText, Sprite icon = null)
    {
        if (s_cluePanel == null) return;
        s_cluePanel.AddClue(clueId, clueText, icon);
    }

    /* 按钮回调 */
    public void OnClickPropTab() => SwitchToProps();
    public void OnClickClueTab() => SwitchToClues();

    protected virtual void OnDestroy()
    {
        if (backpackRoot != null && s_root == backpackRoot)
        {
            s_root = null;
            s_propPanel = null;
            s_cluePanel = null;
            s_isOpen = false;
            s_initialized = false;
        }
    }

}