using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.UI;
using Events;

/*
 * PuzzleOverlayManager
 * 通用谜题场景叠加管理器：支持多个谜题 Scene 的局部、本地加载与卸载，不影响其他玩家。
 * 功能点：
 * - OpenPuzzle(sceneName, options)
 * - ClosePuzzle() / CloseAll()
 * - 支持谜题嵌套（堆栈）或只允许一个（可配置）
 * - 打开时记录并可恢复前一个激活场景
 * - 可选禁用玩家控制 / 锁鼠标 / 黑屏淡入淡出（留扩展点）
 * - 事件：OnPuzzleOpened / OnPuzzleClosed / OnAllClosed
 * - 仅本地客户端加载，不调用 ServerChangeScene
 */
public class PuzzleOverlayManager : MonoBehaviour
{
    /// <summary>
    /// The singleton instance of the PuzzleOverlayManager.
    /// </summary>
    public static PuzzleOverlayManager singleton { get; private set; }

    [Header("General Options")]
    [Tooltip("是否允许多个谜题叠加（堆栈）。关闭则在打开新谜题前先关闭旧的。")]
    [SerializeField] private bool allowStack = false;
    [Tooltip("在加载谜题时是否设其为 ActiveScene。关闭则保持原激活场景，仅加载内容。")]
    [SerializeField] private bool setPuzzleSceneActive = false;
    [Tooltip("关闭最后一个谜题时是否恢复原激活场景。")]
    [SerializeField] private bool restorePreviousActiveScene = true;

    [Header("Player Control Options")]
    [SerializeField] private bool disablePlayerControlDuringPuzzle = true;
    [SerializeField] private bool unlockCursorDuringPuzzle = true;
    [SerializeField] private bool relockCursorOnExit = true;

    [Header("Camera Options")]
    [Tooltip("打开谜题时是否覆盖主相机的可见层（Culling Mask）。")]
    [SerializeField] private bool overrideCameraCulling = false;
    [Tooltip("谜题期间主相机的可见层。仅当 overrideCameraCulling 为 true 生效。")]
    [SerializeField] private LayerMask puzzleCullingMask = ~0; // 默认 Everything
    [Tooltip("打开谜题时是否将主相机移动到谜题场景中的锚点（名为 PuzzleCameraAnchor）。")]
    [SerializeField] private bool moveCameraToAnchor = false;
    [Tooltip("用于定位相机锚点的对象名。会在谜题场景根物体及其子层级查找该名称。")]
    [SerializeField] private string puzzleCameraAnchorName = "PuzzleCameraAnchor";
    [Tooltip("相机移动到锚点的插值时长（秒）；为 0 则瞬移。")]
    [SerializeField] private float cameraMoveLerpDuration = 0f;

    [Header("Completion Options")]
    [Tooltip("用于提示谜题已完成的通知 UI 控制器")]
    public NotificationController notificationUI;

    [Header("Debug")] [SerializeField] private bool verboseLog = true;

    // 事件：外部可订阅
    public event Action<string> OnPuzzleOpened;       // 参数：谜题场景名
    public event Action<string> OnPuzzleClosed;       // 参数：谜题场景名
    public event Action OnAllClosed;

    // 堆栈结构保存打开的谜题场景顺序
    private readonly Stack<PuzzleContext> puzzleStack = new Stack<PuzzleContext>();

    // 已完成的谜题集合，避免重复打开
    private readonly HashSet<string> completedPuzzles = new HashSet<string>();

    // 正在进行的加载/卸载操作保护
    private bool busy = false;

    // 记录初始激活场景（用于全部关闭时恢复）
    private Scene initialActiveScene;

    protected virtual void Awake()
    {
        InitializeSingleton();
        initialActiveScene = SceneManager.GetActiveScene();
        EventBus.Subscribe<PuzzleCompletedEvent>(e => MarkPuzzleCompleted(e.sceneName));
    }

