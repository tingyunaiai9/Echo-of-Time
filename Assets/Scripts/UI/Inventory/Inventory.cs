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
    public Sprite image;
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

    [Header("详情侧边栏")]
    [Tooltip("DetailSideBar GameObject，用于显示物品详细信息")]
    [SerializeField] protected GameObject detailSideBar;

    [Tooltip("侧边栏中显示图标的 Image 组件")]
    [SerializeField] protected Image detailIcon;

    [Tooltip("侧边栏中显示名称的 Text 组件")]
    [SerializeField] protected TextMeshProUGUI detailName;

    [Tooltip("侧边栏中显示描述的 Text 组件")]
    [SerializeField] protected TextMeshProUGUI detailDescription;

    // 静态引用
    protected static GameObject s_root;
    protected static PropBackpack s_propPanel;
    protected static CluePanel s_cluePanel;
    protected static bool s_isOpen;
    protected static bool s_initialized = false;

    // 存储 UI 条目的字典（itemId -> GameObject）
    protected Dictionary<string, GameObject> itemEntries = new Dictionary<string, GameObject>();

    // 存储物品数据的字典（itemId -> InventoryItem）
    protected Dictionary<string, InventoryItem> itemData = new Dictionary<string, InventoryItem>();

    /* 初始化：设置静态引用 */
    protected virtual void Awake()
    {
        
        // 记录静态引用
        if (this is PropBackpack prop) s_propPanel = prop;
        if (this is CluePanel clue) s_cluePanel = clue;

        if (backpackRoot != null)
        {
            // 如果是新的根（切场后新 UI 实例），重绑并同步为“关闭”
            if (s_root != backpackRoot || s_root == null)
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

        // 保存物品数据
        itemData[item.itemId] = item;

        GameObject entry;
        if (itemEntries.TryGetValue(item.itemId, out entry))
        {
            // Item 已存在，更新数量等信息
            Debug.Log($"[{GetType().Name}.CreateOrUpdateItemUI] 更新现有物品条目: {item.itemId}");
            UpdateEntryUI(entry, item);
        }
        else
        {
            // Item不存在，创建新条目
            entry = Instantiate(itemEntryPrefab, itemListContainer);
            entry.name = $"Item_{item.itemId}";
            itemEntries[item.itemId] = entry;

            // 添加点击事件
            var button = entry.GetComponent<Button>();
            if (button == null)
            {
                button = entry.AddComponent<Button>();
            }
            
            // 使用局部变量捕获 itemId，避免闭包问题
            string capturedItemId = item.itemId;
            button.onClick.AddListener(() => OnItemClicked(capturedItemId));

            UpdateEntryUI(entry, item);
        }
    }

    /* 点击物品/线索时调用 */
    protected virtual void OnItemClicked(string itemId)
    {
        if (!itemData.TryGetValue(itemId, out var item))
        {
            Debug.LogWarning($"[{GetType().Name}.OnItemClicked] 未找到物品数据: {itemId}");
            return;
        }

        Debug.Log($"[{GetType().Name}.OnItemClicked] 点击了物品: {item.itemId}");
        ShowDetail(item);
    }

    /* 在右侧详情栏显示物品信息 */
    protected void ShowDetail(InventoryItem item)
    {
        if (detailSideBar == null)
        {
            Debug.LogWarning($"[{GetType().Name}.ShowDetail] DetailSideBar 未设置");
            return;
        }

        Debug.Log($"[{GetType().Name}.ShowDetail] 显示物品详情: {item.description}");
        // 显示侧边栏
        detailSideBar.SetActive(true);

        // 设置图标
        if (detailIcon != null)
        {
            if (item.icon != null)
            {
                detailIcon.color = Color.white;  // 确保图标不透明
                detailIcon.sprite = item.icon;
                detailIcon.preserveAspect = true; // ← 新增：保持图片原比例
                detailIcon.enabled = true;
            }
            else
            {
                detailIcon.enabled = false;
            }
        }

        // 设置名称
        if (detailName != null)
        {
            detailName.text = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.itemId;
        }

        // 设置描述
        if (detailDescription != null)
        {
            detailDescription.text = string.IsNullOrEmpty(item.description) 
                ? "暂无描述..." 
                : item.description;
        }

        Debug.Log($"[{GetType().Name}.ShowDetail] 已显示详情: {item.itemId}");
    }

    /* 隐藏详情栏 */
    public void HideDetail()
    {
        if (detailSideBar != null)
        {
            detailSideBar.SetActive(false);
        }
    }

    /* 更新条目 UI */
    protected virtual void UpdateEntryUI(GameObject entry, InventoryItem item)
    {
        // 查找 Icon
        Transform iconTransform = entry.transform.Find("Icon");
        if (iconTransform != null)
        {
            var iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                // 检查 Sprite 是否存在且未被销毁
                if (item.icon != null)
                {
                    iconImage.sprite = item.icon;
                    iconImage.preserveAspect = true; // ← 新增：保持图片原比例
                    iconImage.enabled = true;
                }
                else
                {
                    // 如果 Sprite 已销毁或为空，隐藏 Image 以免显示白色方块
                    iconImage.enabled = false;
                    iconImage.sprite = null;
                }
            }
        }

        // 查找 Quantity（显示在右下角）
        Transform quantityTransform = entry.transform.Find("Quantity");
        if (quantityTransform != null)
        {
            var quantityText = quantityTransform.GetComponent<TextMeshProUGUI>();
            
            if (quantityText != null)
            {
                // 只有数量大于1时才显示计数，否则隐藏
                bool showCount = item.quantity > 1;
                quantityText.text = showCount ? item.quantity.ToString() : "";
                quantityText.enabled = showCount;
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
        itemData.Clear();
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
        s_cluePanel.AddClue(clueId, clueText, "", icon);
    }

    /* 静态接口：检查是否拥有某物品 */
    public static bool HasPropItem(string itemId)
    {
        if (s_propPanel == null) return false;
        return s_propPanel.itemData.ContainsKey(itemId);
    }

    /* 按钮回调 */
    public void OnClickPropTab() => SwitchToProps();
    public void OnClickClueTab() => SwitchToClues();


    /* 退出按钮点击回调 */
    public void OnClickExit()
    {
        if (s_root != null)
        {
            s_root.SetActive(false);
            s_isOpen = false;

            // 发送事件解冻玩家
            EventBus.LocalPublish(new FreezeEvent { isOpen = false });

            Debug.Log("[Inventory] 点击退出按钮，关闭背包");
        }
    }

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