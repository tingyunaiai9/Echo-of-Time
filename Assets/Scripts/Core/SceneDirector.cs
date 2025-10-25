// Core/Scene/SceneDirector.cs
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDirector : MonoBehaviour
{
    public static SceneDirector Instance { get; private set; }

    private bool timelineLoaded = false;

    [Header("Scene Names")]
    [SerializeField] private string bootScene = "Boot";
    [SerializeField] private string startPageScene = "StartPage";
    [SerializeField] private string onlineMainScene = "Boot";
    [SerializeField] private string[] timelineToScene = new string[] { "Ancient", "Modern", "Future" };

    [Header("Options")]
    [SerializeField] private bool loadStartPageOnBoot = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    private void Start()
    {
        if (loadStartPageOnBoot)
        {
            StartCoroutine(LoadStartPageAdditive());
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void TryLoadTimelineNow()
    {
        if (timelineLoaded) return;
        var lp = NetworkClient.localPlayer;
        if (lp == null) return;

        var tp = lp.GetComponent<TimelinePlayer>();
        if (tp == null || tp.timeline < 0) return;

        timelineLoaded = true;
        var tl = Mathf.Clamp(tp.timeline, 0, timelineToScene.Length - 1);
        var sceneName = timelineToScene[tl];
        Debug.Log($"[SceneDirector] Loading timeline scene: {sceneName}");
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

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

    /// <summary>
    /// StartPage 上 Confirm 按钮调用。仅 Host/Server 触发，统一换到在线主场景。
    /// </summary>
    public void StartGameFromStartPage()
    {
        // 只允许 Host/Server 触发场景切换，避免客户端各自切换造成不同步
        if (!NetworkServer.active)
        {
            Debug.LogWarning("[SceneDirector] StartGameFromStartPage ignored: only Host/Server may start the game.");
            return;
        }

        // 统一换到在线主场景（GameBase）。Mirror 会把所有客户端一起带过去。
        Debug.Log("[SceneDirector] Server changing scene to online main scene...");
        NetworkManager.singleton.ServerChangeScene(onlineMainScene);

        // （可选）也可以先卸载 StartPage：等在线场景加载后由 OnSceneLoaded 里再处理
    }

    /// <summary>
    /// 场景加载完成时回调。
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 当在线主场景加载完成时（所有端都会走到这里）
        if (scene.name == onlineMainScene)
        {
            // 卸载 StartPage（如果还在）
            UnloadSceneIfLoaded(startPageScene);

            // 在每个客户端本地，按本地玩家的 timeline 加载对应时间线场景
            if (NetworkClient.isConnected)
            {
                TryLoadTimelineNow();
                StartCoroutine(WaitAndLoadTimelineScene());
            }
        }
    }

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

        // 映射 timeline → 具体场景名
        int tl = Mathf.Clamp(local.timeline, 0, timelineToScene.Length - 1);
        string sceneName = timelineToScene[tl];

        // 已在则不重复加载
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.Log($"[SceneDirector] Loading timeline scene: {sceneName}");
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
        }

        // 将在线主场景设为 Active，时间线场景只是内容补充
        Scene online = SceneManager.GetSceneByName(onlineMainScene);
        if (online.IsValid()) SceneManager.SetActiveScene(online);
    }

    private void UnloadSceneIfLoaded(string name)
    {
        var sc = SceneManager.GetSceneByName(name);
        if (sc.isLoaded)
        {
            SceneManager.UnloadSceneAsync(sc);
        }
    }
}
