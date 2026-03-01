using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using DG.Tweening;
using RDOnline;
using RDOnline.Audio;
using RhythmCafe.Level;
using RDOnline.ScnLobby;
using RDOnline.Utils;
using SFB;

namespace RDOnline.Component
{
    /// <summary>
    /// 谱面预览 - 显示谱面信息并上传
    /// </summary>
    public class ChartPreview : MonoBehaviour, IChartPreview
    {
        [Header("UI组件")]
        [Tooltip("封面图片")]
        public RawImage CoverImage;
        [Tooltip("谱面名称")]
        public TMP_Text SongNameText;
        [Tooltip("谱面作者")]
        public TMP_Text AuthorText;
        [Tooltip("播放/暂停按钮")]
        public Button PlayButton;
        [Tooltip("音频源")]
        public AudioSource AudioSource;
        [Tooltip("上传进度条")]
        public Image ProgressBar;
        [Tooltip("开始上传按钮")]
        public Button UploadButton;
        [Tooltip("文件浏览按钮（选择 .rdlevel 后预览并可上传）")]
        public Button FileBrowseButton;

        [Header("设置")]
        [Tooltip("封面旋转速度（度/秒）")]
        public float RotationSpeed = 30f;

        [Header("上传结果")]
        [Tooltip("上传成功后的谱面URL")]
        public string UploadedChartUrl;

        private string _currentChartPath;
        private string _currentChartFolder;
        private bool _isPlaying;
        private bool _isUploading;
        private LevelDocument _lastDisplayedLevel;
        private Coroutine _loadCoverUrlCoroutine;
        private bool _previewFromLocalFile;
        private string _currentChartName;

        /// <summary>
        /// 当前谱面名称（本地文件加载时由解析得到，供创建房间等使用）
        /// </summary>
        public string CurrentChartName => _currentChartName;

        /// <summary>
        /// IChartPreview 接口实现 - 是否正在播放
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// 上传成功后的回调（返回chartUrl）
        /// </summary>
        public event Action<string> OnUploadSuccess;

        private void Start()
        {
            // 绑定按钮事件
            if (PlayButton != null)
                PlayButton.onClick.AddListener(TogglePlayPause);
            if (UploadButton != null)
                UploadButton.onClick.AddListener(StartUpload);
            if (FileBrowseButton != null)
                FileBrowseButton.onClick.AddListener(OnFileBrowseClick);

            // 初始化进度条
            if (ProgressBar != null)
            {
                Vector3 scale = ProgressBar.transform.localScale;
                scale.x = 0f;
                ProgressBar.transform.localScale = scale;
            }

            // 初始化上传按钮状态
            if (UploadButton != null)
                UploadButton.interactable = false;
        }

