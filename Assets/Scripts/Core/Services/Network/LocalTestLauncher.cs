using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

/// <summary>
/// Boot 场景的本地测试入口脚本：
/// - 启用时自动跳过联网与房间创建；
/// - 启动 Mirror Host；
/// - 直接加载 GameBase → Modern 场景。
/// </summary>
public class LocalTestLauncher : MonoBehaviour
{
    [Header("本地测试模式开关")]
    [Tooltip("勾选后启动时会跳过联网逻辑，直接以 Host 身份运行。")]
    public bool enableLocalTestMode = false;

    [Header("本地测试目标时间线")]
    [Tooltip("0=Ancient, 1=Modern, 2=Future")]
    [Range(0, 2)]
    public int testTimelineIndex = 1;

    private EchoNetworkManager nm;
    private bool hasLoadedGameBase = false;

    private void Start()
    {
        // 在 Start 中执行，确保 NetworkManager.Start() 已被调用
        nm = FindFirstObjectByType<EchoNetworkManager>();

        if (!enableLocalTestMode) return;

        if (nm == null)
        {
            Debug.LogError("[LocalTestLauncher] 找不到 EchoNetworkManager！");
            return;
        }

        Debug.Log("[LocalTestLauncher] 启用本地测试模式，启动本地 Host...");

        // 1️⃣ 跳过联网（在 NetworkManager.Start() 之后设置也可以生效）
        nm.skipRelay = true;
        
        // 2️⃣ 注册场景加载回调
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // 3️⃣ 启动 Host
        nm.StartHost();

        // 4️⃣ 手动切换至 GameBase
        Debug.Log("[LocalTestLauncher] 切换到 GameBase 场景...");
        NetworkManager.singleton.ServerChangeScene("GameBase");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!enableLocalTestMode) return;
        
        if (scene.name == "GameBase" && !hasLoadedGameBase)
        {
            hasLoadedGameBase = true;
            Debug.Log($"[LocalTestLauncher] GameBase 场景加载完成，准备分配时间线: {testTimelineIndex}");
            
            // 延迟执行以确保所有对象初始化完成
            StartCoroutine(WaitAndAssignTimeline());
        }
    }

    private System.Collections.IEnumerator WaitAndAssignTimeline()
    {
        float t = 0f;
        while (NetworkClient.localPlayer == null && t < 10f)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (NetworkClient.localPlayer != null)
            AssignTimelineAndLoadScene(NetworkClient.localPlayer);
        else
            Debug.LogWarning("[LocalTestLauncher] LocalPlayer 等待超时，未生成。");
    }

    private void AssignTimelineAndLoadScene(NetworkIdentity player)
    {
        var tp = player.GetComponent<TimelinePlayer>();
        if (tp == null)
        {
            Debug.LogError("[LocalTestLauncher] 本地玩家缺少 TimelinePlayer 组件！");
            return;
        }

        tp.timeline = testTimelineIndex;
        Debug.Log($"[LocalTestLauncher] 已为本地玩家分配时间线 {testTimelineIndex}");

        // 5️⃣ 直接加载对应时间线场景
        SceneDirector.Instance.TryLoadTimelineNow();
    }

    private void OnDestroy()
    {
        // 清理回调
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
