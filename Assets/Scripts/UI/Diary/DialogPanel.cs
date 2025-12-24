/* UI/Diary/DialogPanel.cs
 * 日记左侧对话与 AI 交互面板
 * 负责本地聊天消息展示、DeepSeek 文本流式调用与即梦图像生成入口
 */

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

/*
 * 日记对话面板控制组件
 * 管理聊天消息 UI、按钮交互以及调用不同 AI 目标处理玩家输入
 */

public class DialogPanel : MonoBehaviour
{
    [Header("聊天区域组件引用")]
    [Tooltip("聊天消息预制体")]
    public GameObject chatMessagePrefab;
    [Tooltip("第一条聊天消息预制体")]
    public GameObject firstChatMessagePrefab;
    [Tooltip("聊天图片预制体")]
    public GameObject chatImagePrefab;
    [Tooltip("聊天消息容器（Vertical Layout Group）")]
    public Transform chatContent;

    [Header("输入区域组件引用")]
    [Tooltip("输入框组件")]
    public TMP_InputField inputField;
    [Tooltip("不同时间线头像图片")]
    public Sprite[] avatarSprites = new Sprite[3]; // 不同时间线的头像图片
    [Tooltip("默认头像图片")]
    public Sprite defaultAvatarSprite;

    private static DialogPanel s_instance;

    // --- UI 状态变量 ---
    private TMP_Text currentStreamingText;
    private GameObject currentStreamingMessageGO; // 当前流式消息占位对象
    private Queue<string> streamQueue = new Queue<string>(); // 存储流式数据
    private bool isStreaming = false;

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
        
