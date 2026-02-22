using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Codice.Client.BaseCommands.Download;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Editor
{
    public class ModFileCopier
    {
        private static readonly string gamePluginsPath = @"E:\Rhythm Doctor\BepInEx\plugins";
        [MenuItem("Tools/复制Mod文件")]
        public static void CopyModFiles()
        {
            string modDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "RDOL");
            string assembliesDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library","ScriptAssemblies");
            string assetBundleDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ThunderKit","AssetBundleStaging","StandaloneWindows");
            if (!Directory.Exists(modDir))
            {
                Directory.CreateDirectory(modDir);
            }
            
            if (!Directory.Exists(assetBundleDir))
            {
                EditorUtility.DisplayDialog("错误", "AssetBundle目录不存在,请先构建AssetBundles", "确定");
            }
            File.Copy(Path.Combine(assembliesDir,"CheckUpdate.dll"), Path.Combine(modDir,"CheckUpdate.dll"),true);
            File.Copy(Path.Combine(assembliesDir,"RDOL.Entry.dll"), Path.Combine(modDir,"RDOL.Entry.dll"),true);
            File.Copy(Path.Combine(assetBundleDir,"checkupdate.scene.assets"), Path.Combine(modDir,"checkupdate.scene.assets"),true);
            File.Copy(Path.Combine(assetBundleDir,"checkupdate.resources.assets"), Path.Combine(modDir,"checkupdate.resources.assets"),true);
        }
        [MenuItem("Tools/启动游戏")]
        public static void StartGame()
        {
            CopyModFiles();
            string modDir = Path.Combine(gamePluginsPath, "RDOL");
            if (!Directory.Exists(modDir))
            {
                Directory.CreateDirectory(modDir);
            }
            Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Application.dataPath), "RDOL")).ToList().ForEach(a =>
            {
                File.Copy(a, Path.Combine(modDir, Path.GetFileName(a)), true);
            });
            Process.Start(new DirectoryInfo(gamePluginsPath).Parent.Parent.FullName + "\\Rhythm Doctor.exe");
        }
        [MenuItem("Tools/versioninfo.json")]
        public static void GenerateSha256()
        {
            string assembliesDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library","ScriptAssemblies");
            string assetBundleDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ThunderKit","AssetBundleStaging","StandaloneWindows");
            File.Copy(Path.Combine(assembliesDir,"RDOL.dll"), Path.Combine(assetBundleDir,"RDOL.dll"),true);
            VersionInfo versionInfo = new VersionInfo();
            versionInfo.version = "1.0.5";
            versionInfo.announcement = "公告信息";
            versionInfo.downloadFileInfos = new List<DownloadFileInfo>();
            foreach (var file in Directory.GetFiles(assetBundleDir))
            {
                string fileName = Path.GetFileName(file);
                if (fileName.EndsWith(".manifest")) continue;
                if (fileName.StartsWith("rdol") || fileName == ("RDOL.dll"))
                {
                    versionInfo.downloadFileInfos.Add(new DownloadFileInfo()
                    {
                        name = Path.GetFileName(file),
                        url = "https://hk.gh-proxy.org/https://github.com/StArrayJaN/netdisk/releases/download/rdol/" + Path.GetFileName(file),
                        size = new FileInfo(file).Length,
                        hash = "sha256:" + FileHasher.ComputeFileHash(file,HashAlgorithmType.SHA256)
                    });
                    Debug.Log($"已添加文件 {file} 到列表中");
                }
            }
            string json = JsonConvert.SerializeObject(versionInfo, Formatting.Indented);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "RDOL", "versioninfo.json"), json);
        }
        
        /// <summary>
        /// 异步从指定 URL 获取文本内容。
        /// </summary>
        /// <param name="url">目标 URL，必须以 http:// 或 https:// 开头。</param>
        /// <returns>返回的文本内容。</returns>
        /// <exception cref="ArgumentException">当 URL 为空或格式无效时抛出。</exception>
        /// <exception cref="HttpRequestException">当网络请求失败（如 404、无网络）时抛出。</exception>
        public static async Task<string> GetTextFromUrlAsync(string url)
        {
            var _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL 不能为空", nameof(url));

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new ArgumentException("URL 格式无效，请确保包含协议（如 http://）", nameof(url));

            // 发送请求并获取字符串（GetStringAsync 会自动处理编码）
            string content = await _httpClient.GetStringAsync(url);
            return content;
        }
        public struct VersionInfo
        {
            public string version;
            public string announcement;
            public List<DownloadFileInfo> downloadFileInfos;
        }

        public struct DownloadFileInfo
        {
            public string name;
            public string hash;
            public long size;
            public string url;
        }
    }
}