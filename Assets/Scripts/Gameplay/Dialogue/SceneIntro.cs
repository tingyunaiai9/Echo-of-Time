using UnityEngine;
using Events;
using Mirror;
using System.Collections.Generic;

/*
 * 场景开场剧情触发器
 * 放在场景中，当本地玩家加载后自动播放剧情
 */
public class SceneIntro : MonoBehaviour
{
    [Header("剧情配置")]
    [Tooltip("拖入该场景的开场剧情数据（支持多个，依次播放）")]
    public List<DialogueData> introDialogues = new List<DialogueData>();

    [Tooltip("延迟播放时间（秒），给玩家适应场景的时间")]
    public float delaySeconds = 0.5f;

    [Tooltip("是否只播放一次（防止重复进入场景时再次触发）")]
    public bool playOnce = true;

    [Header("结束配置")]
    [Tooltip("剧情播放完毕后显示的 Game Over 面板")]
    public GameObject gameOverPanel;

    [Header("彩蛋配置")]
    [Tooltip("EndPanel 显示后延迟播放的彩蛋剧情")]
    public DialogueData easterEggDialogue;
    [Tooltip("彩蛋延迟时间")]
    public float easterEggDelay = 5f;
    [Tooltip("彩蛋结束后显示的最终面板")]
    public GameObject finalGameOverPanel;

    private bool hasPlayed = false;

    void Awake()
    {
        // 确保开始时面板是隐藏的
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (finalGameOverPanel != null) finalGameOverPanel.SetActive(false);
    }

    void Start()
    {
        // 检查是否已播放过
        if (playOnce && hasPlayed)
        {
            Debug.Log($"[SceneIntro] 场景剧情已播放过，跳过");
            return;
        }

        // 等待本地玩家加载完成后播放
        StartCoroutine(WaitForPlayerAndPlay());
    }

    System.Collections.IEnumerator WaitForPlayerAndPlay()
    {
        // 等待本地玩家存在
        while (NetworkClient.localPlayer == null)
        {
            yield return null;
        }

        // 额外延迟，让场景稳定
        yield return new WaitForSeconds(delaySeconds);

        // 触发剧情
        if (introDialogues != null && introDialogues.Count > 0)
        {
            foreach (var dialogue in introDialogues)
            {
                if (dialogue == null) continue;

                Debug.Log($"[SceneIntro] 播放场景剧情: {dialogue.name}");
                EventBus.LocalPublish(new StartDialogueEvent(dialogue));

                // 等待剧情结束
                bool finished = false;
                System.Action<DialogueEndEvent> onEnd = e => {finished = true;};
                EventBus.Subscribe<DialogueEndEvent>(onEnd);

                while (!finished)
                {
                    yield return null;
                }
                EventBus.Unsubscribe<DialogueEndEvent>(onEnd);
            }
            EventBus.LocalPublish(new IntroEndEvent());
        }
        else
        {
            Debug.LogWarning("[SceneIntro] 未设置 introDialogues！");
        }

        // 剧情播放完毕，显示 Game Over 面板
        if (gameOverPanel != null)
        {
            Debug.Log("[SceneIntro] 剧情结束，显示 Game Over 面板");
            gameOverPanel.SetActive(true);

            // 等待 EndPanelController 完成两张图片的展示
            var endPanelController = gameOverPanel.GetComponent<EndPanelController>();
            if (endPanelController != null && easterEggDialogue != null)
            {
                bool panelFinished = false;
                endPanelController.OnSecondImageTimeout += () => { panelFinished = true; };
                while (!panelFinished) yield return null;
            }
            else
            {
                // 如果没有 EndPanelController，使用原逻辑等待固定时间
                yield return new WaitForSeconds(easterEggDelay);
            }
        }

        // 彩蛋逻辑：如果有配置彩蛋剧情，则播放
        if (easterEggDialogue != null)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            Debug.Log("[SceneIntro] 触发彩蛋剧情");
            EventBus.LocalPublish(new StartDialogueEvent(easterEggDialogue));

            // 等待彩蛋剧情结束
            bool easterEggFinished = false;
            System.Action<DialogueEndEvent> onEasterEggEnd = e => { easterEggFinished = true; };
            EventBus.Subscribe<DialogueEndEvent>(onEasterEggEnd);
            while (!easterEggFinished) yield return null;
            EventBus.Unsubscribe<DialogueEndEvent>(onEasterEggEnd);

            // 显示最终面板
            if (finalGameOverPanel != null)
            {
                Debug.Log("[SceneIntro] 彩蛋结束，显示最终面板");
                finalGameOverPanel.SetActive(true);
            }
        }

        if (playOnce)
        {
            hasPlayed = true;
        }
    }

    // 提供重置方法，用于调试
    [ContextMenu("重置播放状态")]
    void ResetPlayState()
    {
        hasPlayed = false;
        Debug.Log("[SceneIntro] 播放状态已重置");
    }
}