using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Events;

/*
 控制聊天消息的添加与显示
*/
public class ChatPanel : MonoBehaviour
{
    [Tooltip("聊天消息预制体（包含Image和MessageText两个子对象）")]
    public GameObject chatMessagePrefab;

    [Tooltip("聊天消息容器（Vertical Layout Group）")]
    public Transform contentParent;

    private static ChatPanel s_instance;

    void Awake()
    {
        s_instance = this;
        if (contentParent == null)
        {
            contentParent = transform.Find("Viewport/Content");
        }
        EventBus.Instance.Subscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);
    }

    void OnDestroy()
    {
        EventBus.Instance.Unsubscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);
    }

    /* 聊天消息更新事件回调 */
    void OnChatMessageUpdated(ChatMessageUpdatedEvent e)
    {
        AddChatMessage(e.MessageContent, e.MessageImage, publish: false);
    }

    /* 添加新的聊天消息 */
    public static void AddChatMessage(string content, Sprite image = null, bool publish = true)
    {
        if (s_instance == null) return;
        s_instance.CreateChatMessage(content, image);
        if (publish)
        {
            EventBus.Instance.Publish(new ChatMessageUpdatedEvent
            {
                MessageContent = content,
                MessageImage = image
            });
        }
    }

    /* 批量添加聊天消息 */
    public static void AddChatMessages(List<ChatMessageData> messages)
    {
        if (s_instance == null || messages == null) return;
        foreach (var messageData in messages)
        {
            s_instance.CreateChatMessage(messageData.content, messageData.image);
        }
    }

    /* 创建单个聊天消息 */
    private void CreateChatMessage(string content, Sprite image)
    {
        if (contentParent == null || chatMessagePrefab == null) return;
        GameObject newMessage = Instantiate(chatMessagePrefab, contentParent);

        // 设置消息图像
        Transform imageTransform = newMessage.transform.Find("Image");
        if (imageTransform != null)
        {
            UnityEngine.UI.Image messageImage = imageTransform.GetComponent<UnityEngine.UI.Image>();
            if (messageImage != null && image != null)
            {
                messageImage.sprite = image;
                messageImage.preserveAspect = true;
            }
        }

        // 设置消息文本
        Transform messageTextTransform = newMessage.transform.Find("MessageText");
        if (messageTextTransform != null)
        {
            TMP_Text messageText = messageTextTransform.GetComponent<TMP_Text>();
            if (messageText != null)
            {
                messageText.text = content;
            }
        }

        newMessage.transform.SetAsLastSibling();

        // 可选：自动滚动到最新消息
        // ScrollToBottom();
    }

    /* 添加测试聊天消息 */
    [ContextMenu("Test Add Chat Messages")]
    public void TestChatMessages()
    {
        var testMessages = new List<ChatMessageData>
        {
            new ChatMessageData("你好，我发现了一个重要的线索！", null),
            new ChatMessageData("看看这张图片，你觉得这是什么？", Resources.Load<Sprite>("TestImage")),
            new ChatMessageData("我们需要尽快讨论下一步计划。", null)
        };
        AddChatMessages(testMessages);
    }

    /* 可选：滚动到底部方法 */
    private void ScrollToBottom()
    {
        // 如果需要实现自动滚动，可以在这里添加ScrollRect的相关代码
        // Canvas.ForceUpdateCanvases();
        // scrollRect.verticalNormalizedPosition = 0f;
    }
}

/* 聊天消息数据结构 */
[System.Serializable]
public class ChatMessageData
{
    public string content;
    public Sprite image;

    public ChatMessageData(string content, Sprite image)
    {
        this.content = content;
        this.image = image;
    }
}

/* 聊天消息更新事件 */
public class ChatMessageUpdatedEvent
{
    public string MessageContent { get; set; }
    public Sprite MessageImage { get; set; }
}