using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 音效管理器，负责播放UI相关的音效
public class SFXManager : Singleton<SFXManager>
{
    [Header("场景BGM")]
    [Tooltip("古代时间线BGM")]
    public AudioClip ancientBGM;
    [Tooltip("民国时间线BGM")]
    public AudioClip modernBGM;
    [Tooltip("未来时间线BGM")]
    public AudioClip futureBGM;

    [Header("UI音效")]
    [Tooltip("面板开关音效")]
    public AudioClip panelOpenSFX;
    [Tooltip("按钮点击音效")]
    public AudioClip buttonClickSFX;
    [Tooltip("物品交互音效")]
    public AudioClip itemInteractSFX;
    [Tooltip("提示音效")]
    public AudioClip notificationSFX;

    [Header("谜题音效")]
    [Tooltip("谜题完成音效")]
    public AudioClip puzzleSolveSFX;
    [Tooltip("放置诗笺音效")]
    public AudioClip placePoemSFX;
    [Tooltip("放置画布碎片音效")]
    public AudioClip placePieceSFX;
    [Tooltip("放置镜子音效")]
    public AudioClip placeMirrorSFX;
    [Tooltip("旋转罗盘音效")]
    public AudioClip rotateCompassSFX;
    [Tooltip("旋转密码筒音效")]
    public AudioClip rotateLockSFX;

    [Header("自动按钮音效")]
    [Tooltip("是否自动为场景内按钮附加点击音效")]
    public bool autoAddButtonSounds = true;
    [Range(0f, 1f)]
    [Tooltip("按钮点击音效音量")]
    public float buttonSoundVolume = 1f;

    private AudioSource audioSource;
    private readonly HashSet<Button> processedButtons = new HashSet<Button>();

    protected override void Awake()
    {
        base.Awake();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (autoAddButtonSounds)
        {
            StartCoroutine(AddSoundsToAllButtonsDelayed());
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoAddButtonSounds) return;
        StartCoroutine(AddSoundsToAllButtonsDelayed());
    }

    private IEnumerator AddSoundsToAllButtonsDelayed()
    {
        yield return null; // 等待一帧，确保UI完整生成
        AddSoundsToAllButtons();
    }

    public void AddSoundsToAllButtons()
    {
        if (buttonClickSFX == null)
        {
            Debug.LogWarning("[SFXManager] 按钮点击音效未设置");
            return;
        }

        CleanProcessedButtons();
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
        int addedCount = 0;

        foreach (Button button in allButtons)
        {
            if (button == null) continue;

            GameObject go = button.gameObject;
            if (!go.scene.IsValid() || !go.scene.isLoaded) continue; // 跳过预制体

            if (processedButtons.Contains(button)) continue;

            AddSoundToButton(button);
            processedButtons.Add(button);
            addedCount++;
        }

        if (addedCount > 0)
        {
            Debug.Log($"[SFXManager] 已为 {addedCount} 个按钮添加点击音效");
        }
    }

    public void AddSoundToButton(Button button)
    {
        if (button == null || buttonClickSFX == null) return;

        button.onClick.RemoveListener(PlayButtonClickSound);
        button.onClick.AddListener(PlayButtonClickSound);
    }

    public void PlayButtonClickSound()
    {
        PlaySound(buttonClickSFX, buttonSoundVolume);
    }

    public void PlayPanelOpenSound()
    {
        PlaySound(panelOpenSFX);
    }

    public void PlayItemInteractSound()
    {
        PlaySound(itemInteractSFX);
    }

    public void PlayNotificationSound()
    {
        PlaySound(notificationSFX);
    }

    public void PlayPuzzleSolveSound()
    {
        PlaySound(puzzleSolveSFX);
    }

    public void PlayPlacePoemSound()
    {
        PlaySound(placePoemSFX);
    }

    public void PlayPlacePieceSound()
    {
        PlaySound(placePieceSFX);
    }

    public void PlayPlaceMirrorSound()
    {
        PlaySound(placeMirrorSFX);
    }

    public void PlayRotateCompassSound()
    {
        PlaySound(rotateCompassSFX);
    }

    public void PlayRotateLockSound()
    {
        PlaySound(rotateLockSFX);
    }

    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public void ClearProcessedButtons()
    {
        processedButtons.Clear();   
    }

    private void CleanProcessedButtons()
    {
        processedButtons.RemoveWhere(button => button == null);
    }
}
