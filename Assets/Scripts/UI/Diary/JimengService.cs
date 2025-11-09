/* JimengService.cs
 * (已更新，包含完整的火山引擎 v3.1 签名认证)
 *
 * 独立负责处理 即梦 AI v3.1 图像生成 API
 * 包含：1. 提交任务 -> 2. 轮询结果
 */
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AI.DTOs; // 引入 DTO
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic; // 用于 Dictionary
using System.Linq; // 用于 OrderBy

public static class JimengService
{
    // API 配置
    private const string Host = "visual.volcengineapi.com";
    private const string SubmitTaskUrl = "https://visual.volcengineapi.com?Action=CVSync2AsyncSubmitTask&Version=2022-08-31";
    private const string QueryTaskUrl = "https://visual.volcengineapi.com?Action=CVSync2AsyncGetResult&Version=2022-08-31";
    private const string ReqKey = "jimeng_t2i_v31"; // v3.1 的固定值

    // !! 鉴权密钥 (!!不要硬编码, 应从安全位置读取!!)
    private const string VolcAccessKey = "YOUR_ACCESS_KEY"; // TODO: 替换
    private const string VolcSecretKey = "YOUR_SECRET_KEY"; // TODO: 替换
    private const string VolcRegion = "cn-north-1";
    private const string VolcService = "cv";

    private static readonly HttpClient s_sharedClient = new HttpClient();
    
    // 轮询配置
    private const int PollIntervalMs = 2000; // 轮询间隔 (2秒)
    private const int PollTimeoutSeconds = 60; // 总超时 (60秒)

    /// <summary>
    /// (公共) 调用 即梦 AI v3.1 API 生成图像 (包含提交 + 轮询)
    /// </summary>
    public static async Task GenerateImage(
        string prompt,
        Action<string> onSuccess,
        Action<string> onError)
    {
        // (此方法与上一版回复完全相同，它调用下面的内部方法)
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

                if (queryResponse == null)
                {
                    onError?.Invoke("查询失败: 未知错误");
                    return;
                }
                
                if (queryResponse.code != 10000) // 10000 = Success
                {
                    // 检查是否是 'ResponseMetadata' 风格的错误
                    if (queryResponse.ResponseMetadata?.Error != null)
                    {
                        onError?.Invoke($"查询失败: {queryResponse.ResponseMetadata.Error.Message} (Code: {queryResponse.ResponseMetadata.Error.Code})");
                    }
                    else
                    {
                        onError?.Invoke($"查询失败: {queryResponse.message ?? "未知错误"}");
                    }
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
            use_pre_llm = true,
            seed = -1,
            width = 1328,
            height = 1328
        };
        string jsonBody = JsonConvert.SerializeObject(requestBody);
        
        // (调用包含签名的完整请求)
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

        // (调用包含签名的完整请求)
        var request = await CreateSignedRequest(QueryTaskUrl, jsonBody);
        if (request == null) return null; // 签名失败

        HttpResponseMessage response = await s_sharedClient.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Debug.LogError($"[JimengService.Query] API 请求失败: {response.StatusCode}, 错误: {responseBody}");
            return null;
        }

        // 单次反序列化。JsonConvert 会自动填充 data (如果code=10000) 
        // 或 ResponseMetadata (如果code!=10000)
        return JsonConvert.DeserializeObject<JimengQueryResponse>(responseBody);
    }

    /// <summary>
    /// (私有) 核心鉴权：创建已签名的 HttpRequestMessage
    /// </summary>
    private static async Task<HttpRequestMessage> CreateSignedRequest(string url, string jsonBody)
    {
        try
        {
            var uri = new Uri(url);
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // --- 步骤 1 & 2: 准备基础数据 ---
            DateTime utcNow = DateTime.UtcNow;
            string xDate = utcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
            string shortDate = utcNow.ToString("yyyyMMdd");
            string payloadHash = VolcSigner.HashSHA256(jsonBody);

            // 规范化 Headers (必须按key排序)
            var headers = new SortedDictionary<string, string>
            {
                { "host", Host },
                { "x-date", xDate },
                { "x-content-sha256", payloadHash },
                { "content-type", "application/json" }
            };
            
            string canonicalHeaders = string.Join("\n", headers.Select(kv => $"{kv.Key.ToLower()}:{kv.Value.Trim()}")) + "\n";
            string signedHeaders = string.Join(";", headers.Keys.Select(k => k.ToLower()));

            // 规范化 Query (必须按key排序)
            // (使用手动解析器替换 System.Web.HttpUtility)
            var sortedQuery = new SortedDictionary<string, string>();
            if (uri.Query.Length > 1) // 确保有Query (忽略 '?')
            {
                string query = uri.Query.Substring(1);
                foreach (var pair in query.Split('&'))
                {
                    var parts = pair.Split('=');
                    if (parts.Length == 2)
                    {
                        // Note: 不处理 URL 解码，因为我们的 Action/Version 不包含特殊字符
                        sortedQuery[parts[0]] = parts[1];
                    }
                }
            }
            string canonicalQuery = string.Join("&", sortedQuery.Select(kv => $"{kv.Key}={kv.Value}"));

            // --- 步骤 2: 创建规范请求 (CanonicalRequest) ---
            string canonicalRequest =
                "POST" + "\n" +         // HTTPRequestMethod
                uri.AbsolutePath + "\n" + // CanonicalURI (e.g., "/")
                canonicalQuery + "\n" +   // CanonicalQueryString
                canonicalHeaders + "\n" + // CanonicalHeaders
                signedHeaders + "\n" +    // SignedHeaders
                payloadHash;              // HexEncode(Hash(RequestPayload))

            // --- 步骤 3: 创建待签名字符串 (StringToSign) ---
            string credentialScope = $"{shortDate}/{VolcRegion}/{VolcService}/request";
            string stringToSign =
                "HMAC-SHA256" + "\n" +          // Algorithm
                xDate + "\n" +                  // RequestDate
                credentialScope + "\n" +        // CredentialScope
                VolcSigner.HashSHA256(canonicalRequest); // HexEncode(Hash(CanonicalRequest))

            // --- 步骤 4: 派生签名密钥 (kSigning) ---
            byte[] kSigning = VolcSigner.GenSigningSecretKeyV4(VolcSecretKey, shortDate, VolcRegion, VolcService);

            // --- 步骤 5: 计算签名 (Signature) ---
            byte[] signatureBytes = VolcSigner.HmacSHA256(kSigning, stringToSign);
            string signature = VolcSigner.ToHexString(signatureBytes);

            // --- 步骤 6: 构建 Authorization 标头 ---
            string authorization = $"HMAC-SHA256 Credential={VolcAccessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

            // --- 7. 将所有标头添加到实际请求中 ---
            request.Headers.Host = Host;
            request.Headers.Add("X-Date", xDate);
            request.Headers.Add("X-Content-Sha256", payloadHash); // 这是一个非标准但常用的 SigV4 标头
            request.Headers.Add("Authorization", authorization);
            // (Content-Type 已在 Content 属性中设置)

            return request;
        }
        catch (Exception authEx)
        {
            Debug.LogError($"[JimengService] 签名失败: {authEx.Message}");
            return null;
        }
    }
}