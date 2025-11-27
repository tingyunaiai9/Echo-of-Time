using UnityEngine;
using Events;

public class DialogueDebug : MonoBehaviour
{
    [Header("把刚才创建的 TestDialogue 拖进来")]
    public DialogueData testData;

    void Update()
    {
        // 按 T 键触发测试
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (testData != null)
            {
                Debug.Log("开始测试剧情...");
                EventBus.LocalPublish(new StartDialogueEvent(testData));
            }
            else
            {
                Debug.LogError("请在 Inspector 中赋值 Test Data！");
            }
        }
    }
}