using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Events;

public class VisualNovelPanel : MonoBehaviour
{
    [Header("UI 组件引用")]
    public GameObject panelRoot; // 整个对话框的父节点
    public TextMeshProUGUI nameText; // 名字文本
    public TextMeshProUGUI contentText; // 内容文本
    public Image leftPortrait; // 左侧立绘
    public Image backgroundImage; // 剧情背景（位于剧情之下、场景之上）
    public Button continueButton; // 点击继续的全屏按钮
    public Button skipButton; // 跳过剧情按钮

    [Header("角色默认立绘")]
    public Sprite modernSprite; // 叙白
    public Sprite ancientSprite; // 云归
    public Sprite futureSprite; // 恨水

    private Queue<DialogueLine> _currentLines = new Queue<DialogueLine>();
    private bool _isTyping = false;
    private string _targetContent = "";
    private Vector2 _originalContentPosition;
    private Vector2 _originalContentSizeDelta;
    private Sprite _lastBackgroundSprite;

    // 单例方便调用
    public static VisualNovelPanel Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        _originalContentPosition = contentText.rectTransform.anchoredPosition;
        _originalContentSizeDelta = contentText.rectTransform.sizeDelta;
        panelRoot.SetActive(false);
        continueButton.onClick.AddListener(OnContinueClicked);
        skipButton.onClick.AddListener(EndDialogue);
        
        // 订阅剧情开始事件
        EventBus.Subscribe<StartDialogueEvent>(OnStartDialogue);
        Debug.Log("VisualNovelPanel 已初始化并订阅 StartDialogueEvent");
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<StartDialogueEvent>(OnStartDialogue);
    }

    void Update()
    {
        // 空格键推进到下一句（或快速完成打字）
        if (panelRoot != null && panelRoot.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            OnContinueClicked();
        }
    }

    // 事件回调
    private void OnStartDialogue(StartDialogueEvent evt)
    {
        Debug.Log("OnStartDialogue() - 已收到开始剧情事件！");
        UIManager.Instance?.SetFrozen(true); // 暂停游戏
        StartDialogue(evt.data);
    }

    public void StartDialogue(DialogueData data)
    {
        if (data == null) return;

        panelRoot.SetActive(true);
        _currentLines.Clear();

        foreach (var line in data.lines)
        {
            _currentLines.Enqueue(line);
        }

        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        if (_currentLines.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = _currentLines.Dequeue();
        // 设置背景图：本行有则用本行，没有则沿用上一行
        if (backgroundImage != null)
        {
            // 如果勾选了清空背景，则隐藏背景并重置记录
            if (line.clearBackground)
            {
                backgroundImage.gameObject.SetActive(false);
                _lastBackgroundSprite = null;
            }
            else
            {
                Sprite bg = line.backgroundSprite != null ? line.backgroundSprite : _lastBackgroundSprite;
                if (bg != null)
                {
                    backgroundImage.sprite = bg;
                    backgroundImage.gameObject.SetActive(true);
                    _lastBackgroundSprite = bg;
                }
                else
                {
                    backgroundImage.gameObject.SetActive(false);
                }
            }
        }

        if (line.isNarration)
        {
            // 是叙述，隐藏名字和立绘，调整内容框
            nameText.gameObject.SetActive(false);
            leftPortrait.gameObject.SetActive(false);

            var rectTransform = contentText.rectTransform;
            rectTransform.anchoredPosition = new Vector2(_originalContentPosition.x - 200f, _originalContentPosition.y + 30f);
            rectTransform.sizeDelta = new Vector2(_originalContentSizeDelta.x + 200f, _originalContentSizeDelta.y);
        }
        else
        {
            // 是对话，恢复原样
            nameText.gameObject.SetActive(true);
            contentText.rectTransform.anchoredPosition = _originalContentPosition;
            contentText.rectTransform.sizeDelta = _originalContentSizeDelta;
            
            // 设置名字
            nameText.text = line.speakerName;

            // 设置立绘
            UpdatePortraits(line);
        }

        // 打字机效果
        StopAllCoroutines();
        StartCoroutine(TypeContent(line.content));
    }

    void UpdatePortraits(DialogueLine line)
    {
        Sprite spriteToDisplay = null;

        // 根据名字匹配预设的立绘
        switch (line.speakerName)
        {
            case "叙白":
                spriteToDisplay = modernSprite;
                break;
            case "云归":
                spriteToDisplay = ancientSprite;
                break;
            case "恨水":
                spriteToDisplay = futureSprite;
                break;
            default:
                // 如果不是预设的名字，则使用在对话行中单独指定的立绘
                spriteToDisplay = line.characterSprite;
                break;
        }

        // 如果有可显示的立绘，则更新UI
        if (spriteToDisplay != null)
        {
            leftPortrait.sprite = spriteToDisplay;
            leftPortrait.gameObject.SetActive(true);
            leftPortrait.color = Color.white; // 亮起
        }
        else
        {
            // 否则隐藏立绘
            leftPortrait.gameObject.SetActive(false);
        }
    }

    IEnumerator TypeContent(string content)
    {
        _isTyping = true;
        _targetContent = content;
        contentText.text = "";

        foreach (char c in content)
        {
            contentText.text += c;
            yield return new WaitForSeconds(0.05f); // 打字速度
        }

        _isTyping = false;
    }

    void OnContinueClicked()
    {
        // 如果正在打字，点击则瞬间显示全
        if (_isTyping)
        {
            StopAllCoroutines();
            contentText.text = _targetContent;
            _isTyping = false;
        }
        else
        {
            // 否则显示下一句
            DisplayNextLine();
        }
    }

    void EndDialogue()
    {
        panelRoot.SetActive(false);
        Debug.Log("剧情结束");
        // 这里可以恢复玩家控制
        UIManager.Instance?.SetFrozen(false);
        EventBus.LocalPublish(new DialogueEndEvent());
    }
}