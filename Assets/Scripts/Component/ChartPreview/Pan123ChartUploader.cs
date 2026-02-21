using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace RDOnline.Component
{
    /// <summary>
    /// 123 云盘谱面上传（分片上传 + 获取直链），不挂载，由 ChartPreview 协程调用。
    /// Token 使用自有接口；父目录 ID: 32947250；创建文件/上传完毕用开放平台域名，分片用创建文件返回的 servers。
    /// </summary>
    public static class Pan123ChartUploader
    {
        private const string OpenApiHost = "https://open-api.123pan.com";
        private const string DirectLinkPath = "/api/v1/direct-link/url";
        private const int ParentFileId = 32947250;
        private const string Platform = "open_platform";
        private const int MaxRetries = 3;
        private const float RetryDelaySeconds = 1.5f;
        private const float UploadCompletePollInterval = 1f;

        /// <summary>
        /// 上传 zip 到 123 云盘并获取下载链接。由调用方 StartCoroutine 执行。
        /// 遇到网络/协议错误（如 HTTP/2 PROTOCOL_ERROR）时会自动重试最多 MaxRetries 次。
        /// </summary>
        /// <param name="zipData">zip 文件二进制</param>
        /// <param name="onProgress">上传进度 0~1，可为 null</param>
        /// <param name="onSuccess">成功时返回下载链接</param>
        /// <param name="onError">失败时返回错误说明</param>
        public static IEnumerator Upload(
            byte[] zipData,
            Action<float> onProgress,
            Action<string> onSuccess,
            Action<string> onError)
        {
            if (zipData == null || zipData.Length == 0)
            {
                onError?.Invoke("谱面文件为空");
                yield break;
            }

            if (GameConfig.Instance == null)
            {
                onError?.Invoke("游戏配置未就绪");
                yield break;
            }
            string tokenUrl = GameConfig.Instance.Pan123TokenUrl;

            // 1. 获取 token（带重试）
            string token = null;
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                if (attempt > 0)
                    yield return new WaitForSeconds(RetryDelaySeconds);

                using (var req = UnityWebRequest.Get(tokenUrl))
                {
                    req.SendWebRequest();
                    while (!req.isDone)
                        yield return null;

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        string err = NormalizeNetworkError(req.error);
                        if (attempt == MaxRetries - 1)
                        {
                            onError?.Invoke("获取上传凭证失败：" + err);
                            yield break;
                        }
                        continue;
                    }

                    var match = Regex.Match(req.downloadHandler.text, "\"token\"\\s*:\\s*\"([^\"]+)\"");
                    if (!match.Success)
                    {
                        onError?.Invoke("获取上传凭证失败：响应格式错误");
                        yield break;
                    }
                    token = match.Groups[1].Value;
                    break;
                }
            }

            if (string.IsNullOrEmpty(token))
            {
                onError?.Invoke("获取上传凭证失败：token 为空");
                yield break;
            }

            // 2. 创建文件（分片流程入口，使用开放平台域名）
            string filename = "chart_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + UnityEngine.Random.Range(10000, 99999) + ".zip";
            string etag = ComputeMd5Hex(zipData);
            long size = zipData.LongLength;
            string createBody = "{\"parentFileID\":" + ParentFileId + ",\"filename\":\"" + EscapeJsonString(filename) + "\",\"etag\":\"" + etag + "\",\"size\":" + size + "}";
            string createUrl = OpenApiHost + "/upload/v2/file/create";
            string preuploadID = null;
            int sliceSize = 0;
            string sliceServer = null;
            long fileId = 0;
            bool reuse = false;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                if (attempt > 0)
                    yield return new WaitForSeconds(RetryDelaySeconds);
                using (var req = new UnityWebRequest(createUrl, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(createBody));
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.SetRequestHeader("Authorization", token);
                    req.SetRequestHeader("Platform", Platform);
                    req.SendWebRequest();
                    while (!req.isDone)
                        yield return null;
                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        if (attempt == MaxRetries - 1)
                        {
                            onError?.Invoke("创建文件失败：" + NormalizeNetworkError(req.error));
                            yield break;
                        }
                        continue;
                    }
                    string body = req.downloadHandler.text;
                    int code = ParseJsonInt(body, "code");
                    if (code != 0)
                    {
                        string msg = ParseJsonString(body, "message");
                        onError?.Invoke(string.IsNullOrEmpty(msg) ? "创建文件失败（code=" + code + "）" : msg);
                        yield break;
                    }
                    reuse = ParseJsonBool(body, "reuse");
                    fileId = ParseJsonLong(body, "fileID");
                    if (reuse && fileId > 0)
                        break;
                    preuploadID = ParseJsonString(body, "preuploadID");
                    sliceSize = ParseJsonInt(body, "sliceSize");
                    sliceServer = ParseJsonArrayFirstString(body, "servers");
                    if (string.IsNullOrEmpty(preuploadID) || sliceSize <= 0 || string.IsNullOrEmpty(sliceServer))
                    {
                        onError?.Invoke("创建文件返回参数不完整");
                        yield break;
                    }
                    break;
                }
            }

            if (!reuse && (string.IsNullOrEmpty(preuploadID) || sliceSize <= 0 || string.IsNullOrEmpty(sliceServer)))
            {
                onError?.Invoke("创建文件失败");
                yield break;
            }

            // 4. 非秒传时：上传分片（servers 可能无协议头，需补全）
            if (!reuse)
            {
                if (!sliceServer.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    sliceServer = "https://" + sliceServer;
                int totalSlices = (int)((size + sliceSize - 1) / sliceSize);
                string sliceBaseUrl = sliceServer.TrimEnd('/') + "/upload/v2/file/slice";
                for (int sliceNo = 1; sliceNo <= totalSlices; sliceNo++)
                {
                    int offset = (sliceNo - 1) * sliceSize;
                    int len = (int)Math.Min(sliceSize, size - offset);
                    byte[] sliceData = new byte[len];
                    Array.Copy(zipData, offset, sliceData, 0, len);
                    string sliceMD5 = ComputeMd5Hex(sliceData);
                    WWWForm form = new WWWForm();
                    form.AddField("preuploadID", preuploadID);
                    form.AddField("sliceNo", sliceNo);
                    form.AddField("sliceMD5", sliceMD5);
                    form.AddBinaryData("slice", sliceData, "slice", "application/octet-stream");
                    bool sliceOk = false;
                    for (int attempt = 0; attempt < MaxRetries; attempt++)
                    {
                        if (attempt > 0)
                            yield return new WaitForSeconds(RetryDelaySeconds);
                        using (var request = UnityWebRequest.Post(sliceBaseUrl, form))
                        {
                            request.SetRequestHeader("Authorization", token);
                            request.SetRequestHeader("Platform", Platform);
                            request.SendWebRequest();
                            while (!request.isDone)
                            {
                                float p = (sliceNo - 1 + request.uploadProgress) / totalSlices;
                                onProgress?.Invoke(p);
                                yield return null;
                            }
                            onProgress?.Invoke((float)sliceNo / totalSlices);
                            if (request.result != UnityWebRequest.Result.Success)
                            {
                                if (attempt == MaxRetries - 1)
                                {
                                    onError?.Invoke("分片 " + sliceNo + " 上传失败：" + NormalizeNetworkError(request.error));
                                    yield break;
                                }
                                continue;
                            }
                            string body = request.downloadHandler.text;
                            if (ParseJsonInt(body, "code") != 0)
                            {
                                string msg = ParseJsonString(body, "message");
                                onError?.Invoke("分片 " + sliceNo + " 失败：" + (string.IsNullOrEmpty(msg) ? body : msg));
                                yield break;
                            }
                            sliceOk = true;
                            break;
                        }
                    }
                    if (!sliceOk)
                    {
                        onError?.Invoke("分片 " + sliceNo + " 上传失败：多次重试后仍无法完成");
                        yield break;
                    }
                }

                // 5. 上传完毕并轮询直到 completed（接口要求 completed=false 时间隔 1 秒再轮询）
                string completeUrl = OpenApiHost + "/upload/v2/file/upload_complete";
                string completeBody = "{\"preuploadID\":\"" + EscapeJsonString(preuploadID) + "\"}";
                const int maxPollCount = 60;
                for (int pollCount = 0; pollCount < maxPollCount; pollCount++)
                {
                    if (pollCount > 0)
                        yield return new WaitForSeconds(UploadCompletePollInterval);
                    bool requestOk = false;
                    for (int attempt = 0; attempt < MaxRetries; attempt++)
                    {
                        if (attempt > 0)
                            yield return new WaitForSeconds(RetryDelaySeconds);
                        using (var req = new UnityWebRequest(completeUrl, "POST"))
                        {
                            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(completeBody));
                            req.downloadHandler = new DownloadHandlerBuffer();
                            req.SetRequestHeader("Content-Type", "application/json");
                            req.SetRequestHeader("Authorization", token);
                            req.SetRequestHeader("Platform", Platform);
                            req.SendWebRequest();
                            while (!req.isDone)
                                yield return null;
                            if (req.result != UnityWebRequest.Result.Success)
                            {
                                if (attempt == MaxRetries - 1)
                                {
                                    onError?.Invoke("上传完毕请求失败：" + NormalizeNetworkError(req.error));
                                    yield break;
                                }
                                continue;
                            }
                            string body = req.downloadHandler.text;
                            if (ParseJsonInt(body, "code") != 0)
                            {
                                string msg = ParseJsonString(body, "message");
                                onError?.Invoke(string.IsNullOrEmpty(msg) ? "上传完毕失败" : msg);
                                yield break;
                            }
                            bool completed = ParseJsonBool(body, "completed");
                            fileId = ParseJsonLong(body, "fileID");
                            requestOk = true;
                            if (completed && fileId > 0)
                                break;
                            break;
                        }
                    }
                    if (!requestOk)
                        yield break;
                    if (fileId > 0)
                        break;
                }
            }

            if (fileId <= 0)
            {
                onError?.Invoke("上传完成但未返回文件 ID");
                yield break;
            }
            onProgress?.Invoke(1f);

            // 6. 获取直链链接（带重试）
            string directUrl = null;
            string directLinkUrl = OpenApiHost + DirectLinkPath + "?fileID=" + fileId;
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                if (attempt > 0)
                    yield return new WaitForSeconds(RetryDelaySeconds);

                using (var req = UnityWebRequest.Get(directLinkUrl))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + token);
                    req.SetRequestHeader("Platform", Platform);
                    req.SendWebRequest();
                    while (!req.isDone)
                        yield return null;

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        string err = NormalizeNetworkError(req.error);
                        if (attempt == MaxRetries - 1)
                        {
                            onError?.Invoke("获取直链失败：" + err);
                            yield break;
                        }
                        continue;
                    }

                    string body = req.downloadHandler.text;
                    int code = ParseJsonInt(body, "code");
                    if (code != 0)
                    {
                        string msg = GetDirectLinkErrorMessage(code, body);
                        onError?.Invoke(msg);
                        yield break;
                    }

                    directUrl = ParseJsonString(body, "url");
                    if (string.IsNullOrEmpty(directUrl))
                    {
                        onError?.Invoke("获取直链失败：响应中无 url");
                        yield break;
                    }
                    break;
                }
            }

            if (string.IsNullOrEmpty(directUrl))
            {
                onError?.Invoke("获取直链失败：多次重试后仍无法完成");
                yield break;
            }

            onSuccess?.Invoke(directUrl);
        }

        /// <summary>
        /// 将 Unity/curl 的 Unknown Error 或 HTTP/2 错误转为更易读的提示。
        /// </summary>
        private static string NormalizeNetworkError(string error)
        {
            if (string.IsNullOrEmpty(error) || error.Contains("Unknown Error"))
                return "网络或协议异常，请稍后重试";
            if (error.IndexOf("PROTOCOL_ERROR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                error.IndexOf("stream", StringComparison.OrdinalIgnoreCase) >= 0)
                return "连接被异常关闭（可稍后重试）";
            return error;
        }

        private static string ComputeMd5Hex(byte[] data)
        {
            if (data == null || data.Length == 0)
                return "";
            byte[] hash = MD5.Create().ComputeHash(data);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static int ParseJsonInt(string json, string key)
        {
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
        }

        private static long ParseJsonLong(string json, string key)
        {
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
            return m.Success && long.TryParse(m.Groups[1].Value, out long v) ? v : 0;
        }

        private static string ParseJsonString(string json, string key)
        {
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static bool ParseJsonBool(string json, string key)
        {
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            return m.Success && m.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static string ParseJsonArrayFirstString(string json, string key)
        {
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\\[\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private static string GetDirectLinkErrorMessage(int code, string body)
        {
            string msg = ParseJsonString(body, "message");
            return string.IsNullOrEmpty(msg) ? "获取直链失败（code=" + code + "）" : msg;
        }
    }
}
