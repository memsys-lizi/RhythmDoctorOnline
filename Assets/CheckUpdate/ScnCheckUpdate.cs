using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;

/// <summary>
/// 检查更新脚本
/// </summary>
public class ScnCheckUpdate : MonoBehaviour
{
    [Header("服务器")]
    [Tooltip("true=开发环境，false=生产环境")]
    public bool IsDev = true;
    public string DevServerURL = "http://localhost:3004";
    public string ProdServerURL = "https://rdonlineapi.rhythmdoctor.top";
    /// <summary>当前使用的服务器 URL（由 IsDev 决定）</summary>
    private string ServerURL => IsDev ? DevServerURL : ProdServerURL;

    [Header("检查更新面板")]
    [SerializeField] private CanvasGroup checkUpdatePanel;
    [SerializeField] private RectTransform progressBarImage;
    [SerializeField] private TMP_Text txtTitle;
    [SerializeField] private TMP_Text txtStep;
    [SerializeField] private TMP_Text txtProgress;
    [SerializeField] private TMP_Text txtAnnouncement;
    [SerializeField] private ScrollRect scrollAnnouncement;

    [Header("进入游戏")]
    [SerializeField] private CanvasGroup enterGamePanel;
    [SerializeField] private Button btnEnterGame;

    [Header("动画")]
    [SerializeField] private float fadeDuration = 0.3f;

    private const string LocalBundlesPath = "AssetBundles";
    /// <summary>
    /// 可写入的 AssetBundles 根目录（部分平台 StreamingAssets 只读，使用 persistentDataPath）
    /// </summary>
    private string BundlesRootPath => Path.Combine(Application.persistentDataPath, LocalBundlesPath);
    private string StreamingAssetsPath => Path.Combine(BepInModEntry.modPath,"CacheAssets");
    private string LocalInfoFullPath => Path.Combine(StreamingAssetsPath, "info.json");

    AssetBundle scenesBundle;
    AssetBundle resourcesBundle;
    Assembly assembly;
    
