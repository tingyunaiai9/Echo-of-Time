using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
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

    private const string DeepSeekApiUrl = "https://api.deepseek.com/generate"; // 替换为实际的 DeepSeek API URL

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

        EventBus.Instance.Subscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);
    }

    void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
        }

        EventBus.Instance.Unsubscribe<ChatMessageUpdatedEvent>(OnChatMessageUpdated);
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
                // 构造请求内容
                var requestData = new Dictionary<string, string>
                {
                    { "prompt", prompt }
                };
                var content = new FormUrlEncodedContent(requestData);

                // 发送 POST 请求
                HttpResponseMessage response = await client.PostAsync(DeepSeekApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody; // 假设 API 返回的是纯文本
                }
                else
                {
                    Debug.LogError($"[DialogPanel] DeepSeek API 请求失败，状态码: {response.StatusCode}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DialogPanel] 调用 DeepSeek API 时发生异常: {ex.Message}");
            }
        }

        return null;
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