        private void Update()
        {
            if (!_previewFromLocalFile)
            {
                if (SelectedLevel.Current == null)
                {
                    if (_lastDisplayedLevel != null)
                        ClearCommunityLevel();
                }
                else if (SelectedLevel.Current != _lastDisplayedLevel)
                {
                    SetLevelFromCommunity(SelectedLevel.Current);
                }
            }

            if (CoverImage != null && CoverImage.texture != null)
                CoverImage.transform.Rotate(0, 0, -RotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 使用社区关卡信息填充预览（默认使用 url2 作为谱面 URL）
        /// </summary>
        private void SetLevelFromCommunity(LevelDocument doc)
        {
            _previewFromLocalFile = false;
            _currentChartName = null;
            _lastDisplayedLevel = doc;
            UploadedChartUrl = !string.IsNullOrEmpty(doc.url2) ? doc.url2 : doc.url;

            if (SongNameText != null)
                SongNameText.text = doc.song ?? "Unknown";
            if (AuthorText != null)
                AuthorText.text = doc.authors != null && doc.authors.Count > 0 ? string.Join(", ", doc.authors) : "Unknown";

            if (!string.IsNullOrEmpty(doc.image))
            {
                if (_loadCoverUrlCoroutine != null)
                    StopCoroutine(_loadCoverUrlCoroutine);
                _loadCoverUrlCoroutine = StartCoroutine(LoadCoverImageFromUrl(doc.image));
            }
            else if (CoverImage != null)
            {
                CoverImage.texture = null;
            }

            if (AudioSource != null && AudioSource.clip != null)
            {
                if (_isPlaying) Stop();
                AudioSource.clip = null;
            }

            if (UploadButton != null)
            {
                UploadButton.gameObject.SetActive(true);
                UploadButton.interactable = false;
            }
            if (ProgressBar != null)
                ProgressBar.transform.localScale = new Vector3(0f, ProgressBar.transform.localScale.y, ProgressBar.transform.localScale.z);
        }

        private void ClearCommunityLevel()
        {
            _previewFromLocalFile = false;
            _currentChartName = null;
            _lastDisplayedLevel = null;
            UploadedChartUrl = null;
            if (SongNameText != null) SongNameText.text = "";
            if (AuthorText != null) AuthorText.text = "";
            if (CoverImage != null) CoverImage.texture = null;
            if (UploadButton != null)
            {
                UploadButton.gameObject.SetActive(true);
                UploadButton.interactable = false;
            }
        }

        /// <summary>
        /// 文件浏览按钮：用 SFB 选择 .rdlevel，选中后预览该谱面并允许上传
        /// </summary>
        private void OnFileBrowseClick()
        {
            string path = FileDialog.FileDialog.GetFileDialog().OpenFile("选择文件", "",new FileDialog.OpenFileFilter
            {
                Filter = new Dictionary<string, List<string>>
                {
                    { "关卡文件", new List<string> { "*.rdlevel" } }
                },
                IncludeAllFiles = true
            });
            if (!string.IsNullOrEmpty(path))
            {
                LoadChart(path);
                _lastDisplayedLevel = null;
            }
            //ScrAlert.Show("当前平台不支持文件选择", true);
        }

        private IEnumerator LoadCoverImageFromUrl(string url)
        {
            using (var request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success && CoverImage != null)
                    CoverImage.texture = DownloadHandlerTexture.GetContent(request);
            }
            _loadCoverUrlCoroutine = null;
        }

        /// <summary>
        /// 加载谱面（从FileBrowser调用）
        /// </summary>
        public void LoadChart(string chartPath)
        {
            if (string.IsNullOrEmpty(chartPath) || !File.Exists(chartPath))
            {
                Debug.LogError("[ChartPreview] 谱面文件不存在");
                ScrAlert.Show("谱面文件不存在", true);
                return;
            }

            _previewFromLocalFile = true;
            _currentChartPath = chartPath;
            _currentChartFolder = Path.GetDirectoryName(chartPath);

            string content = File.ReadAllText(chartPath);
            ParseChartData(content);

            if (UploadButton != null)
            {
                UploadButton.gameObject.SetActive(true);
                UploadButton.interactable = true;
            }

            ScrAlert.Show("谱面加载成功", true);
        }

        /// <summary>
        /// 解析谱面数据
        /// </summary>
        private void ParseChartData(string content)
        {
            string songName = ExtractField(content, "song");
            string author = ExtractField(content, "author");
            string previewImage = ExtractField(content, "previewImage");
            string audioFile = ExtractField(content, "previewSong");
            if (string.IsNullOrEmpty(audioFile))
                audioFile = ExtractField(content, "songFilename");

            songName = RemoveHtmlTags(songName);
            author = RemoveHtmlTags(author);

            _currentChartName = songName;
            if (SongNameText != null)
                SongNameText.text = songName;
            if (AuthorText != null)
                AuthorText.text = author;

            if (!string.IsNullOrEmpty(previewImage))
            {
                string imagePath = Path.Combine(_currentChartFolder, previewImage);
                StartCoroutine(LoadCoverImage(imagePath));
            }

            if (!string.IsNullOrEmpty(audioFile))
            {
                string audioPath = Path.Combine(_currentChartFolder, audioFile);
                StartCoroutine(LoadAudio(audioPath));
            }
        }

        /// <summary>
        /// 提取字段值
        /// </summary>
        private string ExtractField(string content, string fieldName)
        {
            string pattern = $"\"{fieldName}\"\\s*:\\s*\"([^\"]+)\"";
            Match match = Regex.Match(content, pattern);
            return match.Success ? match.Groups[1].Value : "";
        }

        /// <summary>
        /// 去除HTML标签
        /// </summary>
        private string RemoveHtmlTags(string text)
        {
            return Regex.Replace(text, "<.*?>", "");
        }

        /// <summary>
        /// 加载封面图片
        /// </summary>
        private IEnumerator LoadCoverImage(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                Debug.LogWarning($"[ChartPreview] 封面图片不存在: {imagePath}");
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file:///" + imagePath))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (CoverImage != null)
                    {
                        CoverImage.texture = texture;
                    }
                }
            }
        }

