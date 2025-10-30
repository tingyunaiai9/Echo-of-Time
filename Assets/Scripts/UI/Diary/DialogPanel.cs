using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Events;

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
    private const string ApiKey = "sk-c05934a1774344c29ca7be049fb92741"; // TODO: 替换为你的 DeepSeek API Key

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
    private async void OnSendButtonClicked()
    {
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;

        string userInput = inputField.text.Trim();
        inputField.text = ""; // 清空输入框

        // 添加用户输入的消息到聊天面板
        AddChatMessage(userInput, MessageType.Modern, publish: false);

        // 调用 DeepSeek API 获取响应
        string deepSeekResponse = await CallDeepSeekApi(userInput);

        if (!string.IsNullOrEmpty(deepSeekResponse))
        {
            // 添加 DeepSeek 的响应到聊天面板
            AddChatMessage(deepSeekResponse, MessageType.Future, publish: true);
        }
    }

    /* 调用 DeepSeek API */
    private async Task<string> CallDeepSeekApi(string prompt)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // 设置授权头
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

                // 构造请求体
                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个友好的 AI 助手，帮助用户解答问题。" },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1000
                };

                // 序列化为 JSON
                string jsonBody = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // 发送 POST 请求
                HttpResponseMessage response = await client.PostAsync(DeepSeekApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    // 解析响应
                    var responseObj = JsonConvert.DeserializeObject<DeepSeekResponse>(responseBody);
                    
                    if (responseObj?.choices != null && responseObj.choices.Length > 0)
                    {
                        return responseObj.choices[0].message.content;
                    }
                    else
                    {
                        Debug.LogWarning("[DialogPanel] DeepSeek API 返回的响应格式不正确");
                        return "抱歉，无法获取有效的响应。";
                    }
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"[DialogPanel] DeepSeek API 请求失败，状态码: {response.StatusCode}, 错误信息: {errorBody}");
                    return "抱歉，AI 服务暂时不可用。";
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DialogPanel] 调用 DeepSeek API 时发生异常: {ex.Message}");
                return "抱歉，发生了错误。";
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

        // 设置消息类型文本
        Transform typeTextTransform = newMessage.transform.Find("TypeText");
        if (typeTextTransform != null)
        {
            TMP_Text typeText = typeTextTransform.GetComponent<TMP_Text>();
            if (typeText != null)
            {
                typeText.text = type.ToString(); // 显示消息类型
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

/* DeepSeek API 响应数据结构 */
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