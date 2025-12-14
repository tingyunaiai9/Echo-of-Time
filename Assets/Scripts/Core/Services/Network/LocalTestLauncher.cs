/*
 * File: LocalTestLauncher.cs
 * Path: Assets/Scripts/Core/Services/Network/LocalTestLauncher.cs
 * 
 * Purpose: Boot 场景的本地测试入口脚本
 * 
 * Features:
 * - 启用时自动跳过联网与房间创建
 * - 启动 Mirror Host
 * - 直接加载 GameBase → 指定时间线场景
 * 
 * Usage:
 * - 挂载到 Boot 场景的 GameObject 上
 * - 勾选 enableLocalTestMode 启用本地测试
 * - 设置 testTimelineIndex 指定目标时间线（0=Ancient, 1=Modern, 2=Future）
 */

using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

/*
 * 本地测试启动器
 * 用于开发阶段快速测试单个时间线，跳过联网流程
 */
public class LocalTestLauncher : MonoBehaviour
{
    // 本地测试模式开关（勾选后启动时会跳过联网逻辑，直接以 Host 身份运行）
    [Header("本地测试模式开关")]
    [Tooltip("勾选后启动时会跳过联网逻辑，直接以 Host 身份运行。")]
    public bool enableLocalTestMode = false;

    // 本地测试目标时间线（0=Ancient, 1=Modern, 2=Future）
    [Header("本地测试目标时间线")]
    [Tooltip("0=Ancient, 1=Modern, 2=Future")]
    [Range(0, 2)]
    public int testTimelineIndex = 1;

    // 本地测试目标层级（1=Level1, 2=Level2, ...）
    [Header("本地测试目标层级")]
    [Tooltip("1=Level1, 2=Level2, ...")]
    [Min(1)]
    public int testLevelIndex = 1;

    private EchoNetworkManager nm;
    private bool hasLoadedGameBase = false;

    /*
     * Unity 生命周期：启动本地测试流程
     * 在 Start 中执行，确保 NetworkManager.Start() 已被调用
     */
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

    /*
     * 场景加载回调：检测 GameBase 加载完成并分配时间线
     * @param scene 已加载的场景
     * @param mode 场景加载模式
     */
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

    /*
     * 等待本地玩家生成并分配时间线
     * 协程：最多等待 10 秒，直到 NetworkClient.localPlayer 创建完成
     */
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

    /*
     * 分配时间线并加载对应场景
     * @param player 本地玩家的 NetworkIdentity
     */
    private void AssignTimelineAndLoadScene(NetworkIdentity player)
    {
        var tp = player.GetComponent<TimelinePlayer>();
        if (tp == null)
        {
            Debug.LogError("[LocalTestLauncher] 本地玩家缺少 TimelinePlayer 组件！");
            return;
        }

        tp.currentLevel = testLevelIndex;
        tp.timeline = testTimelineIndex;
        
        Debug.Log($"[LocalTestLauncher] 已为本地玩家分配时间线 {testTimelineIndex}, 层级 {testLevelIndex}");

        // 5️⃣ 尝试加载（如果属性变化已触发加载，SceneDirector 会自动忽略重复请求）
        SceneDirector.Instance.TryLoadTimelineNow();
    }

    /*
     * Unity 生命周期：清理场景加载回调
     */
    private void OnDestroy()
    {
        // 清理回调
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
