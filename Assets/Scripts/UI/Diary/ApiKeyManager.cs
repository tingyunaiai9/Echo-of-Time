/* UI/Diary/ApiKeyManager.cs
 * API 密钥加载与访问管理
 * 负责在游戏启动时从 Assets/StreamingAssets/api_keys.json 加载各类 AI 服务密钥
 * 为 DeepSeek、即梦等服务提供统一的静态访问入口
 */
using System;
using System.IO;
using AI.DTOs; // 引用 DTO
using Newtonsoft.Json;
using UnityEngine;

public static class ApiKeyManager
{
    // 公开的静态属性，供其他服务读取
    public static string DeepSeekKey { get; private set; }
    public static string VolcAccessKey { get; private set; }
    public static string VolcSecretKey { get; private set; }

    // 静态构造函数在类首次被访问时自动执行（且仅执行一次）
    static ApiKeyManager()
    {
        // Application.streamingAssetsPath 在不同平台上路径不同，但 Unity 会处理
        // 注意：Android 平台需要使用 UnityWebRequest 来读取，但 PC/Mac/iOS 可直接读
        string configFilePath = Path.Combine(Application.streamingAssetsPath, "api_keys.json");

        if (!File.Exists(configFilePath))
        {
            Debug.LogError($"[ApiKeyManager] 关键错误：未找到 API 密钥文件！路径: {configFilePath}");
            // 在编辑器中提供帮助
#if UNITY_EDITOR
            Debug.LogError("[ApiKeyManager] 请在 'Assets/StreamingAssets/' 目录下创建 'api_keys.json' 文件。");
#endif
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(configFilePath);
            var config = JsonConvert.DeserializeObject<ApiKeysConfig>(jsonContent);

            if (config == null)
            {
                Debug.LogError("[ApiKeyManager] 无法解析 api_keys.json。文件是否为空或格式错误？");
                return;
            }

            // 加载密钥到静态属性
            DeepSeekKey = config.DeepSeekKey;
            VolcAccessKey = config.VolcAccessKey;
            VolcSecretKey = config.VolcSecretKey;

            // （可选）验证密钥是否为空
            if (string.IsNullOrEmpty(DeepSeekKey) || string.IsNullOrEmpty(VolcAccessKey) || string.IsNullOrEmpty(VolcSecretKey))
            {
                Debug.LogWarning("[ApiKeyManager] api_keys.json 中有一个或多个密钥为空。");
            }

            // Debug.Log("[ApiKeyManager] API 密钥加载成功。");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ApiKeyManager] 加载 api_keys.json 失败: {ex.Message}");
        }
    }
}