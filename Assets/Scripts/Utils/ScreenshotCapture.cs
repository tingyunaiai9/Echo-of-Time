using UnityEngine;
using System;
using System.IO;

public class ScreenshotCapture : MonoBehaviour
{
    public enum SaveLocation { ProjectRoot, Desktop, PersistentDataPath }

    [Header("截图设置")]
    [Tooltip("截图保存位置")]
    public SaveLocation location = SaveLocation.ProjectRoot;

    [Tooltip("截图保存的文件夹名称")]
    public string folderName = "Doc/Screenshots";
    
    [Tooltip("截图快捷键")]
    public KeyCode captureKey = KeyCode.F12;
    
    [Tooltip("放大倍数 (1=原始分辨率, 2=2倍, 4=4倍高清)")]
    [Range(1, 4)]
    public int superSize = 2;

    private static ScreenshotCapture instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(captureKey))
        {
            Capture();
        }
    }

    public void Capture()
    {
        string basePath = "";
        switch (location)
        {
            case SaveLocation.ProjectRoot:
                basePath = Directory.GetParent(Application.dataPath).FullName;
                break;
            case SaveLocation.Desktop:
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                break;
            case SaveLocation.PersistentDataPath:
                basePath = Application.persistentDataPath;
                break;
        }

        // 确保文件夹存在
        string path = Path.Combine(basePath, folderName);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        // 生成文件名：Screenshot_2023-10-01_12-00-00.png
        string filename = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string fullPath = Path.Combine(path, filename);

        // 截图
        ScreenCapture.CaptureScreenshot(fullPath, superSize);
        
        Debug.Log($"[Screenshot] 已保存高清截图: {fullPath} (放大倍数: {superSize}x)");
    }
}
