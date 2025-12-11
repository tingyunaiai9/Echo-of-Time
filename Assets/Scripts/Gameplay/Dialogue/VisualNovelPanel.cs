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
    public Image rightPortrait; // 右侧立绘
    public Button continueButton; // 点击继续的全屏按钮

    private Queue<DialogueLine> _currentLines = new Queue<DialogueLine>();
    private bool _isTyping = false;
    private string _targetContent = "";

    // 单例方便调用
    public static VisualNovelPanel Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        panelRoot.SetActive(false);
        continueButton.onClick.AddListener(OnContinueClicked);
        
        // 订阅剧情开始事件
        EventBus.Subscribe<StartDialogueEvent>(OnStartDialogue);
        Debug.Log("VisualNovelPanel 已初始化并订阅 StartDialogueEvent");
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<StartDialogueEvent>(OnStartDialogue);
    }

    // 事件回调
    private void OnStartDialogue(StartDialogueEvent evt)
    {
        Debug.Log("OnStartDialogue() - 已收到开始剧情事件！");
        EventBus.LocalPublish(new FreezeEvent {isOpen = true}); // 暂停游戏
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
        
        // 设置名字
        nameText.text = line.speakerName;

        // 设置立绘
        UpdatePortraits(line);

        // 打字机效果
        StopAllCoroutines();
        StartCoroutine(TypeContent(line.content));
    }

    void UpdatePortraits(DialogueLine line)
    {
        // 简单的立绘逻辑：有图就显示，没图就隐藏
        if (line.characterSprite != null)
        {
            if (line.isLeft)
            {
                leftPortrait.sprite = line.characterSprite;
                leftPortrait.gameObject.SetActive(true);
                leftPortrait.color = Color.white; // 亮起
                rightPortrait.color = Color.gray; // 另一侧变暗
            }
            else
            {
                rightPortrait.sprite = line.characterSprite;
                rightPortrait.gameObject.SetActive(true);
                rightPortrait.color = Color.white;
                leftPortrait.color = Color.gray;
            }
        }
        // 如果不需要立绘变化，可以保留上一张，或者根据需求隐藏
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
        // 这里可以发送一个剧情结束事件，恢复玩家控制
        EventBus.LocalPublish(new FreezeEvent {isOpen = false});
    }
}