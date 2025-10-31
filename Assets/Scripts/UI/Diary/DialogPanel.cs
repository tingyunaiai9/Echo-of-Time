using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Events;
using System.IO;
using System.Collections;

public class DialogPanel : MonoBehaviour
{
    [Tooltip("聊天消息预制体（包含MessageText和TypeText两个子对象）")]
    public GameObject chatMessagePrefab;

    [Tooltip("聊天消息容器（Vertical Layout Group）")]
    public Transform contentParent;

    [Tooltip("输入框组件")]
    public TMP_InputField inputField;

    [Tooltip("发送按钮")]
    public Button sendButton;

    private static DialogPanel s_instance;

    // DeepSeek API 配置
    private const string DeepSeekApiUrl = "https://api.deepseek.com/v1/chat/completions";
    private const string ApiKey = "sk-c05934a1774344c29ca7be049fb92741";

    // 当前流式输出的消息对象
    private TMP_Text currentStreamingText;
    private Queue<string> streamQueue = new Queue<string>(); // 使用队列存储流式数据
    private bool isStreaming = false;

    // 静态共享的 HttpClient 实例
    private static HttpClient s_sharedClient;

    void Awake()
    {
        s_instance = this;
        if (contentParent == null)
        {
            contentParent = transform.Find("Panel/LeftPanel/ChatScrollView/Viewport/Content");
        }

        if (inputField == null)
        {
            inputField = transform.Find("Panel/LeftPanel/InputPanel/InputField").GetComponent<TMP_InputField>();
            if (inputField == null)
            {
                Debug.LogWarning("[DialogPanel.Awake] 未找到输入框组件");
            }
        }

        if (sendButton == null)
        {
            sendButton = transform.Find("Panel/LeftPanel/InputPanel/InputField/SendButton").GetComponent<Button>();
            if (sendButton == null)
            {
                Debug.LogWarning("[DialogPanel.Awake] 未找到发送按钮组件");
            }
        }

        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }

        EventBus.SafeSubscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);

        // 初始化 HttpClient
        if (s_sharedClient == null)
        {
            s_sharedClient = new HttpClient();
            s_sharedClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
            s_sharedClient.Timeout = System.TimeSpan.FromMinutes(2);

            // 预热连接，在后台发送一个测试请求
            Task.Run(async () =>
            {
                try
                {
                    Debug.Log("[DialogPanel] 开始预热连接...");
                    // 发送简单的请求
                    var warmupBody = new
                    {
                        model = "deepseek-chat",
                        messages = new[] { new { role = "user", content = "hi" } },
                        max_tokens = 1,
                        stream = false
                    };

                    string jsonBody = JsonConvert.SerializeObject(warmupBody);
                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    await s_sharedClient.PostAsync(DeepSeekApiUrl, content);

                    Debug.Log("[DialogPanel] 连接预热完成");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[DialogPanel] 连接预热失败: {ex.Message}");
                }
            });
        }
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

        // 添加用户输入的消息
        AddChatMessage(userInput, MessageType.Modern, publish: false);

        // 预创建 AI 响应的消息框
        CreateStreamingMessage(MessageType.Future);

        // 启动协程处理流式输出
        StartCoroutine(StreamingCoroutine(userInput));
    }

    /* 流式输出协程（在主线程执行） */
    private IEnumerator StreamingCoroutine(string prompt)
    {
        isStreaming = true;
        StringBuilder fullResponse = new StringBuilder();

        // 在后台线程启动 API 调用
        Task apiTask = Task.Run(async () =>
        {
            await CallDeepSeekApiStreaming(prompt);
        });

        // 在主线程处理队列中的流式数据
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

        // 发布完成事件
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
        if (contentParent == null || chatMessagePrefab == null) return;

        GameObject currentStreamingMessage = Instantiate(chatMessagePrefab, contentParent);
        currentStreamingMessage.transform.SetAsLastSibling();

        // 获取 MessageText 组件
        Transform messageTextTransform = currentStreamingMessage.transform.Find("MessageText");
        if (messageTextTransform != null)
        {
            currentStreamingText = messageTextTransform.GetComponent<TMP_Text>();
            if (currentStreamingText != null)
            {
                currentStreamingText.text = "俺在思考……";
            }
        }

        // 设置类型标签
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

    /* 调用 DeepSeek API，在后台线程执行 */
    private async Task CallDeepSeekApiStreaming(string prompt)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
                client.Timeout = System.TimeSpan.FromMinutes(2);

                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个友好的 AI 助手，帮助用户解答问题。" },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1000,
                    stream = true
                };

                string jsonBody = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] 开始发送 API 请求");

                HttpResponseMessage response = await client.PostAsync(DeepSeekApiUrl, content);

                Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] 收到响应头，状态码: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] 开始读取流式数据");
                        while (!reader.EndOfStream)
                        {
                            string line = await reader.ReadLineAsync();

                            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

                            string jsonData = line.Substring(6);

                            if (jsonData.Trim() == "[DONE]")
                            {
                                Debug.Log("[DialogPanel] 流式输出完成");
                                break;
                            }

                            try
                            {
                                var chunk = JsonConvert.DeserializeObject<StreamChunk>(jsonData);
                                if (chunk?.choices != null && chunk.choices.Length > 0)
                                {
                                    string deltaContent = chunk.choices[0].delta?.content;
                                    if (!string.IsNullOrEmpty(deltaContent))
                                    {
                                        // 将内容加入队列（协程会在主线程消费）
                                        streamQueue.Enqueue(deltaContent);
                                        
                                        Debug.Log($"[DialogPanel] 收到内容: {deltaContent}");
                                    }
                                }
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogWarning($"[DialogPanel] 解析失败: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"[DialogPanel] API 请求失败: {response.StatusCode}, 错误: {errorBody}");
                    streamQueue.Enqueue("抱歉，AI 服务暂时不可用。");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DialogPanel] 调用 API 异常: {ex.Message}");
                streamQueue.Enqueue("抱歉，发生了错误。");
            }
            finally
            {
                isStreaming = false; // 标记流式输出结束
            }
        }
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
        if (contentParent == null || chatMessagePrefab == null) return;
        GameObject newMessage = Instantiate(chatMessagePrefab, contentParent);

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

/* 流式响应数据结构 */
[System.Serializable]
public class StreamChunk
{
    public string id;
    public string @object;
    public long created;
    public string model;
    public StreamChoice[] choices;
}

[System.Serializable]
public class StreamChoice
{
    public int index;
    public Delta delta;
    public string finish_reason;
}

[System.Serializable]
public class Delta
{
    public string role;
    public string content;
}

/* 原有的完整响应结构 */
[System.Serializable]
public class DeepSeekResponse
{
    public string id;
    public string @object;
    public long created;
    public string model;
    public Choice[] choices;
    public Usage usage;
}

[System.Serializable]
public class Choice
{
    public int index;
    public Message message;
    public string finish_reason;
}

[System.Serializable]
public class Message
{
    public string role;
    public string content;
}

[System.Serializable]
public class Usage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}