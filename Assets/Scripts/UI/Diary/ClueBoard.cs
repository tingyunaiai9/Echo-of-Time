/* UI/Diary/ClueBoard.cs
 * 线索便签墙控制脚本
 * 负责线索条目的添加、布局以及事件驱动的显示更新
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using Events;
using Unity.VisualScripting;

/*
 * 线索便签墙控制组件
 * 通过 EventBus 接收线索更新事件并在界面上生成便签
 */
public class ClueBoard : MonoBehaviour
{
    [Tooltip("图片便签预制体")]
    public GameObject Note_Image;
    [Tooltip("文字便签预制体")]
    public GameObject Note_Text;

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
        EventBus.Subscribe<ClueSharedEvent>(OnClueUpdated);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<ClueSharedEvent>(OnClueUpdated);
    }

    /* 线索更新事件回调 */
    void OnClueUpdated(ClueSharedEvent e)
    {
        switch (e.ClueType)
        {
            case SharedClueType.Image:
                AddClueEntry(e.timeline, e.imageData, SharedClueType.Image, false);
                break;
            case SharedClueType.Text:
                AddClueEntry(e.timeline, e.textData ?? string.Empty, SharedClueType.Text, false);
                break;
            default:
                Debug.LogWarning("[ClueBoard] 收到未知类型的 ClueSharedEvent");
                break;
        }

        Debug.Log("[ClueBoard] 收到线索共享事件，已添加新线索条目");
    }

    // 兼容旧调用：默认图片类型
    public static void AddClueEntry(int timeline, byte[] imageBytes, bool publish = true)
    {
        AddClueEntry(timeline, imageBytes, SharedClueType.Image, publish);
    }

    // 图片/文本统一入口（byte[] 版本）
    public static void AddClueEntry(int timeline, byte[] data, SharedClueType clueType, bool publish = true)
    {
        if (!EnsureInstance()) return;

        switch (clueType)
        {
            case SharedClueType.Image:
                s_instance.CreateClueEntry(timeline, data);
                if (publish)
                {
                    // 使用 ImageNetworkSender 分块发送大图，避免 Mirror 消息过大
                    if (ImageNetworkSender.LocalInstance != null)
                    {
                        ImageNetworkSender.LocalInstance.SendImage(data, timeline, "Clue");
                    }
                    else
                    {
                        EventBus.Publish(new ClueSharedEvent
                        {
                            timeline = timeline,
                            imageData = data,
                            ClueType = SharedClueType.Image
                        });
                    }
                }
                break;

            case SharedClueType.Text:
                string textPayload = data != null ? Encoding.UTF8.GetString(data) : string.Empty;
                s_instance.CreateClueEntry(timeline, textPayload);
                if (publish)
                {
                    EventBus.Publish(new ClueSharedEvent
                    {
                        timeline = timeline,
                        textData = textPayload,
                        ClueType = SharedClueType.Text
                    });
                }
                break;

            default:
                Debug.LogWarning("[ClueBoard] AddClueEntry 收到未知线索类型");
                break;
        }
    }

    // 文本版本便捷入口
    public static void AddClueEntry(int timeline, string text, bool publish = true)
    {
        AddClueEntry(timeline, text, SharedClueType.Text, publish);
    }

    public static void AddClueEntry(int timeline, string text, SharedClueType clueType, bool publish = true)
    {
        if (!EnsureInstance()) return;

        s_instance.CreateClueEntry(timeline, text);
        if (publish)
        {
            EventBus.Publish(new ClueSharedEvent
            {
                timeline = timeline,
                textData = text,
                ClueType = clueType
            });
        }
    }

    // 确保实例存在（包含未激活对象）
    private static bool EnsureInstance()
    {
        if (s_instance != null) return true;

        ClueBoard[] allBoards = Resources.FindObjectsOfTypeAll<ClueBoard>();
        foreach (var board in allBoards)
        {
            // 排除预制体，只查找场景中的对象
            if (board.gameObject.scene.isLoaded)
            {
                s_instance = board;
                Debug.Log("[ClueBoard] 懒加载 ClueBoard 实例成功（包括非激活对象）");
                break;
            }
        }

        if (s_instance == null)
        {
            Debug.LogWarning("[ClueBoard] AddClueEntry 调用时未找到 ClueBoard 实例，条目未创建。");
            return false;
        }

        return true;
    }
    
    /* 创建单个线索条目 - 图片 */
    private void CreateClueEntry(int timeline, byte[] imageBytes)
    {
        if (contentParent == null || Note_Image == null) return;
        
        GameObject newNote = Instantiate(Note_Image, contentParent);
        ApplyCommonLayout(newNote, timeline);

        // 处理共享图片
        if (imageBytes != null && imageBytes.Length > 0)
        {
            Transform imageTransform = newNote.transform.Find("Image");
            if (imageTransform != null)
            {
                Image image = imageTransform.GetComponent<Image>();
                if (image != null)
                {
                    // 将 byte[] 转换为 Texture2D
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(imageBytes))
                    {
                        // 从 Texture2D 创建 Sprite
                        Sprite sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );
                        
                        image.sprite = sprite;
                        
                        // 设置宽度为 280，高度根据原始比例计算
                        float targetWidth = 280f;
                        float aspectRatio = (float)texture.height / texture.width;
                        float targetHeight = targetWidth * aspectRatio;
                        if (targetHeight > 200f)
                        {
                            targetHeight = 200f;
                            targetWidth = targetHeight / aspectRatio;
                        }
                        Debug.Log($"[ClueBoard]设置图片高度为{targetHeight}，宽度为{targetWidth}以适应");
                        RectTransform imageRect = imageTransform.GetComponent<RectTransform>();
                        if (imageRect != null)
                        {
                            imageRect.sizeDelta = new Vector2(targetWidth, targetHeight);
                        }
                    }
                    else
                    {
                        Debug.LogError("[ClueBoard] 无法从字节数组加载图片");
                    }
                }
            }
        }
    }

    /* 创建单个线索条目 - 文本 */
    private void CreateClueEntry(int timeline, string textContent)
    {
        if (contentParent == null || Note_Text == null) return;

        GameObject newNote = Instantiate(Note_Text, contentParent);
        ApplyCommonLayout(newNote, timeline);

        // 设置正文文本（找到第一个非 DateText 的 TMP_Text）
        TMP_Text[] texts = newNote.GetComponentsInChildren<TMP_Text>();
        foreach (var tmp in texts)
        {
            if (tmp.name != "DateText")
            {
                tmp.text = textContent;
                break;
            }
        }
    }

    // 公共布局逻辑：位置与日期标签
    private void ApplyCommonLayout(GameObject noteGO, int timeline)
    {
        // 设置便签位置
        RectTransform rectTransform = noteGO.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = notePositions[currentPositionIndex];
            currentPositionIndex = (currentPositionIndex + 1) % notePositions.Length;
        }

        // 设置日期文本
        Transform dateTextTransform = noteGO.transform.Find("DateText");
        if (dateTextTransform != null)
        {
            TMP_Text dateTextComponent = dateTextTransform.GetComponent<TMP_Text>();
            if (dateTextComponent != null)
            {
                switch (timeline)
                {
                    case 0:
                        dateTextComponent.text = "Ancient";
                        break;
                    case 1:
                        dateTextComponent.text = "Modern";
                        break;
                    case 2:
                        dateTextComponent.text = "Future";
                        break;
                    default:
                        dateTextComponent.text = "Unknown";
                        break;
                }
            }
        }
    }

    public static void Reset()
    {
        if (s_instance != null)
        {
            // 删除所有子对象
            foreach (Transform child in s_instance.contentParent)
            {
                GameObject.Destroy(child.gameObject);
            }
            // 重置位置索引
            s_instance.currentPositionIndex = 0;
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