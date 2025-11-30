/* Core/Services/Data/PlayerData.cs
 * 玩家数据模型，管理玩家状态、属性、进度等信息
 * 负责数据的序列化、保存和加载操作
 */

using System.Collections.Generic;
using UnityEngine;
using Events;
using System;
using System.IO;

/*
 * 玩家基础信息（时间线、层级等）
 */
[System.Serializable]
public class PlayerProfile
{
    public int timeline;
    public int currentlevel;
}

/* 日记数据结构 */
[System.Serializable]
public class ChatMessage
{
    public int index;
    public string content;
    public int timeline; // 时间线（0=Ancient, 1=Modern, 2=Future）
}

[System.Serializable]
public class ChatImage
{
    public int index;
    public string spritePath; // 图片资源路径（用于序列化）
    public int timeline; // 时间线（0=Ancient, 1=Modern, 2=Future）
}

[System.Serializable]
public class DiaryNote
{
    public int index;
    public string date;
    public string content;
}

[System.Serializable]
public class DiaryData
{
    public List<ChatMessage> chatMessages = new List<ChatMessage>();
    public List<ChatImage> chatImages = new List<ChatImage>();
    public List<DiaryNote> diaryNotes = new List<DiaryNote>();
}

/*
 * 保存数据结构（包含所有需要保存的数据）
 */
[System.Serializable]
public class SaveData
{
    public PlayerProfile playerProfile;
    public DiaryData diaryData;
    public string saveTime;
}

/*
* 玩家数据管理，负责本地玩家数据的存储、同步和持久化
*/
public class DataManager : Singleton<DataManager>
{
    [Header("是否存储日记数据")]
    public bool storeDiaryData = true;

    [Header("保存文件配置")]
    [Tooltip("保存文件名")]
    public string saveFileName = "PlayerData.json";

    // 玩家数据
    private PlayerProfile playerProfile = new PlayerProfile();
    private DiaryData diaryData = new DiaryData();

    // 保存文件路径
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
    
    protected override void Awake()
    {
        base.Awake();
        // 启动时尝试加载本地数据
        LoadFromLocal();
    }

    protected override void OnDestroy()
    {
        // 销毁时保存数据
        SaveToLocal();
    }

    void Update()
    {
        // 检测 S 快捷键
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveToLocal();
            Debug.Log("手动保存玩家数据到本地。");
        }
    }


    /*
     * 将玩家数据保存到本地 JSON 文件
     */
    public void SaveToLocal()
    {
        try
        {
            // 创建保存数据结构
            SaveData saveData = new SaveData
            {
                playerProfile = playerProfile,
                diaryData = diaryData,
                saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 序列化为 JSON
            string json = JsonUtility.ToJson(saveData, true);

            // 写入文件
            File.WriteAllText(SaveFilePath, json);

            Debug.Log($"[DataManager] 数据已保存至: {SaveFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataManager] 保存数据失败: {ex.Message}");
        }
    }

    /*
     * 从本地 JSON 文件加载玩家数据
     */
    public void LoadFromLocal()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                // 读取文件
                string json = File.ReadAllText(SaveFilePath);

                // 反序列化
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                if (saveData != null)
                {
                    playerProfile = saveData.playerProfile ?? new PlayerProfile();
                    diaryData = saveData.diaryData ?? new DiaryData();

                    Debug.Log($"[DataManager] 数据已加载，保存时间: {saveData.saveTime}");
                    Debug.Log($"[DataManager] 聊天消息数: {diaryData.chatMessages.Count}, 图片数: {diaryData.chatImages.Count}");
                }
            }
            else
            {
                Debug.Log($"[DataManager] 未找到保存文件，使用默认数据");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataManager] 加载数据失败: {ex.Message}");
        }
    }

    /*
     * 清除本地保存数据
     */
    public void ClearLocalData()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log($"[DataManager] 已删除保存文件: {SaveFilePath}");
            }

            // 重置数据
            playerProfile = new PlayerProfile();
            diaryData = new DiaryData();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataManager] 清除数据失败: {ex.Message}");
        }
    }

    /* 与服务器数据同步 */
    public void SyncWithServer()
    {
        // 上传本地变更
        // 下载服务器最新数据
        // 处理冲突合并
    }
}