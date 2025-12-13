/* Core/SceneDirector.cs
 * 场景管理器，负责游戏场景的加载、切换与时间线场景管理
 * 
 * 主要功能：
 * - 启动时加载 StartPage（角色选择界面）
 * - 管理在线主场景（GameBase）的加载
 * - 根据玩家时间线自动加载对应场景（Ancient/Modern/Future）
 * - 支持本地测试模式和联网模式的自动切换
 * - 处理场景加载完成后的回调逻辑
 */

using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * 场景管理器单例类，统筹所有场景的加载与切换逻辑
 */
public class SceneDirector : Singleton<SceneDirector>
{
    private bool timelineLoaded = false;

    [Header("Scene Names")]
    [SerializeField] private string bootScene = "Boot";
    [SerializeField] private string startPageScene = "StartPage";
    public string onlineMainScene = "GameBase";
    public string plotScene = "Plot";
    [Tooltip("Prefixes for timeline scenes. Final name will be Prefix + Level (e.g. Ancient1)")]
    [SerializeField] private string[] timelineScenePrefixes = new string[] { "Ancient", "Modern", "Future" };

    private string currentLoadedTimelineScene = "";

    [Header("Options")]
    [SerializeField] private bool loadStartPageOnBoot = true;
    [SerializeField] private bool autoLoadTimelineOnSceneLoaded = true;
    [Tooltip("如果为 true，在本地测试模式（skipRelay）下不自动加载时间线场景，由 LocalTestLauncher 负责")]
    [SerializeField] private bool skipAutoLoadInLocalTest = true;

    private bool isLoadingTimeline = false;

