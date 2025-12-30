/*
 * MirrorPanel.cs
 * 镜子谜题的面板管理：负责镜子数量、重置逻辑、UI 显示以及镜子对象注册。
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * MirrorPanel 类
 * 统一管理镜子数量、拖拽可用状态及重置操作。
 */
public class MirrorPanel : MonoBehaviour
{
	[Header("UI 引用")]
	[Tooltip("镜子剩余数量文本")]
	[SerializeField] private TextMeshProUGUI mirrorCountText;
	[Tooltip("重置按钮")]
	[SerializeField] private Button resetButton;

	[Header("数量配置")]
	[Tooltip("最大镜子数量")]
	[SerializeField] private int maxMirrorCount = 5;

	[Header("镜子列表")]
	[Tooltip("可注册的镜子对象列表；若为空将自动在子层级中查找")]
	[SerializeField] private List<MirrorObject> mirrorObjects = new List<MirrorObject>();

	private int mirrorCount;

	/* 初始化：注册镜子与 UI 事件 */
	void Awake()
	{
		mirrorCount = maxMirrorCount;

		if (mirrorObjects == null || mirrorObjects.Count == 0)
		{
			mirrorObjects = new List<MirrorObject>(GetComponentsInChildren<MirrorObject>(true));
		}

		foreach (var mirror in mirrorObjects)
		{
			mirror?.SetPanel(this);
		}

		UpdateMirrorState();
		if (resetButton != null)
		{
			resetButton.onClick.AddListener(ResetAllMirrors);
		}
	}

	/* 销毁时移除事件监听 */
	void OnDestroy()
	{
		if (resetButton != null)
		{
			resetButton.onClick.RemoveListener(ResetAllMirrors);
		}
	}

	/* 注册新的镜子对象（供镜子在 Awake 时调用） */
	public void RegisterMirror(MirrorObject mirror)
	{
		if (mirror == null) return;
		if (!mirrorObjects.Contains(mirror))
		{
			mirrorObjects.Add(mirror);
		}
		mirror.SetPanel(this);
		mirror.SetInteractable(mirrorCount > 0);
	}

	/* 反注册镜子对象 */
	public void UnregisterMirror(MirrorObject mirror)
	{
		if (mirror == null) return;
		mirrorObjects.Remove(mirror);
	}

	/* 是否还有可用镜子 */
	public bool HasAvailableMirrors()
	{
		return mirrorCount > 0;
	}

	/* 尝试消耗一个镜子，成功返回 true */
	public bool TryConsumeMirror()
	{
		if (mirrorCount <= 0) return false;
		mirrorCount--;
		UpdateMirrorState();
		return true;
	}

	/* 重置所有镜子与镜槽状态 */
	public void ResetAllMirrors()
	{
		mirrorCount = maxMirrorCount;
		MirrorObject.ClearSlotOccupancy();

		// 重置所有镜槽外观与碰撞
		BoxCollider2D[] allColliders = FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);
		foreach (BoxCollider2D collider in allColliders)
		{
			if (collider.gameObject.layer == LayerMask.NameToLayer("Light") && collider.gameObject.name.Contains("Mirror"))
			{
				collider.enabled = false;

				Image image = collider.GetComponent<Image>();
				if (image != null)
				{
					Color grayColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
					image.color = grayColor;
				}

				Outline outline = collider.GetComponent<Outline>();
				if (outline != null)
				{
					outline.enabled = false;
				}
			}
		}

		foreach (var mirror in mirrorObjects)
		{
			mirror?.ResetMirrorPlacement();
		}

		UpdateMirrorState();
	}

	/* 更新镜子数量展示与拖拽可用状态 */
	private void UpdateMirrorState()
	{
		if (mirrorCountText != null)
		{
			mirrorCountText.text = mirrorCount.ToString();
		}

		bool canDrag = mirrorCount > 0;
		foreach (var mirror in mirrorObjects)
		{
			mirror?.SetInteractable(canDrag);
		}
	}
}
