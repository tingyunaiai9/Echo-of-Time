using UnityEngine;
using UnityEngine.UI;

/*
 * PuzzleExitButton
 * 绑定在谜题场景 UI Button 上，点击退出当前顶部谜题。
 */
[RequireComponent(typeof(Button))]
public class PuzzleExitButton : MonoBehaviour
{
    private void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(OnExitClicked);
    }

    private void OnExitClicked()
    {
        if (PuzzleOverlayManager.Instance == null)
        {
            Debug.LogWarning("[PuzzleExit] PuzzleOverlayManager 不存在，无法关闭。");
            return;
        }
        PuzzleOverlayManager.Instance.ClosePuzzle();
    }
}
