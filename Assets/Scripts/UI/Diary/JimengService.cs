/* JimengService.cs
 * 独立负责处理 即梦 AI v3.1 图像生成 API
 * 包含：1. 提交任务 -> 2. 轮询结果
 * 警告：仍需实现火山引擎（VolcEngine）签名认证
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
    private const string SubmitTaskUrl = "https://visual.volcengineapi.com?Action=CVSync2AsyncSubmitTask&Version=2022-08-31";
    private const string QueryTaskUrl = "https://visual.volcengineapi.com?Action=CVSync2AsyncGetResult&Version=2022-08-31";
    private const string ReqKey = "jimeng_t2i_v31"; // v3.1 的固定值

    // !! 火山引擎的鉴权密钥 (!!不要硬编码, 应从安全位置读取!!)
    private const string VolcAccessKey = "YOUR_ACCESS_KEY"; // TODO: 替换
    private const string VolcSecretKey = "YOUR_SECRET_KEY"; // TODO: 替换
    private const string VolcRegion = "cn-north-1";
    private const string VolcService = "cv";

    private static readonly HttpClient s_sharedClient = new HttpClient();
    
    // 轮询配置
    private const int PollIntervalMs = 2000; // 轮询间隔 (2秒)
    private const int PollTimeoutSeconds = 60; // 总超时 (60秒)


    /// <summary>
    /// 调用 即梦 AI v3.1 API 生成图像 (包含提交 + 轮询)
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
            // 1. 提交任务
            string taskId = await SubmitTaskInternal(prompt);
            if (string.IsNullOrEmpty(taskId))
            {
                onError?.Invoke("任务提交失败。");
                return;
            }

            Debug.Log($"[JimengService] 任务提交成功, Task ID: {taskId}. 开始轮询...");

            // 2. 轮询结果
            DateTime startTime = DateTime.UtcNow;
            TimeSpan timeout = TimeSpan.FromSeconds(PollTimeoutSeconds);

            while (DateTime.UtcNow - startTime < timeout)
            {
                var queryResponse = await QueryTaskInternal(taskId);

                if (queryResponse == null || queryResponse.code != 10000)
                {
                    onError?.Invoke($"查询失败: {queryResponse?.message ?? "未知错误"}");
                    return;
                }

                string status = queryResponse.data?.status;
                Debug.Log($"[JimengService] 轮询状态: {status}");

                switch (status)
                {
                    case "done":
                        if (queryResponse.data.image_urls != null && queryResponse.data.image_urls.Count > 0)
                        {
                            onSuccess?.Invoke(queryResponse.data.image_urls[0]);
                            return; // 成功！
                        }
                        else
                        {
                            onError?.Invoke("任务完成，但未返回图像 URL。");
                            return;
                        }
                    case "in_queue":
                    case "generating":
                        // 任务进行中，等待后继续轮询
                        await Task.Delay(PollIntervalMs);
                        break;
                    case "not_found":
                    case "expired":
                        onError?.Invoke($"任务失败: 状态 {status}");
                        return;
                    default:
                        // 包含审核失败等情况 (如 50511, 50412)
                        onError?.Invoke($"任务失败: {queryResponse.message} (Status: {status})");
                        return;
                }
            }
            
            // 循环结束 = 超时
            onError?.Invoke("图像生成超时。");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JimengService] 调用 API 异常: {ex.Message}");
            onError?.Invoke("抱歉，发生了网络错误。");
        }
    }

    /// <summary>
    /// (私有) 步骤 1: 提交任务
    /// </summary>
    private static async Task<string> SubmitTaskInternal(string prompt)
    {
        var requestBody = new JimengSubmitBody
        {
            req_key = ReqKey,
            prompt = prompt,
            use_pre_llm = true, // 开启文本扩写
            seed = -1,
            width = 1328, // 标清 1:1
            height = 1328
        };
        string jsonBody = JsonConvert.SerializeObject(requestBody);
        
        // 签名
        var request = await CreateSignedRequest(SubmitTaskUrl, jsonBody);
        if (request == null) return null; // 签名失败

        HttpResponseMessage response = await s_sharedClient.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Debug.LogError($"[JimengService.Submit] API 请求失败: {response.StatusCode}, 错误: {responseBody}");
            return null;
        }

        var submitResponse = JsonConvert.DeserializeObject<JimengSubmitResponse>(responseBody);
        if (submitResponse.code == 10000)
        {
            return submitResponse.data?.task_id;
        }
        else
        {
            Debug.LogError($"[JimengService.Submit] API 返回业务错误: {submitResponse.message}");
            return null;
        }
    }

    /// <summary>
    /// (私有) 步骤 2: 查询任务
    /// </summary>
    private static async Task<JimengQueryResponse> QueryTaskInternal(string taskId)
    {
        // 构造 req_json (JSON-in-a-string)
        var reqJson = new JimengQueryReqJson
        {
            return_url = true,
            logo_info = new LogoInfo { add_logo = false },
            aigc_meta = new AIGCMeta { producer_id = "echo_of_time_game" }
        };
        string reqJsonString = JsonConvert.SerializeObject(reqJson);

        var requestBody = new JimengQueryBody
        {
            req_key = ReqKey,
            task_id = taskId,
            req_json = reqJsonString
        };
        string jsonBody = JsonConvert.SerializeObject(requestBody);

        // 签名
        var request = await CreateSignedRequest(QueryTaskUrl, jsonBody);
        if (request == null) return null; // 签名失败

        HttpResponseMessage response = await s_sharedClient.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Debug.LogError($"[JimengService.Query] API 请求失败: {response.StatusCode}, 错误: {responseBody}");
            return null;
        }

        return JsonConvert.DeserializeObject<JimengQueryResponse>(responseBody);
    }

    /// <summary>
    /// (私有) 构造已签名的 HttpRequestMessage (占位符)
    /// </summary>
    private static async Task<HttpRequestMessage> CreateSignedRequest(string url, string jsonBody)
    {
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = content;

        // 3. !! 关键：火山引擎签名认证 !!
        //    您必须实现 GetVolcEngineAuthHeaders
        //    这需要您查阅火山引擎的 "公共参数 - 签名参数" 文档
        try
        {
            // Dictionary<string, string> authHeaders = await GetVolcEngineAuthHeaders(
            //     VolcAccessKey, VolcSecretKey, VolcRegion, VolcService, jsonBody
            // );
            //
            // foreach (var header in authHeaders)
            // {
            //     request.Headers.Add(header.Key, header.Value);
            // }

            // 【【临时占位符，请删除】】
            request.Headers.Add("X-Temp-Auth-Placeholder", "Implement VolcEngine Signature");
            
            return request;
        }
        catch (Exception authEx)
        {
            Debug.LogError($"[JimengService] 签名失败: {authEx.Message}");
            return null;
        }
    }

    // TODO: 您必须实现这个方法
    // private static async Task<Dictionary<string, string>> GetVolcEngineAuthHeaders(...)
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