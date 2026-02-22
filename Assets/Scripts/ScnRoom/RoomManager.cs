using UnityEngine;
using TMPro;
using RDOnline.Network;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 房间管理器 - 单例，负责管理房间信息和谱面下载
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [Header("UI组件")]
        [Tooltip("房间名称文本")]
        public TMP_Text RoomNameText;
        [Tooltip("编辑房间按钮（仅房主可见）")]
        public UnityEngine.UI.Button EditRoomButton;

        private bool _isInitialized = false;
        private string _lastChartUrl = null; // 记录上一次的谱面URL，用于判断是否需要重新下载

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            // 取消注册事件
            UnregisterWebSocketEvents();
        }

        /// <summary>
        /// 初始化房间管理器（由 RoomJoiner 调用）
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[RoomManager] 已经初始化过了");
                return;
            }

            Debug.Log("[RoomManager] 开始初始化");

            // 记录初始的谱面URL
            _lastChartUrl = RoomData.Instance?.ChartUrl;

            // 更新房间名显示
            UpdateRoomNameUI();

            // 更新编辑按钮显示状态
            UpdateEditButtonVisibility();

            // 注册 WebSocket 事件监听
            RegisterWebSocketEvents();

            // 开始下载谱面
            StartDownloadChart();

            _isInitialized = true;
        }

        /// <summary>
        /// 开始下载谱面
        /// </summary>
        private void StartDownloadChart()
        {
            if (ChartDownloader.Instance == null)
            {
                Debug.LogError("[RoomManager] ChartDownloader 实例不存在");
                return;
            }

            if (RoomData.Instance == null || string.IsNullOrEmpty(RoomData.Instance.ChartUrl))
            {
                Debug.LogError("[RoomManager] 谱面URL为空");
                return;
            }

            string chartUrl = RoomData.Instance.ChartUrl;
            Debug.Log($"[RoomManager] 开始下载谱面: {chartUrl}");

            ChartDownloader.Instance.DownloadChart(
                chartUrl,
                () => {
                    Debug.Log("[RoomManager] 谱面下载成功");
                    ScrAlert.Show("谱面加载完成", true);
                },
                (error) => {
                    Debug.LogError($"[RoomManager] 谱面下载失败: {error}");
                    ScrAlert.Show($"谱面下载失败: {error}", true);
                }
            );
        }

        /// <summary>
        /// 更新房间名UI显示
        /// </summary>
        private void UpdateRoomNameUI()
        {
            if (RoomNameText != null && RoomData.Instance != null)
            {
                RoomNameText.text = RoomData.Instance.RoomName;
                Debug.Log($"[RoomManager] 更新房间名UI: {RoomData.Instance.RoomName}");
            }
        }

        /// <summary>
        /// 更新编辑按钮显示状态（仅房主可见）
        /// </summary>
        public void UpdateEditButtonVisibility()
        {
            if (EditRoomButton == null)
            {
                Debug.LogWarning("[RoomManager] EditRoomButton 引用未设置");
                return;
            }

            bool isOwner = IsCurrentUserOwner();
            EditRoomButton.gameObject.SetActive(isOwner);

            Debug.Log($"[RoomManager] 编辑按钮显示状态: {(isOwner ? "显示" : "隐藏")}, 当前用户ID: {UserData.Instance?.Id}, 房主ID: {RoomData.Instance?.OwnerId}");
        }

        /// <summary>
        /// 判断当前用户是否是房主
        /// </summary>
        private bool IsCurrentUserOwner()
        {
            if (RoomData.Instance == null || UserData.Instance == null)
                return false;

            return RoomData.Instance.OwnerId == UserData.Instance.Id;
        }

        /// <summary>
        /// 注册 WebSocket 事件监听
        /// </summary>
        private void RegisterWebSocketEvents()
        {
            if (WebSocketManager.Instance == null)
            {
                Debug.LogWarning("[RoomManager] WebSocketManager 实例不存在，无法注册事件");
                return;
            }

            WebSocketManager.Instance.Register("room/updated", OnRoomUpdated);

            Debug.Log("[RoomManager] WebSocket 事件监听已注册");
        }

        /// <summary>
        /// 取消注册 WebSocket 事件监听
        /// </summary>
        private void UnregisterWebSocketEvents()
        {
            if (WebSocketManager.Instance == null) return;

            WebSocketManager.Instance.Unregister("room/updated", OnRoomUpdated);

            Debug.Log("[RoomManager] WebSocket 事件监听已取消注册");
        }

        /// <summary>
        /// 处理房间更新事件
        /// </summary>
        private void OnRoomUpdated(ResponseMessage msg)
        {
            if (msg.data == null) return;

            try
            {
                Debug.Log("[RoomManager] 收到房间信息更新");

                bool hasChanges = false;
                string changeMessage = "房主更新了房间信息：";

                // 检查房间名称变化
                if (msg.data.ContainsKey("name"))
                {
                    string newName = msg.data["name"]?.ToString();
                    if (RoomData.Instance != null && RoomData.Instance.RoomName != newName)
                    {
                        RoomData.Instance.RoomName = newName;
                        hasChanges = true;
                        changeMessage += $"\n房间名称: {newName}";
                        Debug.Log($"[RoomManager] 房间名称更新: {newName}");

                        // 更新UI显示
                        UpdateRoomNameUI();
                    }
                }

                // 检查最大人数变化
                if (msg.data.ContainsKey("maxPlayers"))
                {
                    int newMaxPlayers = (msg.data["maxPlayers"] as Newtonsoft.Json.Linq.JToken)?.ToObject<int>() ?? 0;
                    if (RoomData.Instance != null && RoomData.Instance.MaxPlayers != newMaxPlayers)
                    {
                        RoomData.Instance.MaxPlayers = newMaxPlayers;
                        hasChanges = true;
                        changeMessage += $"\n最大人数: {newMaxPlayers}";
                        Debug.Log($"[RoomManager] 最大人数更新: {newMaxPlayers}");
                    }
                }

                // 检查密码状态变化
                if (msg.data.ContainsKey("hasPassword"))
                {
                    bool newHasPassword = (msg.data["hasPassword"] as Newtonsoft.Json.Linq.JToken)?.ToObject<bool>() ?? false;
                    if (RoomData.Instance != null && RoomData.Instance.HasPassword != newHasPassword)
                    {
                        RoomData.Instance.HasPassword = newHasPassword;
                        hasChanges = true;
                        changeMessage += $"\n密码状态: {(newHasPassword ? "已设置密码" : "已取消密码")}";
                        Debug.Log($"[RoomManager] 密码状态更新: {newHasPassword}");
                    }
                }

                // 检查谱面名称变化（必须在检查URL之前更新）
                if (msg.data.ContainsKey("chartName"))
                {
                    string newChartName = msg.data["chartName"]?.ToString();
                    if (RoomData.Instance != null)
                    {
                        RoomData.Instance.ChartName = newChartName;
                        Debug.Log($"[RoomManager] 谱面名称更新: {newChartName}");
                    }
                }

                // 检查谱面URL变化（重要！需要重新下载）
                if (msg.data.ContainsKey("chartUrl"))
                {
                    string newChartUrl = msg.data["chartUrl"]?.ToString();

                    if (RoomData.Instance != null)
                    {
                        // 使用 _lastChartUrl 来判断是否变化（而不是 RoomData.Instance.ChartUrl）
                        bool urlChanged = _lastChartUrl != newChartUrl;
                        RoomData.Instance.ChartUrl = newChartUrl;

                        if (urlChanged)
                        {
                            hasChanges = true;
                            changeMessage += "\n谱面已更换";
                            Debug.Log($"[RoomManager] 谱面URL更新: {newChartUrl}");

                            // 更新记录的URL
                            _lastChartUrl = newChartUrl;

                            // 重新下载谱面
                            ScrAlert.Show("房主更换了谱面，正在重新下载...", true);
                            StartDownloadChart();
                        }
                    }
                }

                // 检查房间状态变化
                if (msg.data.ContainsKey("status"))
                {
                    string newStatus = msg.data["status"]?.ToString();
                    if (RoomData.Instance != null && RoomData.Instance.Status != newStatus)
                    {
                        RoomData.Instance.Status = newStatus;
                        Debug.Log($"[RoomManager] 房间状态更新: {newStatus}");
                    }
                }

                // 检查人数变化
                if (msg.data.ContainsKey("playerCount"))
                {
                    int newPlayerCount = (msg.data["playerCount"] as Newtonsoft.Json.Linq.JToken)?.ToObject<int>() ?? 0;
                    if (RoomData.Instance != null && RoomData.Instance.PlayerCount != newPlayerCount)
                    {
                        RoomData.Instance.PlayerCount = newPlayerCount;
                        Debug.Log($"[RoomManager] 房间人数更新: {newPlayerCount}");
                    }
                }

                // 检查房主变更
                if (msg.data.ContainsKey("ownerId"))
                {
                    int newOwnerId = (msg.data["ownerId"] as Newtonsoft.Json.Linq.JToken)?.ToObject<int>() ?? 0;
                    if (RoomData.Instance != null)
                    {
                        bool ownerChanged = RoomData.Instance.OwnerId != newOwnerId;
                        if (ownerChanged)
                        {
                            RoomData.Instance.OwnerId = newOwnerId;
                            Debug.Log($"[RoomManager] 房主变更: {newOwnerId}");
                        }

                        // 无论 OwnerId 是否变化，都更新编辑按钮显示状态
                        // 因为 PlayerListUI 可能已经先更新了 RoomData.OwnerId
                        UpdateEditButtonVisibility();
                    }
                }

                // 如果有变化（除了谱面URL，因为谱面URL已经单独提示了），显示提示
                if (hasChanges && !msg.data.ContainsKey("chartUrl"))
                {
                    ScrAlert.Show(changeMessage, true);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RoomManager] 处理房间更新事件失败: {e.Message}");
            }
        }
    }
}
