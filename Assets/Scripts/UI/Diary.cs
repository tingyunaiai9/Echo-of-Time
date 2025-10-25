using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Events;
using UnityEditor.EditorTools;

/*
 控制日记页面的显示与隐藏，并通过事件禁用玩家移动
 挂载于 Canvas/DiaryPanel/DiaryContent
*/
public class Diary : MonoBehaviour
{
    [Tooltip("日记条目预制体（包含日期和内容文本）")]
    public GameObject diaryEntryPrefab;

    [Tooltip("日记条目容器（Vertical Layout Group）")]
    public Transform contentParent;

    [Tooltip("日记面板根对象（用于显示/隐藏）")]
    public GameObject panelRoot;

    // 静态引用和状态
    private static GameObject s_root;
    private static Diary s_instance;
    private static bool s_isOpen;

    protected virtual void Awake()
    {
        Debug.Log("[Diary] Awake 执行");
        if (panelRoot == null)
            panelRoot = gameObject;
        s_root = panelRoot;
        s_instance = this;

        // 确保内容容器被正确引用
        if (contentParent == null)
        {
            // 根据你的层级结构自动查找
            contentParent = transform.Find("DiaryScrollView/Viewport/Content")?.GetComponent<Transform>();
        }

        CloseDiary();
    }

    // 静态切换方法
    public static void ToggleDiary()
    {
        Debug.Log("[Diary] ToggleDiary 调用");
        if (s_isOpen)
            CloseDiary();
        else
            OpenDiary();
    }

    public static void OpenDiary()
    {
        Debug.Log("[Diary] OpenDiary 调用");
        if (s_root == null || s_instance == null) return;
        s_isOpen = true;
        s_root.SetActive(true);

        // 禁用玩家移动
        EventBus.Instance.LocalPublish(new FreezeEvent { isOpen = true });

        // 清空现有条目并重新生成
        s_instance.ClearDiaryEntries();
        TestDiaryEntries();
    }

    public static void CloseDiary()
    {
        if (s_root == null) return;
        s_isOpen = false;
        s_root.SetActive(false);

        // 恢复玩家移动
        EventBus.Instance.LocalPublish(new FreezeEvent { isOpen = false });
    }

    // 添加新的日记条目
    public static void AddDiaryEntry(string content)
    {
        if (s_instance == null) return;
        s_instance.CreateDiaryEntry(System.DateTime.Now, content);
    }

    // 清空所有日记条目
    private void ClearDiaryEntries()
    {
        if (contentParent == null) return;

        // 删除所有子对象（保留前几个示例条目如果需要）
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }

    // 生成日记条目（示例数据）
    public static void TestDiaryEntries()
    {
        if (s_instance == null) return;
        // 示例数据 - 在实际项目中可以从存档或数据库读取
        s_instance.CreateDiaryEntry(System.DateTime.Now.AddDays(-2), "发现了时间回声的秘密，这个世界比想象中更加复杂。");
        s_instance.CreateDiaryEntry(System.DateTime.Now.AddDays(-1), "遇到了神秘的NPC，他告诉我关于时空裂缝的传说。");
        s_instance.CreateDiaryEntry(System.DateTime.Now, "今天是个新的开始，继续我的冒险旅程！");
    }
    
    
    // 创建单个日记条目
    private void CreateDiaryEntry(System.DateTime date, string content)
    {
        if (contentParent == null) return;

        // 使用 Inspector 赋值的预制体
        if (diaryEntryPrefab == null)
        {
            Debug.LogWarning("[Diary] diaryEntryPrefab 未赋值！");
            return;
        }

        GameObject newEntry = Instantiate(diaryEntryPrefab, contentParent);

        // 设置日期文本
        Transform dateTextTransform = newEntry.transform.Find("DateText");
        if (dateTextTransform != null)
        {
            TMP_Text dateText = dateTextTransform.GetComponent<TMP_Text>();
            if (dateText != null)
            {
                dateText.text = date.ToString("yyyy/MM/dd");
            }
        }

        // 设置内容文本
        Transform contentTextTransform = newEntry.transform.Find("ContentText");
        if (contentTextTransform != null)
        {
            TMP_Text contentText = contentTextTransform.GetComponent<TMP_Text>();
            if (contentText != null)
            {
                contentText.text = content;
            }
        }

        newEntry.transform.SetAsFirstSibling();
    }
    
    // 批量添加日记条目（可选方法）
    public static void AddDiaryEntries(System.Collections.Generic.List<DiaryEntryData> entries)
    {
        if (s_instance == null || entries == null) return;

        foreach (var entryData in entries)
        {
            s_instance.CreateDiaryEntry(entryData.date, entryData.content);
        }
    }
}

// 日记条目数据结构（可选，用于更好的数据管理）
[System.Serializable]
public class DiaryEntryData
{
    public System.DateTime date;
    public string content;

    public DiaryEntryData(System.DateTime date, string content)
    {
        this.date = date;
        this.content = content;
    }
}