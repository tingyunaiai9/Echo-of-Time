using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DrawerPanel : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.green;
    private Outline imageOutline;
    private GameObject imageGameObject;

    void Start()
    {
        // 获取 Image 对象上的 Outline 组件
        Transform imageTransform = transform.Find("Background/Image");
        if (imageTransform != null)
        {
            imageGameObject = imageTransform.gameObject;
            imageOutline = imageTransform.GetComponent<Outline>();
            if (imageOutline != null)
            {
                // 初始状态设置为不激活
                imageOutline.enabled = false;
                // 设置 Outline 颜色为绿色
                imageOutline.effectColor = outlineColor;
                
                // 添加 EventTrigger 组件来处理鼠标事件
                EventTrigger trigger = imageTransform.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = imageTransform.gameObject.AddComponent<EventTrigger>();
                }

                // 添加鼠标进入事件
                EventTrigger.Entry entryEnter = new EventTrigger.Entry();
                entryEnter.eventID = EventTriggerType.PointerEnter;
                entryEnter.callback.AddListener((data) => { OnPointerEnter(); });
                trigger.triggers.Add(entryEnter);

                // 添加鼠标离开事件
                EventTrigger.Entry entryExit = new EventTrigger.Entry();
                entryExit.eventID = EventTriggerType.PointerExit;
                entryExit.callback.AddListener((data) => { OnPointerExit(); });
                trigger.triggers.Add(entryExit);

                // 添加鼠标点击事件
                EventTrigger.Entry entryClick = new EventTrigger.Entry();
                entryClick.eventID = EventTriggerType.PointerClick;
                entryClick.callback.AddListener((data) => { OnPointerClick(); });
                trigger.triggers.Add(entryClick);
            }
            else
            {
                Debug.LogWarning("未找到 Outline 组件");
            }
        }
    }

    // 鼠标进入时调用
    private void OnPointerEnter()
    {
        if (imageOutline != null)
        {
            imageOutline.enabled = true;
        }
    }

    // 鼠标离开时调用
    private void OnPointerExit()
    {
        if (imageOutline != null)
        {
            imageOutline.enabled = false;
        }
    }

    // 鼠标点击时调用
    private void OnPointerClick()
    {
        if (imageGameObject != null)
        {
            imageGameObject.SetActive(false);
        }
    }
}