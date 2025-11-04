using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Events;

/*
 控制线索条目的添加与显示
*/
public class ClueBoard : MonoBehaviour
{
    [Tooltip("线索条目预制体（包含日期和内容文本）")]
    public GameObject clueEntryPrefab;

    [Tooltip("线索条目容器（Vertical Layout Group）")]
    public Transform contentParent;

    private static ClueBoard s_instance;

    void Awake()
    {
        s_instance = this;
        if (contentParent == null)
        {
            contentParent = transform.Find("LeftPanel/ClueScrollView/Viewport/Content");
        }
        EventBus.Subscribe<ClueUpdatedEvent>(OnClueUpdated);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<ClueUpdatedEvent>(OnClueUpdated);
    }

    /* 线索更新事件回调 */
    void OnClueUpdated(ClueUpdatedEvent e)
    {
        AddClueEntry(e.ClueEntry, publish: false);
    }

    /* 添加新的线索条目 */
    public static void AddClueEntry(string content, bool publish = true)
    {
        if (s_instance == null) return;
        s_instance.CreateClueEntry(System.DateTime.Now, content);
        if (publish)
        {
            EventBus.Publish(new ClueUpdatedEvent { ClueEntry = content });
        }
    }

    /* 批量添加线索条目 */
    public static void AddClueEntries(List<ClueEntryData> entries)
    {
        if (s_instance == null || entries == null) return;
        foreach (var entryData in entries)
        {
            s_instance.CreateClueEntry(entryData.date, entryData.content);
        }
    }

    /* 创建单个线索条目 */
    private void CreateClueEntry(System.DateTime date, string content)
    {
        if (contentParent == null || clueEntryPrefab == null) return;
        GameObject newEntry = Instantiate(clueEntryPrefab, contentParent);

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

    /* 添加测试线索条目 */
    [ContextMenu("Test Add Clue Entries")]
    public void TestClueEntries()
    {
        var testEntries = new List<ClueEntryData>
        {
            new ClueEntryData(System.DateTime.Now.AddDays(-2), "发现神秘钥匙"),
            new ClueEntryData(System.DateTime.Now.AddDays(-1), "解锁了地下室门"),
            new ClueEntryData(System.DateTime.Now, "获得新的线索：日记残页")
        };
        AddClueEntries(testEntries);
    }
}

/* 线索条目数据结构 */
[System.Serializable]
public class ClueEntryData
{
    public System.DateTime date;
    public string content;

    public ClueEntryData(System.DateTime date, string content)
    {
        this.date = date;
        this.content = content;
    }
}