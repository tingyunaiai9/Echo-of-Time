using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    [Tooltip("剧情背景（可选，不设则沿用上一个）")]
    public Sprite backgroundSprite;

    [Tooltip("是否为叙述（将隐藏名字和立绘）")]
    public bool isNarration = false;

    [Tooltip("说话者名字")]
    public string speakerName;
    
    [Tooltip("对话内容")]
    [TextArea(3, 5)]
    public string content;

    [Tooltip("角色立绘（可选）")]
    public Sprite characterSprite;

    [Tooltip("是否清空当前背景图")]
    public bool clearBackground = false;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/DialogueData")]
public class DialogueData : ScriptableObject
{
    public List<DialogueLine> lines;
}