    private static string GetPlatformParam()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.Android: return "android";
            case RuntimePlatform.IPhonePlayer: return "ios";
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor: return "win";
            default: return null;
        }
    }

    private void Start()
    {
        if (btnEnterGame != null)
            btnEnterGame.onClick.AddListener(OnEnterGameClick);

        if (enterGamePanel != null)
        {
            enterGamePanel.alpha = 0;
            enterGamePanel.blocksRaycasts = false;
            enterGamePanel.interactable = false;
        }

        if (checkUpdatePanel != null)
        {
            checkUpdatePanel.alpha = 1;
            checkUpdatePanel.blocksRaycasts = true;
            checkUpdatePanel.interactable = true;
        }

        StartCoroutine(CheckUpdateCoroutine());
    }

    private void OnEnterGameClick()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartUp");
    }

    /// <summary>
    /// 加载资源包（在检查更新结束后自动调用）
    /// </summary>
    public virtual void LoadAssetBundles()
    {
        Debug.Log("try load" + new StackTrace());
        LoadAssemblyAndMetadata();
        scenesBundle = AssetBundle.LoadFromFile(Path.Combine(BepInModEntry.modPath,"CacheAssets","rdol.scenes.assets"));
        resourcesBundle = AssetBundle.LoadFromFile(Path.Combine(BepInModEntry.modPath,"CacheAssets","rdol.resources.assets"));
        
        Debug.Log("assetbundle loaded");
    }

    public void LoadAssemblyAndMetadata()
    {
        try
        {
            assembly = Assembly.LoadFrom(Path.Combine(BepInModEntry.modPath,"CacheAssets","RDOL.dll"));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private IEnumerator CheckUpdateCoroutine()
    {
        SetTitle("检查更新");
        SetStep("正在获取更新信息...");
        SetProgress(0f);
        SetProgressText("0%");

        // 1. 请求服务器获取更新信息
        string platform = GetPlatformParam();
        if (string.IsNullOrEmpty(platform))
        {
            Debug.LogError("当前平台不支持更新检查");
            SetStep("当前平台不支持");
            OnCheckComplete(success: false);
            yield break;
        }
        string checkUrl = $"{ServerURL.TrimEnd('/')}/checkupdate?platform={platform}";
        using (UnityWebRequest request = UnityWebRequest.Get(checkUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"检查更新失败: {request.error}");
                SetStep($"检查更新失败: {request.error}");
                OnCheckComplete(success: false);
                yield break;
            }

            string json = request.downloadHandler.text;
            UpdateInfo updateInfo;
            try
            {
                updateInfo = JsonConvert.DeserializeObject<UpdateInfo>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"解析更新信息失败: {e.Message}");
                SetStep("解析更新信息失败");
                OnCheckComplete(success: false);
                yield break;
            }

            if (updateInfo == null || string.IsNullOrEmpty(updateInfo.version))
            {
                Debug.LogError("更新信息无效");
                SetStep("更新信息无效");
                OnCheckComplete(success: false);
                yield break;
            }

            // 显示更新日志
            if (txtAnnouncement != null && !string.IsNullOrEmpty(updateInfo.announcement))
            {
                txtAnnouncement.text = updateInfo.announcement;
                // 强制 TMP 重新计算 preferred size
                txtAnnouncement.ForceMeshUpdate();
                // 强制 Content 重新布局（ContentSizeFitter + VerticalLayoutGroup 依赖子对象尺寸）
                if (scrollAnnouncement != null && scrollAnnouncement.content != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollAnnouncement.content);
                yield return null; // 等待一帧让布局完成
                if (scrollAnnouncement != null)
                    scrollAnnouncement.verticalNormalizedPosition = 1f; // 滑动到最顶部
            }

            updateInfo.files = updateInfo.files ?? new FileInfo[0];

            // 读取本地版本
            LocalInfo localInfo = LoadLocalInfo();
            bool needUpdate = CompareVersion(updateInfo.version, localInfo?.version ?? "0.0.0") > 0;

            List<FileInfo> filesToDownload = new List<FileInfo>();

            if (needUpdate)
            {
                // 版本号有更新：需要下载所有云端文件
                SetStep("发现新版本，准备下载...");
                filesToDownload.AddRange(updateInfo.files);
            }
            else
            {
                // 2. 版本相同：检查资源完整性
                SetStep("检查资源完整性...");
                SetProgress(0f);

                if (!Directory.Exists(StreamingAssetsPath))
                    Directory.CreateDirectory(StreamingAssetsPath);

                var cloudFileDict = new Dictionary<string, FileInfo>();
                foreach (var f in updateInfo.files)
                    cloudFileDict[Path.GetFileName(f.name)] = f;

                // 云端有本地没有 -> 下载；云端没有本地有 -> 删除
                var localFiles = Directory.Exists(StreamingAssetsPath)
                    ? Directory.GetFiles(StreamingAssetsPath)
                    : Array.Empty<string>();
                int totalFiles = updateInfo.files.Length + localFiles.Length;
                if (totalFiles == 0)
                {
                    SetProgress(1f);
                    SetProgressText("100%");
                }
                int checkedCount = 0;

                foreach (var cloudFile in updateInfo.files)
                {
                    string localPath = Path.Combine(StreamingAssetsPath, Path.GetFileName(cloudFile.name));
                    if (!File.Exists(localPath))
                    {
                        filesToDownload.Add(cloudFile);
                    }
                    else
                    {
                        string localHash = ComputeFileHash(localPath);
                        if (!string.Equals(localHash, cloudFile.hash, StringComparison.OrdinalIgnoreCase))
                            filesToDownload.Add(cloudFile);
                    }
                    checkedCount++;
                    if (totalFiles > 0)
                    {
                        SetProgress((float)checkedCount / totalFiles);
                        SetProgressText($"{(int)((float)checkedCount / totalFiles * 100)}%");
                    }
                    yield return null;
                }

                foreach (var localPath in localFiles)
                {
                    string fileName = Path.GetFileName(localPath);
                    if (!cloudFileDict.ContainsKey(fileName))
                    {
                        try
                        {
                            File.Delete(localPath);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"删除多余文件失败 {fileName}: {e.Message}");
                        }
                    }
                    checkedCount++;
                    if (totalFiles > 0)
                    {
                        SetProgress((float)checkedCount / totalFiles);
                        SetProgressText($"{(int)((float)checkedCount / totalFiles * 100)}%");
                    }
                    yield return null;
                }
            }

            // 3. 下载资源包
            if (filesToDownload.Count > 0)
            {
                if (!Directory.Exists(StreamingAssetsPath))
                    Directory.CreateDirectory(StreamingAssetsPath);
                yield return DownloadFilesCoroutine(filesToDownload);
            }
            else
            {
                SetStep("资源已是最新");
                SetProgress(1f);
                SetProgressText("100%");
            }

            // 写入本地 info.json
            try
            {
                string infoJson = JsonUtility.ToJson(new LocalInfo
                {
                    version = updateInfo.version,
                    announcement = updateInfo.announcement ?? ""
                });
                string dir = Path.GetDirectoryName(LocalInfoFullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(LocalInfoFullPath, infoJson, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"写入 info.json 失败: {e.Message}");
            }

            // 检查更新结束后自动加载资源包
            SetStep("加载资源中...");
            LoadAssetBundles();
            yield return null;

            OnCheckComplete(success: true);
        }
    }

    private IEnumerator DownloadFilesCoroutine(List<FileInfo> files)
    {
        long totalSize = 0;
        foreach (var f in files)
            totalSize += (long)f.size;

        long downloadedTotal = 0;
        float lastTime = Time.realtimeSinceStartup;
        long lastDownloaded = 0;

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            SetStep($"下载文件 ({i + 1}/{files.Count})");

            string savePath = Path.Combine(StreamingAssetsPath, Path.GetFileName(file.name));

            using (UnityWebRequest request = UnityWebRequest.Get(file.url))
            {
                request.downloadHandler = new DownloadHandlerFile(savePath);
                request.disposeDownloadHandlerOnDispose = true;

                var op = request.SendWebRequest();

                while (!op.isDone)
                {
                    long downloaded = (long)request.downloadedBytes;
                    downloadedTotal = 0;
                    for (int j = 0; j < i; j++)
                        downloadedTotal += (long)files[j].size;
                    downloadedTotal += downloaded;

                    float progress = totalSize > 0 ? (float)downloadedTotal / totalSize : 0f;
                    SetProgress(progress);
                    SetProgressText($"{(int)(progress * 100)}%");

                    float elapsed = Time.realtimeSinceStartup - lastTime;
                    float speed = elapsed > 0 ? (downloadedTotal - lastDownloaded) / elapsed : 0;
                    lastTime = Time.realtimeSinceStartup;
                    lastDownloaded = downloadedTotal; 
                    SetStep($"下载文件 ({i + 1}/{files.Count})\n{FormatSize(downloadedTotal)}/{FormatSize(totalSize)} {FormatSpeed(speed)}");
                    yield return null;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"下载失败 {file.name}: {request.error}");
                    SetStep($"下载失败: {request.error}");
                    OnCheckComplete(success: false);
                    yield break;
                }
            }
        }

        SetProgress(1f);
        SetProgressText("100%");
        SetStep("下载完成");
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
        return $"{bytes / (1024f * 1024f):F1} MB";
    }

    private static string FormatSpeed(float bytesPerSecond)
    {
        return $"{FormatSize((long)bytesPerSecond)}/s";
    }

    private LocalInfo LoadLocalInfo()
    {
        try
        {
            string path = LocalInfoFullPath;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                return JsonUtility.FromJson<LocalInfo>(json);
            }
            // Android 首次运行：info 在 apk 内，此处简化处理返回 null，将触发全量下载
            return null;
        }
        catch
        {
            return null;
        }
    }

    private int CompareVersion(string a, string b)
    {
        int[] pa = ParseVersion(a);
        int[] pb = ParseVersion(b);
        int len = Math.Max(pa.Length, pb.Length);
        for (int i = 0; i < len; i++)
        {
            int va = i < pa.Length ? pa[i] : 0;
            int vb = i < pb.Length ? pb[i] : 0;
            if (va != vb) return va.CompareTo(vb);
        }
        return 0;
    }

    private static int[] ParseVersion(string v)
    {
        if (string.IsNullOrEmpty(v)) return new[] { 0 };
        var parts = v.Split('.');
        var result = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            int.TryParse(parts[i], out result[i]);
        return result;
    }

    private static string ComputeFileHash(string filePath)
    {
        try
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha.ComputeHash(stream);
                string hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                return "sha256:" + hex;
            }
        }
        catch
        {
            return "";
        }
    }

    private void OnCheckComplete(bool success)
    {
        if (success)
        {
            SetTitle("欢迎来到RD Online");
            if (checkUpdatePanel != null)
            {
                checkUpdatePanel.DOFade(0f, fadeDuration)
                    .OnComplete(() =>
                    {
                        checkUpdatePanel.blocksRaycasts = false;
                        checkUpdatePanel.interactable = false;
                    });
            }
            if (enterGamePanel != null)
            {
                enterGamePanel.blocksRaycasts = true;
                enterGamePanel.interactable = true;
                enterGamePanel.DOFade(1f, fadeDuration);
            }
        }
        else
        {
            SetTitle("更新失败");
            // 不覆盖 txtStep，保留各失败点设置的具体错误信息
            if (enterGamePanel != null)
            {
                enterGamePanel.alpha = 0;
                enterGamePanel.blocksRaycasts = false;
                enterGamePanel.interactable = false;
            }
        }
    }

    private void SetTitle(string text)
    {
        if (txtTitle != null) txtTitle.text = text;
    }

    private void SetStep(string text)
    {
        if (txtStep != null) txtStep.text = text;
    }

    private void SetProgress(float progress)
    {
        if (progressBarImage != null)
        {
            var scale = progressBarImage.localScale;
            progressBarImage.localScale = new Vector3(Mathf.Clamp01(progress), scale.y, scale.z);
        }
    }

    private void SetProgressText(string text)
    {
        if (txtProgress != null) txtProgress.text = text;
    }

    [Serializable]
    private class UpdateInfo
    {
        public string version;
        public string announcement;
        public FileInfo[] files;
    }

    [Serializable]
    private class FileInfo
    {
        public string name;
        public string hash;
        public double size;
        public string url;
    }

    [Serializable]
    private class LocalInfo
    {
        public string version;
        public string announcement;
    }
}
