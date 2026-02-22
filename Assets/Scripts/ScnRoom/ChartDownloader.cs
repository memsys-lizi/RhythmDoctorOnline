using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 谱面下载器 - 单例，负责下载和解压谱面
    /// </summary>
    public class ChartDownloader : MonoBehaviour
    {
        public static ChartDownloader Instance { get; private set; }

        [Header("引用")]
        [Tooltip("房间谱面预览")]
        public RoomChartPreview ChartPreview;
        [Tooltip("准备按钮")]
        public UnityEngine.UI.Button ReadyButton;

        [Header("UI组件")]
        [Tooltip("进度条Image（通过X缩放显示进度）")]
        public Image ProgressBar;
        [Tooltip("重新下载按钮（下载失败时显示）")]
        public Button RetryButton;

        private string _chartDirectory;
        private bool _isDownloading = false;
        private string _currentChartUrl;
        private Action _lastOnSuccess;
        private Action<string> _lastOnError;
        private UnityWebRequest _currentRequest;
        private bool _cancelRequested = false;

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 设置谱面目录路径
            _chartDirectory = Path.Combine(Application.persistentDataPath, "Chart");
            Debug.Log($"[ChartDownloader] 谱面目录: {_chartDirectory}");

            // 初始化准备按钮为不可用
            SetReadyButtonInteractable(false);

            // 重新下载按钮
            if (RetryButton != null)
            {
                RetryButton.gameObject.SetActive(false);
                RetryButton.onClick.AddListener(OnRetryButtonClick);
            }
        }

        private void OnRetryButtonClick()
        {
            if (string.IsNullOrEmpty(_currentChartUrl) || _lastOnSuccess == null || _lastOnError == null)
                return;
            SetRetryButtonVisible(false);
            DownloadChart(_currentChartUrl, _lastOnSuccess, _lastOnError);
        }

        /// <summary>
        /// 下载谱面
        /// </summary>
        /// <param name="chartUrl">谱面下载URL</param>
        /// <param name="onSuccess">下载成功回调</param>
        /// <param name="onError">下载失败回调</param>
        public void DownloadChart(string chartUrl, Action onSuccess, Action<string> onError)
        {
            if (_isDownloading)
            {
                Debug.LogWarning("[ChartDownloader] 正在下载中，请稍候");
                onError?.Invoke("正在下载中，请稍候");
                return;
            }

            if (string.IsNullOrEmpty(chartUrl))
            {
                Debug.LogError("[ChartDownloader] 谱面URL为空");
                onError?.Invoke("谱面URL为空");
                return;
            }

            _currentChartUrl = chartUrl;
            _lastOnSuccess = onSuccess;
            _lastOnError = onError;
            StartCoroutine(DownloadChartCoroutine(chartUrl, onSuccess, onError));
        }

        /// <summary>
        /// 下载谱面协程
        /// </summary>
        private IEnumerator DownloadChartCoroutine(string chartUrl, Action onSuccess, Action<string> onError)
        {
            _isDownloading = true;

            // 禁用准备按钮，隐藏重新下载按钮
            SetReadyButtonInteractable(false);
            SetRetryButtonVisible(false);

            Debug.Log($"[ChartDownloader] 开始下载谱面: {chartUrl}");

            // 初始化进度条
            UpdateProgressBar(0f);

            // 1. 清理谱面目录
            CleanChartDirectory();

            // 2. 确保目录存在
            if (!Directory.Exists(_chartDirectory))
            {
                Directory.CreateDirectory(_chartDirectory);
                Debug.Log($"[ChartDownloader] 创建谱面目录: {_chartDirectory}");
            }

            // 3. 下载谱面文件
            string zipFilePath = Path.Combine(_chartDirectory, "chart.zip");
            UnityWebRequest request = UnityWebRequest.Get(chartUrl);
            request.downloadHandler = new DownloadHandlerFile(zipFilePath);
            _currentRequest = request;

            var operation = request.SendWebRequest();

            // 持续更新进度条，并检测取消
            while (!operation.isDone)
            {
                if (_cancelRequested)
                {
                    request.Abort();
                    _currentRequest = null;
                    _isDownloading = false;
                    _cancelRequested = false;
                    UpdateProgressBar(0f);
                    Debug.Log("[ChartDownloader] 下载已取消（例如房主更换了谱面）");
                    yield break;
                }
                float progress = request.downloadProgress;
                UpdateProgressBar(progress * 0.9f); // 下载占90%进度
                yield return null;
            }

            _currentRequest = null;
            if (_cancelRequested)
            {
                _isDownloading = false;
                _cancelRequested = false;
                UpdateProgressBar(0f);
                Debug.Log("[ChartDownloader] 下载已取消（例如房主更换了谱面）");
                yield break;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                _isDownloading = false;
                UpdateProgressBar(0f);
                string error = $"下载失败: {request.error}";
                Debug.LogError($"[ChartDownloader] {error}");
                SetRetryButtonVisible(true);
                onError?.Invoke(error);
                yield break;
            }

            Debug.Log($"[ChartDownloader] 谱面下载完成: {zipFilePath}");
            UpdateProgressBar(0.9f);

            // 4. 解压谱面
            bool extractSuccess = ExtractZipFile(zipFilePath);

            if (extractSuccess)
            {
                Debug.Log("[ChartDownloader] 谱面解压成功");
                UpdateProgressBar(1f); // 解压完成，进度100%
                _isDownloading = false;

                // 自动加载谱面预览
                if (ChartPreview != null)
                {
                    ChartPreview.LoadChartFromDirectory();
                }
                else
                {
                    Debug.LogWarning("[ChartDownloader] ChartPreview 引用未设置");
                }

                // 启用准备按钮
                SetReadyButtonInteractable(true);

                onSuccess?.Invoke();
            }
            else
            {
                _isDownloading = false;
                UpdateProgressBar(0f);
                string error = "谱面解压失败";
                Debug.LogError($"[ChartDownloader] {error}");
                SetRetryButtonVisible(true);
                onError?.Invoke(error);
            }
        }

        private void SetRetryButtonVisible(bool visible)
        {
            if (RetryButton != null)
                RetryButton.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 清理谱面目录
        /// </summary>
        private void CleanChartDirectory()
        {
            try
            {
                if (Directory.Exists(_chartDirectory))
                {
                    Debug.Log($"[ChartDownloader] 清理谱面目录: {_chartDirectory}");
                    Directory.Delete(_chartDirectory, true);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChartDownloader] 清理谱面目录失败: {e.Message}");
            }
        }

        /// <summary>
        /// 解压ZIP文件
        /// </summary>
        private bool ExtractZipFile(string zipFilePath)
        {
            try
            {
                Debug.Log($"[ChartDownloader] 开始解压谱面: {zipFilePath}");

                // 使用 System.IO.Compression 解压
                System.IO.Compression.ZipFile.ExtractToDirectory(zipFilePath, _chartDirectory);

                // 删除zip文件
                File.Delete(zipFilePath);
                Debug.Log($"[ChartDownloader] 已删除zip文件: {zipFilePath}");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChartDownloader] 解压失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取谱面目录路径
        /// </summary>
        public string GetChartDirectory()
        {
            return _chartDirectory;
        }

        /// <summary>
        /// 在目录下查找谱面文件：优先 main.rdlevel（含子目录），不存在则返回第一个找到的 .rdlevel
        /// </summary>
        public static string GetFirstRdlevelPath(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return null;
            try
            {
                string firstAny = null;
                foreach (string path in Directory.GetFiles(directory, "*.rdlevel", SearchOption.AllDirectories))
                {
                    if (firstAny == null)
                        firstAny = path;
                    if (Path.GetFileName(path).Equals("main.rdlevel", StringComparison.OrdinalIgnoreCase))
                        return path;
                }
                return firstAny;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ChartDownloader] 查找 .rdlevel 失败: {e.Message}");
            }
            return null;
        }

        /// <summary>
        /// 是否正在下载
        /// </summary>
        public bool IsDownloading()
        {
            return _isDownloading;
        }

        /// <summary>
        /// 取消当前下载（例如房主更换谱面时）。取消后不会调用 onError，下次 DownloadChart 可立即开始。
        /// </summary>
        public void CancelDownload()
        {
            _cancelRequested = true;
            if (_currentRequest != null)
                _currentRequest.Abort();
        }

        /// <summary>
        /// 更新进度条
        /// </summary>
        private void UpdateProgressBar(float progress)
        {
            if (ProgressBar != null)
            {
                // 通过修改X缩放来显示进度
                Vector3 scale = ProgressBar.transform.localScale;
                scale.x = Mathf.Clamp01(progress);
                ProgressBar.transform.localScale = scale;
            }
        }

        /// <summary>
        /// 设置准备按钮的可交互状态
        /// </summary>
        private void SetReadyButtonInteractable(bool interactable)
        {
            if (ReadyButton != null)
            {
                ReadyButton.interactable = interactable;
                Debug.Log($"[ChartDownloader] 准备按钮状态: {(interactable ? "可用" : "不可用")}");
            }
        }
    }
}
