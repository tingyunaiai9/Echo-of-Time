/* UI/Diary/ClueBoard.cs
 * 线索便签墙控制脚本
 * 负责线索条目的添加、布局以及事件驱动的显示更新
 */
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Events;

/*
 * 线索便签墙控制组件
 * 通过 EventBus 接收线索更新事件并在界面上生成便签
 */
public class ClueBoard : MonoBehaviour
{
    [Tooltip("便签预制体")]
    public GameObject Note;

    [Tooltip("便签容器")]
    public Transform contentParent;

    private static ClueBoard s_instance;
    
    // 便签位置数组（循环使用）
    private static readonly Vector2[] notePositions = new Vector2[]
    {
        new Vector2(-400f, 250f),
        new Vector2(-200f, 0f),
        new Vector2(-400f, -250f),
        new Vector2(200f, 250f),
        new Vector2(400f, 0f),
        new Vector2(200f, -250f)
    };
    
    // 当前位置索引
    private int currentPositionIndex = 0;

    void Awake()
    {
        s_instance = this;
        EventBus.Subscribe<ClueUpdatedEvent>(OnClueUpdated);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<ClueUpdatedEvent>(OnClueUpdated);
    }

    /* 线索更新事件回调 */
    void OnClueUpdated(ClueUpdatedEvent e)
    {
        AddClueEntry(e.date, e.content, publish: false);
    }

    /* 添加新的线索条目 */
    public static void AddClueEntry(string date, string content, bool publish = true)
    {
        if (s_instance == null) return;
        s_instance.CreateClueEntry(date, content);
        if (publish)
        {
            EventBus.Publish(new ClueUpdatedEvent { date = date, content = content });
        }
    }
    
    /* 创建单个线索条目 */
    private void CreateClueEntry(string date, string content)
    {
        if (contentParent == null || Note == null) return;
        
        GameObject newNote = Instantiate(Note, contentParent);
        
        // 设置便签位置
        RectTransform rectTransform = newNote.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 使用当前位置索引获取位置
            rectTransform.anchoredPosition = notePositions[currentPositionIndex];
            
            // 更新索引，循环使用位置
            currentPositionIndex = (currentPositionIndex + 1) % notePositions.Length;
        }
    
        // 设置日期文本
        Transform dateTextTransform = newNote.transform.Find("DateText");
        if (dateTextTransform != null)
        {
            TMP_Text dateText = dateTextTransform.GetComponent<TMP_Text>();
            dateText.text = date;
        }

        // 设置内容文本
        Transform contentTextTransform = newNote.transform.Find("ContentText");
        if (contentTextTransform != null)
        {
            TMP_Text contentText = contentTextTransform.GetComponent<TMP_Text>();
            contentText.text = content;
        }
    }
}

/* 线索条目数据结构 */
[System.Serializable]
public class ClueEntryData
{
    public string date;
    public string content;

    public ClueEntryData(string date, string content)
    {
        this.date = date;
        this.content = content;
    }
}