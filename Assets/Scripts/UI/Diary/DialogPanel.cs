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

    [Header("正确答案配置")]
    [Tooltip("需要匹配的正确答案")]
    public string correctAnswer = "南山";

    private static DialogPanel s_instance;

    // --- UI 状态变量 ---
    private TMP_Text currentStreamingText;
    private Queue<string> streamQueue = new Queue<string>(); // 存储流式数据
    private bool isStreaming = false;
    private TMP_Text confirmButtonText; // 存储按钮文字组件
    private bool isConfirmButtonCooldown = false; // 按钮冷却状态
    private HashSet<uint> answeredPlayers = new HashSet<uint>(); // 已回答正确的玩家列表

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
            resultContent = transform.Find("Panel/RightPanel/ResultPanel/BackGround/InputField").GetComponent<TMP_InputField>();
        if (confirmButton == null)
            confirmButton = transform.Find("Panel/RightPanel/ResultPanel/BackGround/ConfirmButton").GetComponent<Button>();
        
        // 获取确认按钮的文字组件
        confirmButtonText = confirmButton.GetComponentInChildren<TMP_Text>();
        
        // 绑定按钮事件
        sendButton.onClick.AddListener(OnSendButtonClicked);
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);

        EventBus.Subscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);
        EventBus.Subscribe<AnswerCorrectEvent>(OnReceiveAnswerCorrectEvent);
    }

    void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
        }
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }
        EventBus.Unsubscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);
        EventBus.Unsubscribe<AnswerCorrectEvent>(OnReceiveAnswerCorrectEvent);
    }

    /* 聊天消息更新事件回调 */
    void OnChatMessageUpdated(ChatMessageUpdatedEvent e)
    {
        CreateChatMessage(e.MessageContent, e.MessageType);
    }

    /* 接收答案正确事件回调 */
    void OnReceiveAnswerCorrectEvent(AnswerCorrectEvent e)
    {
        // 如果该玩家已经回答过，忽略重复事件
        if (!answeredPlayers.Add(e.playerNetId))
        {
            Debug.Log($"[DialogPanel] 玩家 {e.playerNetId} 重复回答，已忽略");
            return;
        }

        Debug.Log($"[DialogPanel] 收到答案正确事件，玩家: {e.playerNetId}，当前正确计数: {answeredPlayers.Count}");

        if (answeredPlayers.Count >= 3)
        {
            Debug.Log("[DialogPanel] 全部玩家解答正确");
        }
    }
    
    /* 发送按钮点击事件 */
    private void OnSendButtonClicked()
    {
        Debug.Log("[DialogPanel] 发送按钮被点击");
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text))
        {
            Debug.LogWarning("[DialogPanel] 输入框为空，已忽略");
            return;
        }

        string userInput = inputField.text.Trim();
        inputField.text = "";

        // 添加用户输入的消息 (本地显示)
        AddChatMessage(userInput, MessageType.Modern, publish: false);

        // 预创建 AI 响应的消息框 (本地显示)
        CreateStreamingMessage(MessageType.Future);

        // 启动协程处理流式输出
        StartCoroutine(StreamingCoroutine(userInput));
    }

    /* 确认按钮点击事件 */
    private void OnConfirmButtonClicked()
    {
        Debug.Log("[DialogPanel] 确认按钮被点击");

        // 如果在冷却期间，直接返回
        if (isConfirmButtonCooldown)
        {
            Debug.Log("[DialogPanel] 按钮冷却中，忽略点击");
            return;
        }

        if (resultContent == null) return;

        string userAnswer = resultContent.text.Trim();
        // 检查答案是否正确
        if (userAnswer == correctAnswer)
        {
            confirmButtonText.text = "正确！";
            confirmButtonText.color = Color.green;
            confirmButton.interactable = false; // 禁用按钮
            Debug.Log("[DialogPanel] 答案正确！");
            // 发布答案正确事件
            var localPlayer = Mirror.NetworkClient.localPlayer;
            if (localPlayer != null)
            {
                EventBus.Publish(new AnswerCorrectEvent { playerNetId = localPlayer.netId });
                Debug.Log("[DialogPanel] 已发布 AnswerCorrectEvent");
            }
            else
                Debug.LogWarning("[DialogPanel] 无法获取本地玩家，未发布 AnswerCorrectEvent");
        }
        else
        {
            Debug.Log($"[DialogPanel] 答案错误。输入: '{userAnswer}', 正确答案: '{correctAnswer}'");
            // 启动冷却协程
            StartCoroutine(ErrorCooldownCoroutine());
        }

        // 匿名协程函数
        IEnumerator ErrorCooldownCoroutine()
        {
            isConfirmButtonCooldown = true;
            confirmButtonText.text = "错误！";
            confirmButtonText.color = Color.red;

            yield return new WaitForSeconds(1f);

            confirmButtonText.text = "确认";
            confirmButtonText.color = Color.black;
            isConfirmButtonCooldown = false;
            Debug.Log("[DialogPanel] 按钮文字已重置");
        }
    }


    /* 流式输出协程（在主线程执行）*/
    private IEnumerator StreamingCoroutine(string prompt)
    {
        isStreaming = true;
        StringBuilder fullResponse = new StringBuilder();

        // 在主线程获取 Timeline
        var localPlayerIdentity = Mirror.NetworkClient.localPlayer;
        var localPlayer = localPlayerIdentity != null ? localPlayerIdentity.GetComponent<TimelinePlayer>() : null;
        int timeline = localPlayer != null ? localPlayer.timeline : -1;
        Debug.Log($"[DialogPanel] 获取到本地玩家 Timeline: {timeline}");

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
                timeline, // 使用在主线程获取的timeline
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
            EventBus.Publish(new ChatMessageUpdatedEvent
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
            EventBus.Publish(new ChatMessageUpdatedEvent
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