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

    private bool hasPlayed = false;

    void Awake()
    {
        // 确保开始时面板是隐藏的
        if (gameOverPanel != null)
        {
            // 如果面板里有多余的 EventSystem，销毁它以防报错
            var es = gameOverPanel.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>();
            if (es != null)
            {
                // 立即销毁，防止 Update 报错
                if (es.gameObject == gameOverPanel)
                {
                    DestroyImmediate(es); // 如果组件在 Panel 上，只销毁组件
                }
                else
                {
                    DestroyImmediate(es.gameObject); // 否则销毁子物体
                }
            }
            
            gameOverPanel.SetActive(false);
        }
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
                System.Action<DialogueEndEvent> onEnd = e => finished = true;
                EventBus.Subscribe<DialogueEndEvent>(onEnd);

                while (!finished)
                {
                    yield return null;
                }
                EventBus.Unsubscribe<DialogueEndEvent>(onEnd);
            }
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