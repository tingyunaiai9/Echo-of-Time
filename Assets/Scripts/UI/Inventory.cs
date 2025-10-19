/* Gameplay/Player/PlayerInventory.cs
 * 玩家背包/面板基类：提供开关面板、切换栏目、静态添加接口
 */
using UnityEngine;

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

    // 静态引用
    protected static GameObject s_root;
    protected static PropBackpack s_propPanel;
    protected static CluePanel s_cluePanel;
    protected static bool s_isOpen;

    protected virtual void Awake()
    {
        Debug.Log($"[Awake] {GetType().Name} - root:{(backpackRoot ? backpackRoot.name : "null")}");
        
        if (backpackRoot != null)
        {
            // 仅在首次设置 s_root 时初始化为关闭状态
            if (s_root == null)
            {
                s_root = backpackRoot;
                if (s_root.activeSelf) s_root.SetActive(false); // 仅首次关闭
                Debug.Log($"[Awake] 首次设置 s_root 为: {s_root.name}，初始关闭");
            }
        }
        if (this is PropBackpack prop) s_propPanel = prop;
        if (this is CluePanel clue) s_cluePanel = clue;
    }

    public static void ToggleBackpack()
    {
        Debug.Log($"[Toggle] 开始 - root:{(s_root ? s_root.name : "null")} prop:{(s_propPanel ? "√" : "X")} clue:{(s_cluePanel ? "√" : "X")} isOpen:{s_isOpen}");
        
        EnsureRoot();
        if (s_root == null) { Debug.LogError("[Toggle] root仍为null"); return; }

        // 详细检查父节点状态
        Transform checkParent = s_root.transform.parent;
        Debug.Log($"[Toggle] root的父节点: {(checkParent ? checkParent.name : "null")}");
        while (checkParent != null)
        {
            Debug.Log($"[Toggle] 检查父节点 {checkParent.name} - activeSelf:{checkParent.gameObject.activeSelf}, activeInHierarchy:{checkParent.gameObject.activeInHierarchy}");
            if (!checkParent.gameObject.activeSelf)
            {
                Debug.LogError($"[Toggle] 父节点 {checkParent.name} 的 activeSelf=false！");
            }
            if (!checkParent.gameObject.activeInHierarchy)
            {
                Debug.LogError($"[Toggle] 父节点 {checkParent.name} 的 activeInHierarchy=false！");
            }
            checkParent = checkParent.parent;
        }

        // 打印调用前的状态
        Debug.Log($"[Toggle] 调用前 - root.activeSelf:{s_root.activeSelf}, root.activeInHierarchy:{s_root.activeInHierarchy}");
        
        s_isOpen = !s_isOpen;
        s_root.SetActive(s_isOpen);
        
        // 打印调用后的状态
        Debug.Log($"[Toggle] 调用 SetActive({s_isOpen}) 后 - root.activeSelf:{s_root.activeSelf}, root.activeInHierarchy:{s_root.activeInHierarchy}");

        if (s_isOpen)
        {
            EnsurePanelsFromRoot();
            Debug.Log($"[Toggle] 查找后 - prop:{(s_propPanel ? s_propPanel.name : "null")} clue:{(s_cluePanel ? s_cluePanel.name : "null")}");
            
            if (s_propPanel != null)
            {
                Debug.Log($"[Toggle] PropPanel状态 - activeSelf:{s_propPanel.gameObject.activeSelf}, activeInHierarchy:{s_propPanel.gameObject.activeInHierarchy}");
                s_propPanel.gameObject.SetActive(true);
                Debug.Log($"[Toggle] PropPanel SetActive(true)后 - activeSelf:{s_propPanel.gameObject.activeSelf}, activeInHierarchy:{s_propPanel.gameObject.activeInHierarchy}");
                
                if (s_cluePanel != null) s_cluePanel.gameObject.SetActive(false);
            }
            else if (s_cluePanel != null)
            {
                Debug.Log($"[Toggle] 激活CluePanel");
                s_cluePanel.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("[Toggle] 两个面板都为null!");
            }
        }
        Debug.Log("[Toggle] 完成");
    }

    public static void OpenBackpack()
    {
        EnsureRoot();
        if (s_root == null) return;
        
        s_isOpen = true;
        s_root.SetActive(true);
        EnsurePanelsFromRoot();
        
        if (s_propPanel != null)
        {
            s_propPanel.gameObject.SetActive(true);
            if (s_cluePanel != null) s_cluePanel.gameObject.SetActive(false);
        }
        else if (s_cluePanel != null)
        {
            s_cluePanel.gameObject.SetActive(true);
        }
    }

    public static void CloseBackpack()
    {
        if (s_root == null) return;
        s_isOpen = false;
        s_root.SetActive(false);
    }

    public static void SwitchToProps()
    {
        EnsureRoot();
        EnsurePanelsFromRoot();
        if (s_propPanel != null) s_propPanel.gameObject.SetActive(true);
        if (s_cluePanel != null) s_cluePanel.gameObject.SetActive(false);
    }

    public static void SwitchToClues()
    {
        EnsureRoot();
        EnsurePanelsFromRoot();
        if (s_propPanel != null) s_propPanel.gameObject.SetActive(false);
        if (s_cluePanel != null) s_cluePanel.gameObject.SetActive(true);
    }

    public static void AddPropItem(InventoryItem item)
    {
        EnsurePanelsFromRoot();
        if (s_propPanel == null) return;
        s_propPanel.AddOrUpdateItem(item);
    }

    public static void AddClue(string clueId, string clueText)
    {
        EnsurePanelsFromRoot();
        if (s_cluePanel == null) return;
        s_cluePanel.AddClue(clueId, clueText);
    }

    // 供 UI Button 绑定
    public void OnClickPropTab() => SwitchToProps();
    public void OnClickClueTab() => SwitchToClues();

    // 仅负责拿到根节点
    static void EnsureRoot()
    {
        if (s_root != null) return;
        Debug.Log("[EnsureRoot] 开始查找...");

#if UNITY_2023_1_OR_NEWER
        var any = Object.FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        if (any != null && any.backpackRoot != null)
            s_root = any.backpackRoot;
#else
        var all = Resources.FindObjectsOfTypeAll<Inventory>();
        foreach (var inv in all)
        {
            if (inv == null) continue;
            if (!inv.gameObject.scene.IsValid()) continue;
            if (inv.backpackRoot != null) { s_root = inv.backpackRoot; break; }
        }
#endif
        Debug.Log($"[EnsureRoot] 结果: {(s_root ? s_root.name : "null")}");
        if (s_root != null && s_root.activeSelf) s_root.SetActive(false);
    }

    // 从根节点层级抓取两个子面板（包含未激活）
    static void EnsurePanelsFromRoot()
    {
        if (s_root == null) return;

        if (s_propPanel == null)
        {
            var p = s_root.GetComponentInChildren<PropBackpack>(true);
            if (p != null) { s_propPanel = p; Debug.Log($"[查找] PropPanel: {p.name}"); }
            else Debug.Log("[查找] PropPanel: 未找到");
        }

        if (s_cluePanel == null)
        {
            var c = s_root.GetComponentInChildren<CluePanel>(true);
            if (c != null) { s_cluePanel = c; Debug.Log($"[查找] CluePanel: {c.name}"); }
            else Debug.Log("[查找] CluePanel: 未找到");
        }
    }
}