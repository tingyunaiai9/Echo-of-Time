using UnityEngine;

public class LockController : MonoBehaviour
{
    public DialWheel[] wheels;   // 在 Inspector 里拖进来
    public int[] correctCode;    // 正确的组合，比如 {1, 4, 2}

    public GameObject doorToOpen; // 要打开的门/播放动画的对象

    public void OnWheelChanged()
    {
        if (IsCorrect())
        {
            Debug.Log("Unlocked!");
            // 这里执行开锁逻辑：播放动画、切场景、触发事件等
            if (doorToOpen != null)
            {
                // 比如直接打开门（你可以替换成 Animator 触发）
                doorToOpen.SetActive(false);
            }
        }
    }

    bool IsCorrect()
    {
        if (wheels.Length != correctCode.Length) return false;

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i].GetCurrentIndex() != correctCode[i])
                return false;
        }
        return true;
    }
}
