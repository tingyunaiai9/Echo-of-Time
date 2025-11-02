using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks; // 仅用于启动 Task
using System.Text;
using Events;
using System.Collections; // 用于协程
using System;

public class DialogPanel : MonoBehaviour
{
    [Tooltip("聊天消息预制体（包含MessageText和TypeText两个子对象）")]
    public GameObject chatMessagePrefab;

    [Tooltip("聊天消息容器（Vertical Layout Group）")]
    public Transform chatContent;

    [Tooltip("输入框组件")]
    public TMP_InputField inputField;

    [Tooltip("发送按钮")]
    public Button sendButton;

    [Tooltip("结果输入组件")]
    public TMP_InputField resultContent;

    [Tooltip("确认按钮")]
    public Button confirmButton;

    private static DialogPanel s_instance;

    // --- UI 状态变量 ---
    private TMP_Text currentStreamingText;
    private Queue<string> streamQueue = new Queue<string>(); // 存储流式数据
    private bool isStreaming = false;

    void Awake()
    {
        s_instance = this;
        // 查找 UI 元素
        if (chatContent == null)
            chatContent = transform.Find("Panel/LeftPanel/ChatScrollView/Viewport/Content");
        if (inputField == null)
            inputField = transform.Find("Panel/LeftPanel/InputPanel/InputField").GetComponent<TMP_InputField>();
        if (sendButton == null)
            sendButton = transform.Find("Panel/LeftPanel/InputPanel/InputField/SendButton").GetComponent<Button>();
        if (resultContent == null)
            resultContent = transform.Find("Panel/RightPanel/ResultPanel/Background/InputField").GetComponent<TMP_InputField>();
        if (confirmButton == null)
            confirmButton = transform.Find("Panel/RightPanel/ResultPanel/Background/ConfirmButton").GetComponent<Button>();

        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }

        EventBus.SafeSubscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);
    }

    void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
        }
        EventBus.SafeUnsubscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);
    }

    /* 聊天消息更新事件回调 */
    void OnChatMessageUpdated(ChatMessageUpdatedEvent e)
    {
        CreateChatMessage(e.MessageContent, e.MessageType);
    }

    /* 发送按钮点击事件 */
    private void OnSendButtonClicked()
    {
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;

        string userInput = inputField.text.Trim();
        inputField.text = "";

        // 添加用户输入的消息 (本地显示)
        AddChatMessage(userInput, MessageType.Modern, publish: false);

        // 预创建 AI 响应的消息框 (本地显示)
        CreateStreamingMessage(MessageType.Future);

        // 启动协程处理流式输出
        StartCoroutine(StreamingCoroutine(userInput));
    }

    /* 流式输出协程（在主线程执行）*/
    private IEnumerator StreamingCoroutine(string prompt)
    {
        isStreaming = true;
        StringBuilder fullResponse = new StringBuilder();

        // 1. 定义回调函数，用于处理来自服务的数据
        Action<string> onChunkReceived = (chunk) =>
        {
            // (运行在后台线程) 将数据块放入队列
            streamQueue.Enqueue(chunk);
        };

        Action onStreamEnd = () =>
        {
            // (运行在后台线程) 标记流式传输结束
            isStreaming = false;
        };
        
        Action<string> onError = (errorMessage) =>
        {
            // (运行在后台线程) 将错误信息放入队列
            streamQueue.Enqueue(errorMessage);
            isStreaming = false;
        };

        // 2. 在后台线程启动 API 调用
        //    现在改为调用新拆分出去的 DeepSeekService
        Task.Run(async () =>
        {
            // 调用独立的静态服务
            await DeepSeekService.GetChatCompletionStreaming(
                prompt,
                onChunkReceived,
                onStreamEnd,
                onError
            );
        });

        // 3. 在主线程处理队列中的流式数据
        while (isStreaming || streamQueue.Count > 0)
        {
            if (streamQueue.Count > 0)
            {
                string deltaContent = streamQueue.Dequeue();
                fullResponse.Append(deltaContent);

                // 实时更新 UI
                if (currentStreamingText != null)
                {
                    currentStreamingText.text = fullResponse.ToString();
                }
            }

            yield return null; // 等待下一帧
        }

        // 4. 发布完成事件
        //    这会将最终结果通过 EventBus 同步给所有玩家
        string finalContent = fullResponse.ToString();
        if (!string.IsNullOrEmpty(finalContent))
        {
            EventBus.Instance.Publish(new ChatMessageUpdatedEvent
            {
                MessageContent = finalContent,
                MessageType = MessageType.Future
            });
        }

        Debug.Log("[DialogPanel] 流式输出协程结束");
    }

    /* 创建流式输出的消息容器 */
    private void CreateStreamingMessage(MessageType type)
    {
        if (chatContent == null || chatMessagePrefab == null) return;

        GameObject currentStreamingMessage = Instantiate(chatMessagePrefab, chatContent);
        currentStreamingMessage.transform.SetAsLastSibling();

        Transform messageTextTransform = currentStreamingMessage.transform.Find("MessageText");
        if (messageTextTransform != null)
        {
            currentStreamingText = messageTextTransform.GetComponent<TMP_Text>();
            if (currentStreamingText != null)
            {
                currentStreamingText.text = "俺在思考……";
            }
        }

        Transform typeTextTransform = currentStreamingMessage.transform.Find("TypeText");
        if (typeTextTransform != null)
        {
            TMP_Text typeText = typeTextTransform.GetComponent<TMP_Text>();
            if (typeText != null)
            {
                typeText.text = type.ToString();
            }
        }

        streamQueue.Clear();
        isStreaming = true;
    }

    /* 添加新的聊天消息 */
    public static void AddChatMessage(string content, MessageType type, bool publish = true)
    {
        if (s_instance == null) return;
        s_instance.CreateChatMessage(content, type);
        if (publish)
        {
            EventBus.Instance.Publish(new ChatMessageUpdatedEvent
            {
                MessageContent = content,
                MessageType = type
            });
        }
    }

    /* 批量添加聊天消息 */
    public static void AddChatMessages(List<ChatMessageData> messages)
    {
        if (s_instance == null || messages == null) return;
        foreach (var messageData in messages)
        {
            s_instance.CreateChatMessage(messageData.content, messageData.type);
        }
    }

    /* 创建单个聊天消息 */
    private void CreateChatMessage(string content, MessageType type)
    {
        if (chatContent == null || chatMessagePrefab == null) return;
        GameObject newMessage = Instantiate(chatMessagePrefab, chatContent);

        Transform messageTextTransform = newMessage.transform.Find("MessageText");
        if (messageTextTransform != null)
        {
            TMP_Text messageText = messageTextTransform.GetComponent<TMP_Text>();
            if (messageText != null)
            {
                messageText.text = content;
            }
        }

        Transform typeTextTransform = newMessage.transform.Find("TypeText");
        if (typeTextTransform != null)
        {
            TMP_Text typeText = typeTextTransform.GetComponent<TMP_Text>();
            if (typeText != null)
            {
                typeText.text = type.ToString();
            }
        }

        newMessage.transform.SetAsLastSibling();
    }
}

/* 聊天消息数据结构 */
[System.Serializable]
public class ChatMessageData
{
    public string content;
    public MessageType type;

    public ChatMessageData(string content, MessageType type)
    {
        this.content = content;
        this.type = type;
    }
}