/* UI/Diary/ClueBoard.cs
 * 线索便签墙控制脚本
 * 负责线索条目的添加、布局以及事件驱动的显示更新
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Events;
using Unity.VisualScripting;

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
        EventBus.Subscribe<ClueSharedEvent>(OnClueUpdated);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<ClueSharedEvent>(OnClueUpdated);
    }

    /* 线索更新事件回调 */
    void OnClueUpdated(ClueSharedEvent e)
    {
        AddClueEntry(e.timeline, e.imageData, false);
        Debug.Log("[ClueBoard] 收到线索共享事件，已添加新线索条目");
    }

    public static void AddClueEntry(int timeline, byte[] imageBytes, bool publish = true)
    {
        // 懒加载实例，防止 Awake 尚未执行或对象未激活导致静态实例为空
        if (s_instance == null)
        {
            s_instance = Object.FindFirstObjectByType<ClueBoard>();
            if (s_instance == null)
            {
                Debug.LogWarning("[ClueBoard] AddClueEntry 调用时未找到 ClueBoard 实例，条目未创建。");
                return;
            }
            else
            {
                Debug.Log("[ClueBoard] 懒加载 ClueBoard 实例成功，继续添加条目。");
            }
        }

        s_instance.CreateClueEntry(timeline, imageBytes);
        if (publish)
        {
            // 使用 ImageNetworkSender 分块发送大图，避免 Mirror 消息过大
            if (ImageNetworkSender.LocalInstance != null)
            {
                ImageNetworkSender.LocalInstance.SendImage(imageBytes, timeline, "Clue");
            }
            else
            {
                // 如果没有 ImageNetworkSender (例如未联网)，尝试走普通事件总线
                EventBus.Publish(new ClueSharedEvent
                {
                    timeline = timeline,
                    imageData = imageBytes
                });
            }
        }
    }
    
    /* 创建单个线索条目 */
    private void CreateClueEntry(int timeline, byte[] imageBytes = null)
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