    // Like NetworkManager
    protected void InitializeSingleton()
    {
        if (singleton != null && singleton == this) return;

        if (singleton != null)
        {
            if (verboseLog) Debug.LogWarning("场景中检测到多个 PuzzleOverlayManager。一次只能存在一个。重复的将被销毁。");
            Destroy(gameObject);
            return;
        }

        singleton = this;
        if (Application.isPlaying)
        {
            // Use DontDestroyOnLoad to make it persistent across scenes
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// A static convenient property to access the singleton instance without using PuzzleOverlayManager.singleton.
    /// It's the same as PuzzleOverlayManager.singleton.
    /// </summary>
    public static PuzzleOverlayManager Instance => singleton;

    public bool HasAnyPuzzleOpen => puzzleStack.Count > 0;
    public string CurrentPuzzleSceneName => puzzleStack.Count > 0 ? puzzleStack.Peek().SceneName : null;

    public void OpenPuzzle(string sceneName)
    {
        if (busy)
        {
            if (verboseLog) Debug.LogWarning("[PuzzleOverlay] Busy, ignore OpenPuzzle request: " + sceneName);
            return;
        }
        if (string.IsNullOrEmpty(sceneName)) return;

        // 若该谜题已完成，提示并不再打开
        if (completedPuzzles.Contains(sceneName))
        {
            if (notificationUI != null)
            {
                notificationUI.ShowNotification($"{sceneName}谜题已完成");
            }
            else if (verboseLog)
            {
                Debug.Log("[PuzzleOverlay] 谜题已完成: " + sceneName);
            }
            return;
        }
        Debug.Log("打开过的谜题列表: " + string.Join(", ", completedPuzzles));

        // 若不允许堆栈且已有则先关闭
        if (!allowStack && HasAnyPuzzleOpen)
        {
            ClosePuzzle();
        }

        if (sceneName == "Light" && PropBackpack.GetPropCount("mirror") <= 4){
            if (notificationUI != null)
            {
                notificationUI.ShowNotification($"你还没有找到足够的镜子来解开这个谜题。");
            }
            else if (verboseLog)
            {
                Debug.Log("[PuzzleOverlay] 镜子数量不足，无法打开谜题: " + sceneName);
            }
            return;
        }
        StartCoroutine(CoOpenPuzzle(sceneName));
    }

    public void ClosePuzzle()
    {
        if (busy)
        {
            if (verboseLog) Debug.LogWarning("[PuzzleOverlay] Busy, ignore ClosePuzzle request.");
            return;
        }
        if (!HasAnyPuzzleOpen) return;
        StartCoroutine(CoCloseTop());
    }

    public void CloseAll()
    {
        if (busy)
        {
            if (verboseLog) Debug.LogWarning("[PuzzleOverlay] Busy, ignore CloseAll request.");
            return;
        }
        if (!HasAnyPuzzleOpen) return;
        StartCoroutine(CoCloseAll());
    }

    /// <summary>
    /// 标记指定谜题已完成，后续调用 OpenPuzzle 时将直接提示已完成而不再打开。
    /// 由各谜题在完成时调用。
    /// </summary>
    public void MarkPuzzleCompleted(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        completedPuzzles.Add(sceneName);
        if (verboseLog) Debug.Log("[PuzzleOverlay] 记录谜题完成: " + sceneName);
    }

    private IEnumerator CoOpenPuzzle(string sceneName)
    {
        busy = true;
        var prevActive = SceneManager.GetActiveScene();

        if (verboseLog) Debug.Log($"[PuzzleOverlay] Opening puzzle scene '{sceneName}' additively...");

        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
        }

        var puzzleScene = SceneManager.GetSceneByName(sceneName);
        if (!puzzleScene.IsValid())
        {
            Debug.LogError($"[PuzzleOverlay] 场景 '{sceneName}' 加载失败或无效。请确保它已添加到 Build Settings 中。");
            busy = false;
            yield break; // 提前退出协程
        }

        // 自动禁用 Puzzle 场景中的 EventSystem，防止与主场景冲突
        DisableEventSystemInScene(puzzleScene);

        if (setPuzzleSceneActive)
            SceneManager.SetActiveScene(puzzleScene);

        // 禁用主 UI，防止点击穿透（例如右上角日记按钮与谜题退出按钮重叠）
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMainUIActive(false);
        }

        // 相机处理：记录当前主相机状态并根据设置进行调整
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            if (verboseLog) Debug.LogError("[PuzzleOverlay] Camera.main is NULL. 无法执行相机操作。请确保场景中有一个 Tag 为 'MainCamera' 的激活相机。");
        }

        CameraState prevCamState = default;
        if (mainCam != null)
        {
            if (verboseLog) Debug.Log($"[PuzzleOverlay] 找到主相机: {mainCam.name}。当前 Culling Mask: {mainCam.cullingMask}");
            prevCamState = CaptureCameraState(mainCam);

            if (moveCameraToAnchor)
            {
                var anchor = FindAnchorInScene(puzzleScene, puzzleCameraAnchorName);
                if (anchor != null)
                {
                    if (cameraMoveLerpDuration > 0f)
                        yield return MoveCameraTo(mainCam, anchor.transform, cameraMoveLerpDuration);
                    else
                        ApplyCameraTransform(mainCam, anchor.transform);
                }
                else if (verboseLog)
                {
                    Debug.LogWarning($"[PuzzleOverlay] 未找到相机锚点 '{puzzleCameraAnchorName}'，跳过相机定位。");
                }
            }

            if (overrideCameraCulling)
            {
                mainCam.cullingMask = puzzleCullingMask;
                if (verboseLog) Debug.Log($"[PuzzleOverlay] 已覆盖 Culling Mask。新 Culling Mask: {mainCam.cullingMask}");
            }
        }

        // 玩家控制处理
        if (disablePlayerControlDuringPuzzle)
            SetLocalPlayerControl(false);