        /// <summary>
        /// 加载音频文件
        /// </summary>
        private IEnumerator LoadAudio(string audioPath)
        {
            if (!File.Exists(audioPath))
            {
                Debug.LogWarning($"[ChartPreview] 音频文件不存在: {audioPath}");
                yield break;
            }

            AudioType audioType = GetAudioType(audioPath);
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file:///" + audioPath, audioType))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    if (AudioSource != null)
                    {
                        AudioSource.clip = clip;
                        // 不自动播放，等待用户点击播放按钮
                        // AudioSource.Play();
                        // _isPlaying = true;

                        Debug.Log("[ChartPreview] 音频加载成功");
                    }
                }
            }
        }

        /// <summary>
        /// 获取音频类型
        /// </summary>
        private AudioType GetAudioType(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext switch
            {
                ".mp3" => AudioType.MPEG,
                ".ogg" => AudioType.OGGVORBIS,
                ".wav" => AudioType.WAV,
                _ => AudioType.UNKNOWN
            };
        }

        /// <summary>
        /// 切换播放/暂停
        /// </summary>
        private void TogglePlayPause()
        {
            if (AudioSource == null || AudioSource.clip == null)
                return;

            if (_isPlaying)
            {
                AudioSource.Pause();
                _isPlaying = false;

                // 通知 AudioManager 预览停止
                if (RDOnline.Audio.AudioManager.Instance != null)
                {
                    RDOnline.Audio.AudioManager.Instance.OnPreviewStop(this);
                }
            }
            else
            {
                AudioSource.Play();
                _isPlaying = true;

                // 通知 AudioManager 预览开始
                if (RDOnline.Audio.AudioManager.Instance != null)
                {
                    RDOnline.Audio.AudioManager.Instance.OnPreviewStart(this);
                }
            }
        }

        /// <summary>
        /// 开始上传
        /// </summary>
        private void StartUpload()
        {
            if (_isUploading)
            {
                ScrAlert.Show("正在上传中，请稍候", true);
                return;
            }

            if (string.IsNullOrEmpty(_currentChartFolder))
            {
                Debug.LogError("[ChartPreview] 没有选择谱面");
                ScrAlert.Show("请先选择谱面文件", true);
                return;
            }

            ScrAlert.Show("开始上传谱面...", true);
            StartCoroutine(UploadChart());
        }

        /// <summary>
        /// 上传谱面协程
        /// </summary>
        private IEnumerator UploadChart()
        {
            _isUploading = true;
            if (UploadButton != null)
                UploadButton.interactable = false;

            // 1. 打包文件夹为zip
            string zipPath = Path.Combine(Application.temporaryCachePath, "chart_temp.zip");

            // 删除旧的临时文件
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            // 创建zip文件
            yield return CreateZipFile(_currentChartFolder, zipPath);

            // 2. 使用 123 云盘上传
            byte[] fileData = File.ReadAllBytes(zipPath);
            string errorMsg = null;
            string resultUrl = null;

            yield return Pan123ChartUploader.Upload(
                fileData,
                UpdateProgress,
                url =>
                {
                    resultUrl = url;
                },
                msg =>
                {
                    errorMsg = msg;
                }
            );

            if (!string.IsNullOrEmpty(errorMsg))
            {
                Debug.LogError("[ChartPreview] " + errorMsg);
                ScrAlert.Show(errorMsg, true);
            }
            else if (!string.IsNullOrEmpty(resultUrl))
            {
                UploadedChartUrl = resultUrl;
                OnUploadSuccess?.Invoke(resultUrl);
                ScrAlert.Show("谱面上传成功！", true);
            }

            // 3. 清理临时文件
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            _isUploading = false;
            if (UploadButton != null)
                UploadButton.interactable = true;
        }

        /// <summary>
        /// 创建ZIP文件
        /// </summary>
        private IEnumerator CreateZipFile(string folderPath, string zipPath)
        {
            // 使用 System.IO.Compression 创建ZIP
            ZipFile.CreateFromDirectory(folderPath,zipPath);
            yield return null;
        }

        /// <summary>
        /// 更新上传进度
        /// </summary>
        private void UpdateProgress(float progress)
        {
            if (ProgressBar != null)
            {
                Vector3 scale = ProgressBar.transform.localScale;
                scale.x = progress;
                ProgressBar.transform.localScale = scale;
            }
        }

        /// <summary>
        /// IChartPreview 接口实现 - 停止预览
        /// </summary>
        public void Stop()
        {
            if (AudioSource != null && _isPlaying)
            {
                AudioSource.Stop();
                _isPlaying = false;

                Debug.Log("[ChartPreview] 预览已停止");
            }
        }
    }
}
