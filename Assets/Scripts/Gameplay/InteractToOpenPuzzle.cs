using UnityEngine;

/*
 * InteractToOpenPuzzle
 * 一个可交互组件，当玩家通过 PlayerController 的交互系统调用它时，打开指定谜题。
 * 它需要被挂载在带有 'Interaction' 组件或类似交互基类的物体上，
 * 或者让这个类直接继承自你的交互基类。
 *
 * **重要**: 这个修改后的版本不再使用 OnTriggerEnter 或 Update。
 * 它依赖于 PlayerController 的 TryInteract() 来被调用。
 */
public class InteractToOpenPuzzle : MonoBehaviour // 如果你有交互基类，可以改为继承它，如: Interaction
{
    [SerializeField] private string puzzleSceneName = "Light";
    [Tooltip("是否在按交互键时若已有其它谜题关闭它（当不允许堆栈时可以忽略）")]
    [SerializeField] private bool closeExistingBeforeOpen = false;

    // 这个方法应该由你的 PlayerController 在检测到交互时调用。
    // 如果你有一个像 OnInteract(PlayerController player) 这样的标准交互方法，
    // 可以把这个逻辑放进去。
    public void OpenPuzzle()
    {
        if (PuzzleOverlayManager.singleton == null)
        {
            Debug.LogError("[InteractToOpenPuzzle] PuzzleOverlayManager 不存在，请在启动场景放置管理器对象。");
            return;
        }

        // 检查调用者是否是本地玩家。这一步现在可以移到 PlayerController 的 TryInteract 中，
        // 因为只有本地玩家才会执行那个方法。

        if (closeExistingBeforeOpen && PuzzleOverlayManager.singleton.HasAnyPuzzleOpen)
        {
            PuzzleOverlayManager.singleton.CloseAll();
        }

        Debug.Log($"[InteractToOpenPuzzle] 正在打开谜题: {puzzleSceneName}");
        PuzzleOverlayManager.singleton.OpenPuzzle(puzzleSceneName);
    }
}
