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
    private static bool s_subscribed; 

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
    }

    // 确保订阅事件（静态初始化）
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSubscribed()
    {
        if (s_subscribed) return;
        EventBus.Subscribe<ClueSharedEvent>(OnClueUpdatedStatic);
        s_subscribed = true;
    }
    
    private static void OnClueUpdatedStatic(ClueSharedEvent e)
    {
        if (!EnsureInstance()) return;
        s_instance.OnClueUpdated(e);
    }

    /* 线索更新事件回调 */
    void OnClueUpdated(ClueSharedEvent e)
    {
        // 根据事件中实际携带的数据类型区分图片/文字
        if (e.imageData != null && e.imageData.Length > 0)
        {
            CreateClueEntry(e.timeline, e.level, e.imageData);
        }
        else if (!string.IsNullOrEmpty(e.text))
        {
            CreateClueEntry(e.timeline, e.level, e.text);
        }
        else
        {
            Debug.LogWarning("[ClueBoard] 收到 ClueSharedEvent，但既没有图片也没有文本数据");
            return;
        }

        Debug.Log("[ClueBoard] 收到线索共享事件，已添加新线索条目");
    }

    // 图片入口
    public static void AddClueEntry(int timeline, int level, byte[] data, bool publish = true)
    {
        if (!EnsureInstance()) return;
        s_instance.CreateClueEntry(timeline, level, data);
        if (publish)
        {
            EventBus.Publish(new ClueSharedEvent
            {
                timeline = timeline,
                level = level,
                imageData = data,
            });
        }
    }

    // 文本入口
    public static void AddClueEntry(int timeline, int level, string text, bool publish = true)
    {
        if (!EnsureInstance()) return;

        s_instance.CreateClueEntry(timeline, level, text);
        if (publish)
        {
            EventBus.Publish(new ClueSharedEvent
            {
                timeline = timeline,
                level = level,
                text = text,
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
    private void CreateClueEntry(int timeline, int level, byte[] imageBytes)
    {
        if (contentParent == null || Note_Image == null) return;
        
        GameObject newNote = Instantiate(Note_Image, contentParent);
        ApplyCommonLayout(newNote, timeline, level);

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
    private void CreateClueEntry(int timeline, int level, string textContent)
    {
        if (contentParent == null || Note_Text == null) return;

        GameObject newNote = Instantiate(Note_Text, contentParent);
        ApplyCommonLayout(newNote, timeline, level);

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
    private void ApplyCommonLayout(GameObject noteGO, int timeline, int level)
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
                        dateTextComponent.text = "古代";
                        break;
                    case 1:
                        dateTextComponent.text = "民国";
                        break;
                    case 2:
                        dateTextComponent.text = "未来";
                        break;
                    default:
                        dateTextComponent.text = "未知";
                        break;
                }
                switch(level)
                {
                    case 1:
                        dateTextComponent.text += " - 第一层";
                        break;
                    case 2:
                        dateTextComponent.text += " - 第二层";
                        break;
                    case 3:
                        dateTextComponent.text += " - 第三层";
                        break;
                    default:
                        dateTextComponent.text += " - 未知章节";
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

