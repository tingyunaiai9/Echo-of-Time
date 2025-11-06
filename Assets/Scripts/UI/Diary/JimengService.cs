/* JimengService.cs
 * 独立负责处理 即梦 AI 图像生成 API 的所有网络请求
 * 包含火山引擎（VolcEngine）签名认证（待实现）
 */
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AI.DTOs; // 引入 DTO
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic; // 用于 Dictionary

public static class JimengService
{
    // API 配置
    // 注意：Query 参数已包含在 URL 中
    private const string JimengApiUrl = "https://visual.volcengineapi.com?Action=CVProcess&Version=2022-08-31";

    // !! 火山引擎的鉴权密钥 (!!不要硬编码, 应从安全位置读取!!)
    private const string VolcAccessKey = "YOUR_ACCESS_KEY"; // TODO: 替换
    private const string VolcSecretKey = "YOUR_SECRET_KEY"; // TODO: 替换
    private const string VolcRegion = "cn-north-1";
    private const string VolcService = "cv";

    private static readonly HttpClient s_sharedClient = new HttpClient();

    // 静态构造函数 (可以移除 DeepSeekService.cs 中的预热逻辑)
    static JimengService()
    {
        s_sharedClient.Timeout = System.TimeSpan.FromMinutes(2);
    }

    /// <summary>
    /// 调用 即梦 AI API 生成图像，通过回调返回图像 URL
    /// </summary>
    /// <param name="prompt">用户输入的提示词</param>
    /// <param name="onSuccess">成功时调用 (string: imageUrl)</param>
    /// <param name="onError">发生错误时调用 (string: errorMessage)</param>
    public static async Task GenerateImage(
        string prompt,
        Action<string> onSuccess,
        Action<string> onError)
    {
        try
        {
            // 1. 构造请求体 (Body)
            var requestBody = new JimengRequestBody
            {
                req_key = "jimeng_high_aes_general_v21_L",
                prompt = prompt,
                return_url = true,
                width = 512,
                height = 512,
                use_pre_llm = true, // 开启文本扩写
                use_sr = true,      // 开启超分 (出图 1024x1024)
                logo_info = new LogoInfo { add_logo = false },
                aigc_meta = new AIGCMeta { producer_id = "echo_of_time_game" }
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // 2. 构造 HttpRequestMessage (因为我们需要手动添加鉴权Header)
            var request = new HttpRequestMessage(HttpMethod.Post, JimengApiUrl);
            request.Content = content;

            // 3. !! 关键：火山引擎签名认证 !!
            //    您必须实现 GetVolcEngineAuthHeaders
            //    这需要您查阅火山引擎的 "公共参数 - 签名参数" 文档
            //    它通常涉及 HMAC-SHA256 签名
            try
            {
                // Dictionary<string, string> authHeaders = GetVolcEngineAuthHeaders(
                //     VolcAccessKey,
                //     VolcSecretKey,
                //     VolcRegion,
                //     VolcService,
                //     jsonBody
                // );

                // // 将生成的签名标头添加到请求中
                // foreach (var header in authHeaders)
                // {
                //     request.Headers.Add(header.Key, header.Value);
                // }
                
                // 【【临时占位符，请删除】】
                // 在您实现签名之前，请求将失败。
                // 这里只是为了让代码能跑通
                request.Headers.Add("X-Temp-Auth-Placeholder", "Implement VolcEngine Signature");
                // 【【临时占位符，请删除】】
            }
            catch (Exception authEx)
            {
                Debug.LogError($"[JimengService] 签名失败: {authEx.Message}");
                onError?.Invoke("签名计算失败。");
                return;
            }


            Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] [JimengService] 开始发送 API 请求");

            // 4. 发送请求
            HttpResponseMessage response = await s_sharedClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] [JimengService] 收到响应，状态码: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var jimengResponse = JsonConvert.DeserializeObject<JimengResponse>(responseBody);

                if (jimengResponse.code == 10000) // 10000 代表成功
                {
                    if (jimengResponse.data?.image_urls != null && jimengResponse.data.image_urls.Count > 0)
                    {
                        // 成功！返回第一张图片的 URL
                        onSuccess?.Invoke(jimengResponse.data.image_urls[0]);
                    }
                    else
                    {
                        onError?.Invoke("API 成功，但未返回图像 URL。");
                    }
                }
                else
                {
                    Debug.LogError($"[JimengService] API 返回业务错误: {jimengResponse.message}");
                    onError?.Invoke($"AI 图像生成失败: {jimengResponse.message}");
                }
            }
            else
            {
                Debug.LogError($"[JimengService] API 请求失败: {response.StatusCode}, 错误: {responseBody}");
                onError?.Invoke($"抱歉，图像服务暂时不可用 ({response.StatusCode})。");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JimengService] 调用 API 异常: {ex.Message}");
            onError?.Invoke("抱歉，发生了网络错误。");
        }
    }

    // TODO: 您必须实现这个方法
    // private static Dictionary<string, string> GetVolcEngineAuthHeaders(...)
    // {
    //    // 1. 获取当前时间戳 (ISO8601 格式)
    //    // 2. 构造规范请求 (Canonical Request)
    //    // 3. 构造签名字符串 (String to Sign)
    //    // 4. 计算签名 (HMAC-SHA256)
    //    // 5. 构造 Authorization 标头
    //
    //    // 返回包含 'Authorization', 'X-Date', 'X-Content-Sha256' 等的字典
    //    throw new NotImplementedException("请根据火山引擎文档实现签名算法");
    // }
}