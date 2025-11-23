using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks; // 仅用于启动 Task
using System.Text;
using Events;
using System.Collections; // 用于协程
using System;
using System.IO;
using System.Net.Http;

public class DialogPanel : MonoBehaviour
{
    [Tooltip("聊天消息预制体（包含MessageText和TypeText两个子对象）")]
    public GameObject chatMessagePrefab;

    [Tooltip("聊天图片预制体")]
    public GameObject chatImagePrefab;

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
    public string correctAnswer = "";
    [Tooltip("按层数配置的正确答案列表，第1层索引0，第2层索引1，以此类推")]
    public List<string> levelAnswers = new List<string>() { "南山", "归去" }; // 初始第一层答案

    private static DialogPanel s_instance;

    // --- UI 状态变量 ---
    private TMP_Text currentStreamingText;
    private Queue<string> streamQueue = new Queue<string>(); // 存储流式数据
    private bool isStreaming = false;
    private TMP_Text confirmButtonText; // 存储按钮文字组件
    private bool isConfirmButtonCooldown = false; // 按钮冷却状态
    private HashSet<uint> answeredPlayers = new HashSet<uint>(); // 已回答正确的玩家列表

    public enum AiTarget
    {
        DeepSeek,
        JimengImage
    }

    [Header("调试用：当前消息发送目标 AI")]
    public AiTarget currentAiTarget = AiTarget.JimengImage;

    void Awake()
    {
        s_instance = this;
        
        // 查找 UI 元素
        if (chatContent == null)
            chatContent = transform.Find("LeftPanel/ChatPanel/ChatScrollView/Viewport/Content");
        if (inputField == null)
            inputField = transform.Find("LeftPanel/InputPanel/InputField").GetComponent<TMP_InputField>();
        if (sendButton == null)
            sendButton = transform.Find("LeftPanel/InputPanel/SendButton").GetComponent<Button>();
        if (resultContent == null)
            resultContent = transform.Find("RightPanel/ResultPanel/InputField").GetComponent<TMP_InputField>();
        if (confirmButton == null)
            confirmButton = transform.Find("RightPanel/ResultPanel/ConfirmButton").GetComponent<Button>();
        
        
        // 获取确认按钮的文字组件
        confirmButtonText = confirmButton.GetComponentInChildren<TMP_Text>();
        
        // 绑定按钮事件
        sendButton.onClick.AddListener(OnSendButtonClicked);
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);

