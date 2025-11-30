using UnityEngine;

public class DialWheel : MonoBehaviour
{
    public int steps = 10;              // 一圈分几格
    public float rotateSpeed = 300f;

    int currentIndex = 0;
    bool isRotating = false;
    Quaternion targetRotation;
    Quaternion baseRotation;            // 记录初始旋转

    public LockController lockController;

    void Start()
    {
        baseRotation   = transform.localRotation;  // 记住刚开始的 localRotation
        targetRotation = baseRotation;
    }

    void OnMouseDown()
    {
        if (isRotating) return;

        currentIndex = (currentIndex + 1) % steps;

        float stepAngle = 360f / steps;

        // 你的轴是 Y 轴：在初始旋转的基础上叠加
        targetRotation = baseRotation * Quaternion.Euler(0, currentIndex * stepAngle, 0);
        isRotating = true;

        if (lockController != null)
            lockController.OnWheelChanged();
    }

    void Update()
    {
        if (!isRotating) return;

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(transform.localRotation, targetRotation) < 0.1f)
        {
            transform.localRotation = targetRotation;
            isRotating = false;
        }
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }
}