        EventBus.Subscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);
        EventBus.Subscribe<ChatImageUpdatedEvent>(OnChatImageUpdated);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnSendButtonClicked();
        }
    }

    void OnEnable()
    {
        Debug.Log("[DialogPanel] 日记对话面板已初始化并启用");
        if (firstChatMessagePrefab != null && chatContent != null)
        {
            // 添加第一条欢迎消息
            Transform messageTextTransform = firstChatMessagePrefab.transform.Find("MessageText");
            TMP_Text messageText = messageTextTransform.GetComponent<TMP_Text>();
            if (TimelinePlayer.Local != null)
            {
                int timeline = TimelinePlayer.Local.timeline;
                switch (timeline)
                {
                    case 0:
                        messageText.text = "千言万语皆空妄，真言唯我为世诠。";
                        break;
                    case 1:
                        messageText.text = "万象纷纭皆是幻，真容待我笔中现。";
                        break;
                    case 2:
                        messageText.text = "欲将心语化星文，暗寄荧屏无字痕。";
                        break;
                    default:
                        messageText.text = "欢迎来到桃花源";
                        break;
                }
            } 
        }
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);
        EventBus.Unsubscribe<ChatImageUpdatedEvent>(OnChatImageUpdated);
        //EventBus.Unsubscribe<AnswerCorrectEvent>(OnReceiveAnswerCorrectEvent);
    }

    /* 聊天消息更新事件回调 */
    void OnChatMessageUpdated(ChatMessageUpdatedEvent e)
    {
        CreateChatMessage(e.MessageContent, e.timeline);
    }

    /* 聊天图片更新事件回调 */
    void OnChatImageUpdated(ChatImageUpdatedEvent e)
    {
        CreateChatImage(e.imageData, e.timeline);
    }
    
    /* 发送按钮点击事件 */
    public void OnSendButtonClicked()
    {
        Debug.Log("[DialogPanel] 发送按钮被点击");
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text))
        {
            Debug.LogWarning("[DialogPanel] 输入框为空，已忽略");
            return;
        }

        string userInput = inputField.text.Trim();
        inputField.text = "";
        
        // 禁用输入框，防止在 AI 响应期间发送新消息
        inputField.interactable = false;

        // 获取当前玩家的时间线
        int timeline = TimelinePlayer.Local.timeline;

        // 添加用户输入的消息 (本地显示)
        AddChatMessage(userInput, timeline, publish: false);

        // 预创建 AI 响应的消息框 (本地显示)
        CreateStreamingMessage(timeline);

        Debug.Log($"[DialogPanel] 当前 Timeline: {timeline}");

        // 根据时间线自动路由
        // Timeline 1: 即梦 AI
        // Timeline 0, 2 (及其他): DeepSeek
        if (timeline == 1)
        {
            // 添加民国风格的 Prompt 前缀
            string stylePrompt = $"一张民国时期的黑白素描，复古风格，线条粗糙，旧纸张质感，{userInput}，画面当中不能有任何文字描述";
            StartCoroutine(ImageGenCoroutine(stylePrompt));
        }
        else
        {
            StartCoroutine(StreamingCoroutine(userInput));
        }
    }


    /* 流式输出协程（在主线程执行）*/
    private IEnumerator StreamingCoroutine(string prompt)
    {
        isStreaming = true;
        StringBuilder fullResponse = new StringBuilder();

        // 在主线程获取 Timeline
        int timeline = TimelinePlayer.Local.timeline;
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
                timeline = timeline
            });
        }
        
        // 重新启用输入框
        if (inputField != null)
        {
            inputField.interactable = true;
        }

        Debug.Log("[DialogPanel] 流式输出协程结束");
    }

    /* 即梦图像生成协程（在主线程执行） */
    private IEnumerator ImageGenCoroutine(string prompt)
    {
        isStreaming = true;
    
        int timeline = TimelinePlayer.Local.timeline;
        int level = TimelinePlayer.Local.currentLevel;
        Debug.Log($"[DialogPanel] ImageGenCoroutine 启动，Timeline: {timeline}, Prompt: {prompt}");
    
        string imageUrl = null;
        string errorMessage = null;
        bool callCompleted = false;
    
        // 后台 Task 负责调用即梦 API，获取 URL 或错误
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
            Debug.LogError($"[Jimeng] 即梦调用错误: {errorMessage}");
        }
        else if (!string.IsNullOrEmpty(imageUrl))
        {
            var www = UnityEngine.Networking.UnityWebRequest.Get(imageUrl);
            yield return www.SendWebRequest();
    
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                try
                {
                    byte[] imageData = www.downloadHandler.data;
                    Debug.Log($"[Jimeng] 图片下载成功，大小: {imageData.Length} 字节");
    
                    // 压缩图片数据
                    byte[] compressedData = ImageUtils.CompressImageBytesToJpeg(imageData, 10); // 可调整质量
                    if (compressedData != null)
                    {
                        Debug.Log($"[Jimeng] 图片压缩成功，压缩后大小: {compressedData.Length} 字节");

                        // 创建图片消息（强制设置 timeline 为 3，表示即梦图片）
                        s_instance.CreateChatImage(imageData, 3);
                        // 发布图片事件，统一由 EventBus 处理网络发送
                        EventBus.Publish(new ChatImageUpdatedEvent
                        {
                            imageData = compressedData,
                            timeline = timeline
                        });

                        // 图片生成成功后，移除"俺在思考……"占位消息
                        if (currentStreamingMessageGO != null)
                        {
                            Destroy(currentStreamingMessageGO);
                            currentStreamingMessageGO = null;
                            currentStreamingText = null;
                        }
                        finalMessage = string.Empty; // 聊天面板只展示图片，不展示文本
                    }
                    else
                    {
                        finalMessage = "[即梦生成失败] 图片压缩失败";
                        Debug.LogError("[Jimeng] 图片压缩失败");
                    }
                }
                catch (Exception ex)
                {
                    finalMessage = $"[即梦生成失败] 处理图片出错: {ex.Message}";
                    Debug.LogError($"[Jimeng] 处理图片失败: {ex.Message}");
                }
            }
            else
            {
                finalMessage = $"[即梦生成失败] 下载图片出错: {www.error}";
                Debug.LogError($"[Jimeng] 下载图片失败: {www.error}");
            }
        }
        else
        {
            finalMessage = "[即梦生成失败] 未收到图片 URL";
        }
    
        // 仅在需要时在聊天框中显示文本（例如错误信息）
        if (!string.IsNullOrEmpty(finalMessage))
        {
            if (currentStreamingText != null)
            {
                currentStreamingText.text = finalMessage;
            }
    
            EventBus.Publish(new ChatMessageUpdatedEvent
            {
                MessageContent = finalMessage,
                timeline = timeline
            });
        }
        
        // 重新启用输入框
        if (inputField != null)
        {
            inputField.interactable = true;
        }
    
        Debug.Log("[DialogPanel] ImageGenCoroutine 结束");
    }    

    /* 创建流式输出的消息容器 */
    private void CreateStreamingMessage(int timeline)
    {
        if (chatContent == null || chatMessagePrefab == null) return;

        GameObject currentStreamingMessage = Instantiate(chatMessagePrefab, chatContent);
        currentStreamingMessage.transform.SetAsLastSibling();

        // 记录当前占位消息对象，以便后续替换/销毁
        currentStreamingMessageGO = currentStreamingMessage;

        Transform messageTextTransform = currentStreamingMessage.transform.Find("MessageText");
        if (messageTextTransform != null)
        {
            currentStreamingText = messageTextTransform.GetComponent<TMP_Text>();
            if (currentStreamingText != null)
            {
                switch (timeline)
                {
                    case 0:
                        currentStreamingText.text = "日记上显现出神秘的诗篇……";
                        break;
                    case 1:
                        currentStreamingText.text = "日记中浮现出梦幻的画卷……";
                        break;
                    case 2:
                        currentStreamingText.text = "日记上跳出了奇怪的代码……";
                        break;
                    default:
                        currentStreamingText.text = "思考中……";
                        break;
                }
            }
            Transform backgroundTransform = currentStreamingMessage.transform.Find("Background");
            Transform avatarTransform = backgroundTransform?.Find("Avatar");
            if (avatarTransform != null)
            {
                Image avatarImage = avatarTransform.GetComponent<Image>();
                if (avatarImage != null)
                {
                    avatarImage.sprite = defaultAvatarSprite;
                }
            }
        }
        Transform typeTextTransform = currentStreamingMessage.transform.Find("TypeText");
        if (typeTextTransform != null)
        {
            TMP_Text typeText = typeTextTransform.GetComponent<TMP_Text>();
            if (typeText != null)
            {
                typeText.text = GetTimelineName(timeline);
            }
        }

        streamQueue.Clear();
        isStreaming = true;
    }

    /* 添加新的聊天消息 */
    public static void AddChatMessage(string content, int timeline, bool publish = true)
    {        
        s_instance.CreateChatMessage(content, timeline);
        if (publish)
        {
            EventBus.Publish(new ChatMessageUpdatedEvent
            {
                MessageContent = content,
                timeline = timeline
            });
        }
    }

    /* 创建单个聊天消息 */
    private void CreateChatMessage(string content, int timeline)
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
    
        // 根据时间线设置 Background 颜色
        Transform backgroundTransform = newMessage.transform.Find("Background");
        if (backgroundTransform != null)
        {
            Image backgroundImage = backgroundTransform.GetComponent<Image>();
            if (backgroundImage != null)
            {
                switch (timeline)
                {
                    case 0: // Ancient - 青绿色
                        backgroundImage.color = new Color(0.4f, 0.8f, 0.6f);
                        break;
                    case 1: // Modern - 黄褐色
                        backgroundImage.color = new Color(0.8f, 0.65f, 0.4f);
                        break;
                    case 2: // Future - 天蓝色
                        backgroundImage.color = new Color(0.53f, 0.81f, 0.92f);
                        break;
                    default:
                        backgroundImage.color = Color.white;
                        break;
                }
                Debug.Log($"[DialogPanel.CreateChatMessage] Background 颜色设置成功，Timeline: {timeline}");
            }
        }

        // 根据时间线设置 Avatar 图片
        Transform avatarTransform = backgroundTransform?.Find("Avatar");
        if (avatarTransform != null)
        {
            Image avatarImage = avatarTransform.GetComponent<Image>();
            if (avatarImage != null)
            {
                // 使用 avatarSprites 数组设置头像
                if (timeline >= 0 && timeline < avatarSprites.Length && avatarSprites[timeline] != null)
                {
                    avatarImage.sprite = avatarSprites[timeline];
                    Debug.Log($"[DialogPanel.CreateChatMessage] Avatar 图片设置成功，Timeline: {timeline}");
                }
                else
                {
                    avatarImage.sprite = defaultAvatarSprite;
                    Debug.LogWarning($"[DialogPanel.CreateChatMessage] Avatar 图片未设置，Timeline: {timeline}，数组长度: {avatarSprites.Length}");
                }
            }
        }
    
        newMessage.transform.SetAsLastSibling();
        Debug.Log($"[DialogPanel.CreateChatMessage] 消息创建完成！");
    }
    
    /* 添加新的聊天图片消息 */
    public static void AddChatImage(byte[] imageData, int timeline, bool publish = true)
    {
        // 创建图片消息
        s_instance.CreateChatImage(imageData, timeline);
        if (publish)
        {
            EventBus.Publish(new ChatImageUpdatedEvent
            {
                imageData = imageData,
                timeline = timeline
            });
        }
    }

    /* 创建单个聊天图片消息 */
    private void CreateChatImage(byte[] imageData, int timeline)
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
                // Convert byte[] to Texture2D
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(imageData))
                {
                    // Create Sprite from Texture2D
                    Sprite image = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    imageComponent.sprite = image;

                    // 设置图片宽度为 405pt，高度根据图片比例动态调整
                    RectTransform rectTransform = imageComponent.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        float aspectRatio = (float)tex.height / tex.width; // 使用 Texture2D 的实际尺寸计算宽高比
                        rectTransform.sizeDelta = new Vector2(405f, 405f * aspectRatio); // 设置宽度为 405，高度根据比例调整
                    }

                    Debug.Log("[DialogPanel.CreateChatImage] 图片设置成功");
                }
                else
                {
                    Debug.LogError("[DialogPanel.CreateChatImage] 图片数据解析失败，无法创建 Texture2D");
                }
            }
        }

        // 根据时间线设置 Background 颜色
        Transform backgroundTransform = newImageMessage.transform.Find("Background");
        if (backgroundTransform != null)
        {
            Image backgroundImage = backgroundTransform.GetComponent<Image>();
            if (backgroundImage != null)
            {
                switch (timeline)
                {
                    case 0: // Ancient - 青绿色
                        backgroundImage.color = new Color(0.4f, 0.8f, 0.6f);
                        break;
                    case 1: // Modern - 黄褐色
                        backgroundImage.color = new Color(0.8f, 0.65f, 0.4f);
                        break;
                    case 2: // Future - 天蓝色
                        backgroundImage.color = new Color(0.53f, 0.81f, 0.92f);
                        break;
                    default:
                        backgroundImage.color = Color.white;
                        break;
                }
                Debug.Log($"[DialogPanel.CreateChatImage] Background 颜色设置成功，Timeline: {timeline}");
            }
        }

        // 根据时间线设置 Avatar 图片
        Transform avatarTransform = backgroundTransform?.Find("Avatar");
        if (avatarTransform != null)
        {
            Image avatarImage = avatarTransform.GetComponent<Image>();
            if (avatarImage != null)
            {
                // 使用 avatarSprites 数组设置头像
                if (timeline >= 0 && timeline < avatarSprites.Length && avatarSprites[timeline] != null)
                {
                    avatarImage.sprite = avatarSprites[timeline];
                    Debug.Log($"[DialogPanel.CreateChatImage] Avatar 图片设置成功，Timeline: {timeline}");
                }
                else
                {
                    avatarImage.sprite = defaultAvatarSprite;
                    Debug.LogWarning($"[DialogPanel.CreateChatImage] Avatar 图片未设置，Timeline: {timeline}，数组长度: {avatarSprites.Length}");
                }
            }
        }

        // 将消息放置在聊天内容的末尾
        newImageMessage.transform.SetAsLastSibling();
    }

    public static void ResetMessage()
    {
        if (s_instance != null)
        {
            // 清空聊天内容，但保留第一条消息
            int childCount = s_instance.chatContent.childCount;
            for (int i = childCount - 1; i >= 1; i--) // 从最后一个开始，保留索引0（第一条消息）
            {
                Destroy(s_instance.chatContent.GetChild(i).gameObject);
            }
            
            s_instance.currentStreamingText = null;
            s_instance.currentStreamingMessageGO = null;
            s_instance.streamQueue.Clear();
            s_instance.isStreaming = false;
        }
    }
    /* 根据 timeline 获取时间线名称 */
    private string GetTimelineName(int timeline)
    {
        switch (timeline)
        {
            case 0: return "Ancient";
            case 1: return "Modern";
            case 2: return "Future";
            default: return "Unknown";
        }
    }
}