        EventBus.Subscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);
        //EventBus.Subscribe<AnswerCorrectEvent>(OnReceiveAnswerCorrectEvent);
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
        //EventBus.Unsubscribe<AnswerCorrectEvent>(OnReceiveAnswerCorrectEvent);
    }

    /* 聊天消息更新事件回调 */
    void OnChatMessageUpdated(ChatMessageUpdatedEvent e)
    {
        CreateChatMessage(e.MessageContent, e.MessageType);
    }

    /* 接收答案正确事件回调 */
    /*
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
    */
    
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

        // 根据当前调试目标，发送到不同的 AI
        if (currentAiTarget == AiTarget.DeepSeek)
        {
            StartCoroutine(StreamingCoroutine(userInput));
        }
        else
        {
            StartCoroutine(ImageGenCoroutine(userInput));
        }
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

        // 获取当前层级 & 动态正确答案
        var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<TimelinePlayer>();
        int currentLevel = localPlayer != null ? localPlayer.currentLevel : 1;
        string expectedAnswer = GetCorrectAnswerForLevel(currentLevel);
        Debug.Log($"[DialogPanel] 当前层数: {currentLevel}, 期望答案: '{expectedAnswer}', 玩家输入: '{userAnswer}'");

        // 检查答案是否正确（忽略前后空白, 支持完全匹配，可扩展为模糊匹配）
        if (string.Equals(userAnswer, expectedAnswer, StringComparison.Ordinal))
        {
            confirmButtonText.text = "正确！";
            confirmButtonText.color = Color.green;
            confirmButton.interactable = false; // 禁用按钮
            Debug.Log("[DialogPanel] 答案正确！");
            if (localPlayer != null)
            {
                // 调用服务器上报（层数推进在 NetworkManager 中处理）
                localPlayer.CmdReportedCorrectAnswer();
                Debug.Log("[DialogPanel] 已调用 CmdReportedCorrectAnswer() 上报服务器");
            }
            else
            {
                Debug.LogWarning("[DialogPanel] 未找到 TimelinePlayer，无法上报答案正确");
            }
        }
        else
        {
            Debug.Log($"[DialogPanel] 答案错误。输入: '{userAnswer}', 正确答案应为: '{expectedAnswer}'");
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

    // 根据层数获取正确答案（层数从1开始，列表索引从0开始）
    private string GetCorrectAnswerForLevel(int level)
    {
        if (level <= 0)
        {
            return correctAnswer; // 容错：非法层数返回默认答案
        }
        int index = level - 1;
        if (index < levelAnswers.Count)
        {
            var ans = levelAnswers[index];
            if (!string.IsNullOrWhiteSpace(ans)) return ans.Trim();
        }
        // 若该层未配置，返回最后一个已配置答案或默认
        if (levelAnswers.Count > 0 && !string.IsNullOrWhiteSpace(levelAnswers[levelAnswers.Count - 1]))
        {
            return levelAnswers[levelAnswers.Count - 1].Trim();
        }
        return correctAnswer;
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

    /* 即梦图像生成协程（在主线程执行）*/
    private IEnumerator ImageGenCoroutine(string prompt)
    {
        isStreaming = true;

        var localPlayerIdentity = Mirror.NetworkClient.localPlayer;
        var localPlayer = localPlayerIdentity != null ? localPlayerIdentity.GetComponent<TimelinePlayer>() : null;
        int timeline = localPlayer != null ? localPlayer.timeline : -1;
        Debug.Log($"[DialogPanel] ImageGenCoroutine 启动，Timeline: {timeline}, Prompt: {prompt}");

        string imageUrl = null;
        string errorMessage = null;
        bool callCompleted = false;

        // 后台 Task 只负责调用即梦 API，拿到 URL 或错误
        Task.Run(async () =>
        {
            await JimengService.GenerateImage(
                prompt,
                url =>
                {
                    imageUrl = url;
                    callCompleted = true;
                },
                err =>
                {
                    errorMessage = err;
                    callCompleted = true;
                }
            );

            isStreaming = false;
        });

        // 在主线程等待 API 调用结束
        while (!callCompleted)
        {
            yield return null;
        }

        string finalMessage = string.Empty;

        if (!string.IsNullOrEmpty(errorMessage))
        {
            finalMessage = $"[即梦调用错误] {errorMessage}";
        }
        else if (!string.IsNullOrEmpty(imageUrl))
        {
            // 在主线程中使用 Unity API，构建本地路径并下载图片
            string fullPath = null;
            try
            {
                string folder = Path.Combine(Application.persistentDataPath, "JimengTempImages");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = $"jimeng_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                fullPath = Path.Combine(folder, fileName);
            }
            catch (Exception ex)
            {
                finalMessage = $"[即梦生成失败] 生成本地路径出错: {ex.Message}";
            }

            if (string.IsNullOrEmpty(finalMessage))
            {
                var www = UnityEngine.Networking.UnityWebRequest.Get(imageUrl);
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    try
                    {
                        File.WriteAllBytes(fullPath, www.downloadHandler.data);
                        finalMessage = $"[即梦生成完成]\nURL: {imageUrl}\n本地: {fullPath}";
                    }
                    catch (Exception ex)
                    {
                        finalMessage = $"[即梦生成失败] 保存图片出错: {ex.Message}";
                    }
                }
                else
                {
                    finalMessage = $"[即梦生成失败] 下载图片出错: {www.error}";
                }
            }
        }
        else
        {
            finalMessage = "[即梦生成失败] 未收到图片 URL";
        }

        if (currentStreamingText != null)
        {
            currentStreamingText.text = finalMessage;
        }

        EventBus.Publish(new ChatMessageUpdatedEvent
        {
            MessageContent = finalMessage,
            MessageType = MessageType.Future
        });

        Debug.Log("[DialogPanel] ImageGenCoroutine 结束");
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

        // 根据消息类型设置 Avatar 颜色
        Transform avatarTransform = newMessage.transform.Find("Avatar");
        if (avatarTransform != null)
        {
            Image avatarImage = avatarTransform.GetComponent<Image>();
            if (avatarImage != null)
            {
                switch (type)
                {
                    case MessageType.Modern:
                        avatarImage.color = new Color(0.68f, 0.85f, 0.90f); // 浅蓝色
                        break;
                    case MessageType.Ancient:
                        avatarImage.color = new Color(0.80f, 0.65f, 0.40f); // 黄褐色
                        break;
                    case MessageType.Future:
                        avatarImage.color = new Color(0.60f, 0.50f, 0.90f); // 蓝紫色
                        break;
                    default:
                        avatarImage.color = Color.white; // 默认白色
                        break;
                }
                Debug.Log($"[DialogPanel.CreateChatMessage] Avatar 颜色设置成功: {type}");
            }
        }

        newMessage.transform.SetAsLastSibling();
        Debug.Log($"[DialogPanel.CreateChatMessage] 消息创建完成！");
    }

    /* 添加新的聊天图片消息 */
    public static void AddChatImage(Sprite image)
    {
        if (s_instance == null || image == null) return;

        // 创建图片消息
        s_instance.CreateChatImage(image);
    }

    /* 创建单个聊天图片消息 */
    private void CreateChatImage(Sprite image)
    {
        if (chatContent == null || chatImagePrefab == null) return;
    
        // 实例化图片消息预制体
        GameObject newImageMessage = Instantiate(chatImagePrefab, chatContent);
    
        // 查找图片组件并设置图片
        Transform imageTransform = newImageMessage.transform.Find("Image");
        if (imageTransform != null)
        {
            Image imageComponent = imageTransform.GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.sprite = image;
    
                // 设置图片宽度为 405pt，高度根据图片比例动态调整
                RectTransform rectTransform = imageComponent.GetComponent<RectTransform>();
                if (rectTransform != null && image != null)
                {
                    float aspectRatio = image.rect.height / image.rect.width; // 计算图片宽高比
                    rectTransform.sizeDelta = new Vector2(405f, 405f * aspectRatio); // 设置宽度为 405，高度根据比例调整
                }
            }
        }
    
        // 将消息放置在聊天内容的末尾
        newImageMessage.transform.SetAsLastSibling();
    }
    
    // 进入新的一层时重置“提交”按钮状态（从“正确！”恢复为“提交”并可点击）
    public static void ResetConfirmButtonForNewLevel()
    {
        if (s_instance == null) return;
        try
        {
            if (s_instance.confirmButtonText != null)
            {
                s_instance.confirmButtonText.text = "提交";
                s_instance.confirmButtonText.color = Color.black;
            }
            if (s_instance.confirmButton != null)
            {
                s_instance.confirmButton.interactable = true;
            }
            if (s_instance.resultContent != null)
            {
                s_instance.resultContent.text = string.Empty;
            }
            s_instance.isConfirmButtonCooldown = false;
            Debug.Log("[DialogPanel] 已因层数提升重置提交按钮为 '提交'");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DialogPanel] 重置提交按钮失败: {ex.Message}");
        }
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