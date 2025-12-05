using UnityEngine;
using Events;
using Mirror;

/*
 * 场景开场剧情触发器
 * 放在场景中，当本地玩家加载后自动播放剧情
 */
public class SceneIntro : MonoBehaviour
{
    [Header("剧情配置")]
    [Tooltip("拖入该场景的开场剧情数据")]
    public DialogueData introDialogue;

    [Tooltip("延迟播放时间（秒），给玩家适应场景的时间")]
    public float delaySeconds = 0.5f;

    [Tooltip("是否只播放一次（防止重复进入场景时再次触发）")]
    public bool playOnce = true;

    private static bool hasPlayed = false; // 静态变量，跨场景保留

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
        if (introDialogue != null)
        {
            Debug.Log($"[SceneIntro] 播放场景剧情: {introDialogue.name}");
            EventBus.LocalPublish(new StartDialogueEvent(introDialogue));

            if (playOnce)
            {
                hasPlayed = true;
            }
        }
        else
        {
            Debug.LogWarning("[SceneIntro] 未设置 introDialogue！");
        }
    }

    // 可选：提供重置方法，用于调试
    [ContextMenu("重置播放状态")]
    void ResetPlayState()
    {
        hasPlayed = false;
        Debug.Log("[SceneIntro] 播放状态已重置");
    }
} 