        if (unlockCursorDuringPuzzle)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        puzzleStack.Push(new PuzzleContext(sceneName, prevActive, prevCamState));
        busy = false;
        OnPuzzleOpened?.Invoke(sceneName);
        if (verboseLog) Debug.Log($"[PuzzleOverlay] Puzzle '{sceneName}' opened. Stack depth={puzzleStack.Count}");
    }

    private IEnumerator CoCloseTop()
    {
        busy = true;
        var ctx = puzzleStack.Pop();
        string sceneName = ctx.SceneName;

        if (verboseLog) Debug.Log($"[PuzzleOverlay] Closing puzzle scene '{sceneName}'...");

        // 相机恢复到进入本谜题前的状态
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            ApplyCameraState(mainCam, ctx.PreviousCameraState);
        }

        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            var op = SceneManager.UnloadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
        }

        // 恢复前一个激活场景（当前堆栈顶的 prevActive 或初始）
        Scene targetRestore = ctx.PreviousActiveScene;
        if (restorePreviousActiveScene && targetRestore.IsValid())
            SceneManager.SetActiveScene(targetRestore);

        if (!HasAnyPuzzleOpen)
        {
            // 所有关闭后恢复初始控制
            if (disablePlayerControlDuringPuzzle)
                SetLocalPlayerControl(true);

            // 恢复主 UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetMainUIActive(true);
            }

            if (relockCursorOnExit)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            OnAllClosed?.Invoke();
        }
        else
        {
            // 若还有谜题，决定是否继续禁用控制（保持禁用）
            // 不做额外操作即可
        }

        busy = false;
        OnPuzzleClosed?.Invoke(sceneName);
        if (verboseLog) Debug.Log($"[PuzzleOverlay] Puzzle '{sceneName}' closed. Remaining depth={puzzleStack.Count}");
    }

    private IEnumerator CoCloseAll()
    {
        while (puzzleStack.Count > 0)
        {
            var top = puzzleStack.Peek();
            yield return CoCloseTop();
        }
    }

    private void SetLocalPlayerControl(bool enabled)
    {
        if (!NetworkClient.isConnected || NetworkClient.localPlayer == null) return;
        var root = NetworkClient.localPlayer.gameObject;
        // 根据项目实际添加/替换控制类名
        // 示例：移动、相机、输入等组件
        var movers = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var comp in movers)
        {
            // 你可以用标签接口，比如 IPuzzleLockable，在此判断实现后禁用/启用
            if (comp is IPuzzleLockable lockable)
            {
                if (enabled) lockable.OnPuzzleUnlock();
                else lockable.OnPuzzleLock();
            }
            // 或者直接判断名称（不推荐，最好用接口）
        }
    }

    // 接口：可让需要暂停的脚本实现
    public interface IPuzzleLockable
    {
        void OnPuzzleLock();
        void OnPuzzleUnlock();
    }

    private struct CameraState
    {
        public Vector3 position;
        public Quaternion rotation;
        public float fieldOfView;
        public int cullingMask;
    }

    private struct PuzzleContext
    {
        public string SceneName { get; }
        public Scene PreviousActiveScene { get; }
        public CameraState PreviousCameraState { get; }
        public PuzzleContext(string name, Scene prev, CameraState prevCam)
        {
            SceneName = name;
            PreviousActiveScene = prev;
            PreviousCameraState = prevCam;
        }
    }

    private CameraState CaptureCameraState(Camera cam)
    {
        return new CameraState
        {
            position = cam.transform.position,
            rotation = cam.transform.rotation,
            fieldOfView = cam.fieldOfView,
            cullingMask = cam.cullingMask
        };
    }

    private void ApplyCameraState(Camera cam, CameraState state)
    {
        cam.transform.position = state.position;
        cam.transform.rotation = state.rotation;
        cam.fieldOfView = state.fieldOfView;
        cam.cullingMask = state.cullingMask;
    }

    private void ApplyCameraTransform(Camera cam, Transform t)
    {
        cam.transform.position = t.position;
        cam.transform.rotation = t.rotation;
    }

    private IEnumerator MoveCameraTo(Camera cam, Transform target, float duration)
    {
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        float t = 0f;
        while (t < duration)
        {
            float k = t / duration;
            cam.transform.position = Vector3.Lerp(startPos, target.position, k);
            cam.transform.rotation = Quaternion.Slerp(startRot, target.rotation, k);
            t += Time.deltaTime;
            yield return null;
        }
        cam.transform.position = target.position;
        cam.transform.rotation = target.rotation;
    }

    private GameObject FindAnchorInScene(Scene scene, string name)
    {
        if (!scene.IsValid()) return null;
        var roots = scene.GetRootGameObjects();
        foreach (var go in roots)
        {
            var found = FindChildRecursive(go.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = FindChildRecursive(parent.GetChild(i), name);
            if (c != null) return c;
        }
        return null;
    }

    private void DisableEventSystemInScene(Scene scene)
    {
        if (!scene.IsValid()) return;

        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var eventSystems = root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true);
            foreach (var es in eventSystems)
            {
                if (es.gameObject.activeSelf)
                {
                    if (verboseLog) Debug.Log($"[PuzzleOverlay] Auto-disabling EventSystem in puzzle scene '{scene.name}': {es.gameObject.name}");
                    es.gameObject.SetActive(false);
                }
            }
        }
    }
}