    /*
     * Unity 生命周期：初始化时加载 StartPage 并注册场景加载回调
     */
    private void Start()
    {
        if (loadStartPageOnBoot)
        {
            StartCoroutine(LoadStartPageAdditive());
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /*
     * 尝试立即加载时间线场景（需要本地玩家已分配时间线）
     */
    public void TryLoadTimelineNow()
    {
        // 仅在在线主场景下加载时间线场景
        if (SceneManager.GetActiveScene().name != onlineMainScene) return;

        var lp = NetworkClient.localPlayer;
        if (lp == null) return;

        var tp = lp.GetComponent<TimelinePlayer>();
        if (tp == null || tp.timeline < 0) return;

        string sceneName = GetSceneName(tp.timeline, tp.currentLevel);
        if (string.IsNullOrEmpty(sceneName)) return;

        // 1. 检查是否正在加载这个场景
        if (currentLoadedTimelineScene == sceneName && isLoadingTimeline) return;

        // 2. 检查场景是否已经加载完毕
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid() && loadedScene.isLoaded) 
        {
            // 确保记录正确
            currentLoadedTimelineScene = sceneName;
            return;
        }

        // 3. 如果记录的是其他场景，先卸载
        if (!string.IsNullOrEmpty(currentLoadedTimelineScene) && currentLoadedTimelineScene != sceneName)
        {
            UnloadSceneIfLoaded(currentLoadedTimelineScene);
        }

        // 4. 开始加载
        StartCoroutine(LoadTimelineSceneRoutine(sceneName));
    }

    private IEnumerator LoadTimelineSceneRoutine(string sceneName)
    {
        isLoadingTimeline = true;
        currentLoadedTimelineScene = sceneName; // 提前占位，防止重复触发
        Debug.Log($"[SceneDirector] Loading timeline scene: {sceneName}");

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return op;

        isLoadingTimeline = false;
        
        // 将在线主场景设为 Active，时间线场景只是内容补充
        Scene online = SceneManager.GetSceneByName(onlineMainScene);
        if (online.IsValid()) SceneManager.SetActiveScene(online);

        // 场景加载完成后，重置玩家位置到 SpawnPoint
        if (TimelinePlayer.Local != null)
        {
            TimelinePlayer.Local.TriggerResetPosition();
        }
    }

    public string GetSceneName(int timeline, int level)
    {
        if (timeline < 0 || timeline >= timelineScenePrefixes.Length) return "";
        return $"{timelineScenePrefixes[timeline]}{level}";
    }

    /*
     * 协程：以叠加模式加载 StartPage 场景并设为活动场景
     */
    private IEnumerator LoadStartPageAdditive()
    {
        if (!SceneManager.GetSceneByName(startPageScene).isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(startPageScene, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
        }
        Scene start = SceneManager.GetSceneByName(startPageScene);
        if (start.IsValid()) SceneManager.SetActiveScene(start);
    }

    /*
     * StartPage 上 Confirm 按钮调用，仅 Host/Server 触发场景切换
     * 将所有客户端统一切换到在线主场景（GameBase）
     */
    public void StartGameFromStartPage()
    {
        // 只允许 Host/Server 触发场景切换，避免客户端各自切换造成不同步
        if (!NetworkServer.active)
        {
            Debug.LogWarning("[SceneDirector] StartGameFromStartPage ignored: only Host/Server may start the game.");
            return;
        }

        // 直接进入在线主场景（GameBase），跳过 Plot 场景（改为 Panel 遮罩）
        Debug.Log("[SceneDirector] Server changing scene to Online Main scene...");
        NetworkManager.singleton.ServerChangeScene(onlineMainScene);
    }

    /*
     * 场景加载完成时的回调处理
     * - 检测本地测试模式，决定是否自动加载时间线场景
     * - 卸载 StartPage 场景
     * - 触发时间线场景加载逻辑
     */
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果加载的是 Plot 场景（或者 Boot, StartPage 等非 GameBase 主场景）
        // 且 mode 是 Single（意味着替换了主场景）
        if (mode == LoadSceneMode.Single)
        {
            if (scene.name != onlineMainScene)
            {
                currentLoadedTimelineScene = "";
                isLoadingTimeline = false;
            }
        }

        // 当在线主场景加载完成时（所有端都会走到这里）
        if (scene.name == onlineMainScene)
        {
            // 检查是否为本地测试模式
            if (skipAutoLoadInLocalTest)
            {
                var nm = FindFirstObjectByType<EchoNetworkManager>();
                if (nm != null && nm.skipRelay)
                {
                    Debug.Log("[SceneDirector] 本地测试模式：跳过自动加载时间线场景，由 LocalTestLauncher 负责");
                    // 仍然卸载 StartPage（如果需要）
                    UnloadSceneIfLoaded(startPageScene);
                    return;
                }
            }

            // 卸载 StartPage（如果还在）
            UnloadSceneIfLoaded(startPageScene);

            // 在每个客户端本地，按本地玩家的 timeline 加载对应时间线场景
            if (NetworkClient.isConnected && autoLoadTimelineOnSceneLoaded)
            {
                // 启动协程等待并加载，协程内部会调用 TryLoadTimelineNow
                StartCoroutine(WaitAndLoadTimelineScene());
            }
        }
    }

    /*
     * 协程：等待本地玩家生成并分配时间线后，加载对应时间线场景
     * 超时时间：20 秒
     */
    private IEnumerator WaitAndLoadTimelineScene()
    {
        // 等待本地玩家生成并且 Timeline 分配完成（TimelinePlayer.timeline >= 0）
        TimelinePlayer local = null;
        float timeout = 20f;
        float t = 0f;

        while (t < timeout)
        {
            if (NetworkClient.localPlayer != null)
            {
                local = NetworkClient.localPlayer.GetComponent<TimelinePlayer>();
                if (local != null && local.timeline >= 0) break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        if (local == null || local.timeline < 0)
        {
            Debug.LogWarning("[SceneDirector] Local timeline not ready, skip timeline scene loading.");
            yield break;
        }

        // 调用统一的加载方法
        TryLoadTimelineNow();
    }

    /*
     * 卸载指定名称的场景（如果已加载）
     */
    private void UnloadSceneIfLoaded(string name)
    {
        var sc = SceneManager.GetSceneByName(name);
        if (sc.isLoaded)
        {
            SceneManager.UnloadSceneAsync(sc);
        }
    }
}
