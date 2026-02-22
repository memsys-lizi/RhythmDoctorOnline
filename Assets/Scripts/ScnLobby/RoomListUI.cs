using System.Collections.Generic;
using UnityEngine;
using RDOnline.Network;

namespace RDOnline.ScnLobby
{
    /// <summary>
    /// 房间列表UI管理器 - 管理房间列表的显示和更新
    /// </summary>
    public class RoomListUI : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("房间列表容器（ScrollView的Content）")]
        public Transform RoomListContent;
        [Tooltip("房间项预制体")]
        public GameObject RoomItemPrefab;

        [Header("设置")]
        [Tooltip("是否在启动时自动加载房间列表")]
        public bool LoadOnStart = true;

        // 房间项字典，key为roomId
        private Dictionary<string, RoomItem> _roomItems = new Dictionary<string, RoomItem>();

        private void Start()
        {
            // 注册WebSocket事件监听
            RegisterWebSocketEvents();

            // 自动加载房间列表
            if (LoadOnStart)
            {
                LoadRoomList();
            }
        }

        private void OnDestroy()
        {
            // 取消注册WebSocket事件
            UnregisterWebSocketEvents();
        }

        /// <summary>
        /// 注册WebSocket事件监听
        /// </summary>
        private void RegisterWebSocketEvents()
        {
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.Register("room/created", OnRoomCreated);
                WebSocketManager.Instance.Register("room/updated", OnRoomUpdated);
                WebSocketManager.Instance.Register("room/destroyed", OnRoomDestroyed);
                WebSocketManager.Instance.Register("lobby/sync", OnLobbySync);
            }
        }

        /// <summary>
        /// 取消注册WebSocket事件
        /// </summary>
        private void UnregisterWebSocketEvents()
        {
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.Unregister("room/created", OnRoomCreated);
                WebSocketManager.Instance.Unregister("room/updated", OnRoomUpdated);
                WebSocketManager.Instance.Unregister("room/destroyed", OnRoomDestroyed);
                WebSocketManager.Instance.Unregister("lobby/sync", OnLobbySync);
            }
        }

        /// <summary>
        /// 加载房间列表
        /// </summary>
        public void LoadRoomList()
        {
            Debug.Log("[RoomListUI] 开始加载房间列表");

            WebSocketManager.Instance.Send("room/list", null, (res) =>
            {
                if (res.success)
                {
                    Debug.Log($"[RoomListUI] 房间列表加载成功");

                    // 清空现有列表
                    ClearRoomList();

                    // 解析房间列表
                    if (res.data != null && res.data.ContainsKey("rooms"))
                    {
                        var rooms = res.data["rooms"] as Newtonsoft.Json.Linq.JArray;
                        if (rooms != null)
                        {
                            foreach (var room in rooms)
                            {
                                string roomId = room["id"]?.ToString();
                                string roomName = room["name"]?.ToString();
                                int playerCount = room["playerCount"]?.ToObject<int>() ?? 0;
                                int maxPlayers = room["maxPlayers"]?.ToObject<int>() ?? 0;
                                bool hasPassword = room["hasPassword"]?.ToObject<bool>() ?? false;
                                string status = room["status"]?.ToString();
                                string chartUrl = room["chartUrl"]?.ToString();
                                string chartName = room["chartName"]?.ToString();
                                int ownerId = room["ownerId"]?.ToObject<int>() ?? 0;

                                CreateRoomItem(roomId, roomName, playerCount, maxPlayers, hasPassword, status, chartUrl, chartName, ownerId);
                            }

                            Debug.Log($"[RoomListUI] 加载了 {rooms.Count} 个房间");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[RoomListUI] 加载房间列表失败: {res.message}");
                    ScrAlert.Show($"加载房间列表失败: {res.message}", true);
                }
            });
        }

        /// <summary>
        /// 清空房间列表
        /// </summary>
        private void ClearRoomList()
        {
            foreach (var item in _roomItems.Values)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _roomItems.Clear();
        }

        /// <summary>
        /// 创建房间项
        /// </summary>
        private void CreateRoomItem(string roomId, string roomName, int playerCount, int maxPlayers, bool hasPassword, string status, string chartUrl, string chartName, int ownerId)
        {
            if (RoomItemPrefab == null || RoomListContent == null)
            {
                Debug.LogError("[RoomListUI] RoomItemPrefab 或 RoomListContent 未设置");
                return;
            }

            // 检查是否已存在
            if (_roomItems.ContainsKey(roomId))
            {
                Debug.LogWarning($"[RoomListUI] 房间 {roomId} 已存在，跳过创建");
                return;
            }

            // 实例化房间项
            GameObject itemObj = Instantiate(RoomItemPrefab, RoomListContent);
            RoomItem roomItem = itemObj.GetComponent<RoomItem>();

            if (roomItem != null)
            {
                // 设置房间数据
                roomItem.SetRoomData(roomId, roomName, playerCount, maxPlayers, hasPassword, status, chartUrl, chartName, ownerId);

                // 添加到字典
                _roomItems[roomId] = roomItem;

                Debug.Log($"[RoomListUI] 创建房间项: {roomId} - {roomName}");
            }
            else
            {
                Debug.LogError("[RoomListUI] RoomItemPrefab 上没有 RoomItem 组件");
                Destroy(itemObj);
            }
        }

        /// <summary>
        /// 移除房间项
        /// </summary>
        private void RemoveRoomItem(string roomId)
        {
            if (_roomItems.TryGetValue(roomId, out RoomItem roomItem))
            {
                Destroy(roomItem.gameObject);
                _roomItems.Remove(roomId);
                Debug.Log($"[RoomListUI] 移除房间项: {roomId}");
            }
        }

        /// <summary>
        /// 房间创建事件
        /// </summary>
        private void OnRoomCreated(ResponseMessage msg)
        {
            Debug.Log("[RoomListUI] 收到 room/created 事件");

            if (msg.data != null && msg.data.ContainsKey("room"))
            {
                var room = msg.data["room"] as Newtonsoft.Json.Linq.JObject;
                if (room != null)
                {
                    string roomId = room["id"]?.ToString();
                    string roomName = room["name"]?.ToString();
                    int playerCount = room["playerCount"]?.ToObject<int>() ?? 0;
                    int maxPlayers = room["maxPlayers"]?.ToObject<int>() ?? 0;
                    bool hasPassword = room["hasPassword"]?.ToObject<bool>() ?? false;
                    string status = room["status"]?.ToString();
                    string chartUrl = room["chartUrl"]?.ToString();
                    string chartName = room["chartName"]?.ToString();
                    int ownerId = room["ownerId"]?.ToObject<int>() ?? 0;

                    CreateRoomItem(roomId, roomName, playerCount, maxPlayers, hasPassword, status, chartUrl, chartName, ownerId);
                }
            }
        }

        /// <summary>
        /// 房间更新事件
        /// </summary>
        private void OnRoomUpdated(ResponseMessage msg)
        {
            Debug.Log("[RoomListUI] 收到 room/updated 事件");

            if (msg.data != null && msg.data.ContainsKey("roomId"))
            {
                string roomId = msg.data["roomId"]?.ToString();

                if (_roomItems.TryGetValue(roomId, out RoomItem roomItem))
                {
                    // 提取更新的字段
                    string roomName = msg.data.ContainsKey("name") ? msg.data["name"]?.ToString() : null;
                    int? playerCount = msg.data.ContainsKey("playerCount") ? (msg.data["playerCount"] as Newtonsoft.Json.Linq.JToken)?.ToObject<int>() : null;
                    int? maxPlayers = msg.data.ContainsKey("maxPlayers") ? (msg.data["maxPlayers"] as Newtonsoft.Json.Linq.JToken)?.ToObject<int>() : null;
                    bool? hasPassword = msg.data.ContainsKey("hasPassword") ? (msg.data["hasPassword"] as Newtonsoft.Json.Linq.JToken)?.ToObject<bool>() : null;
                    string status = msg.data.ContainsKey("status") ? msg.data["status"]?.ToString() : null;
                    string chartUrl = msg.data.ContainsKey("chartUrl") ? msg.data["chartUrl"]?.ToString() : null;
                    string chartName = msg.data.ContainsKey("chartName") ? msg.data["chartName"]?.ToString() : null;
                    int? ownerId = msg.data.ContainsKey("ownerId") ? (msg.data["ownerId"] as Newtonsoft.Json.Linq.JToken)?.ToObject<int>() : null;

                    // 更新房间项
                    roomItem.UpdateRoomData(roomName, playerCount, maxPlayers, hasPassword, status, chartUrl, chartName, ownerId);

                    Debug.Log($"[RoomListUI] 更新房间项: {roomId}");
                }
                else
                {
                    Debug.LogWarning($"[RoomListUI] 房间 {roomId} 不存在，无法更新");
                }
            }
        }

        /// <summary>
        /// 房间销毁事件
        /// </summary>
        private void OnRoomDestroyed(ResponseMessage msg)
        {
            Debug.Log("[RoomListUI] 收到 room/destroyed 事件");

            if (msg.data != null && msg.data.ContainsKey("roomId"))
            {
                string roomId = msg.data["roomId"]?.ToString();
                RemoveRoomItem(roomId);
            }
        }

        /// <summary>
        /// 大厅定时同步事件 - 每5秒接收完整的房间列表
        /// 用于解决网络丢包导致的状态不同步问题
        /// </summary>
        private void OnLobbySync(ResponseMessage msg)
        {
            Debug.Log("[RoomListUI] 收到 lobby/sync 定时广播");

            if (msg.data != null && msg.data.ContainsKey("rooms"))
            {
                var rooms = msg.data["rooms"] as Newtonsoft.Json.Linq.JArray;
                if (rooms != null)
                {
                    SyncRoomList(rooms);
                }
            }
        }

        /// <summary>
        /// 同步房间列表 - 使用完全替换策略
        /// </summary>
        private void SyncRoomList(Newtonsoft.Json.Linq.JArray rooms)
        {
            // 收集服务器返回的所有房间ID
            HashSet<string> serverRoomIds = new HashSet<string>();

            foreach (var room in rooms)
            {
                string roomId = room["id"]?.ToString();
                if (string.IsNullOrEmpty(roomId)) continue;

                serverRoomIds.Add(roomId);

                string roomName = room["name"]?.ToString();
                int playerCount = room["playerCount"]?.ToObject<int>() ?? 0;
                int maxPlayers = room["maxPlayers"]?.ToObject<int>() ?? 0;
                bool hasPassword = room["hasPassword"]?.ToObject<bool>() ?? false;
                string status = room["status"]?.ToString();
                string chartUrl = room["chartUrl"]?.ToString();
                string chartName = room["chartName"]?.ToString();
                int ownerId = room["ownerId"]?.ToObject<int>() ?? 0;

                // 如果房间已存在，更新数据；否则创建新房间项
                if (_roomItems.TryGetValue(roomId, out RoomItem existingRoom))
                {
                    existingRoom.UpdateRoomData(roomName, playerCount, maxPlayers, hasPassword, status, chartUrl, chartName, ownerId);
                }
                else
                {
                    CreateRoomItem(roomId, roomName, playerCount, maxPlayers, hasPassword, status, chartUrl, chartName, ownerId);
                }
            }

            // 移除本地存在但服务器不存在的房间（已被删除的房间）
            List<string> roomsToRemove = new List<string>();
            foreach (var roomId in _roomItems.Keys)
            {
                if (!serverRoomIds.Contains(roomId))
                {
                    roomsToRemove.Add(roomId);
                }
            }

            foreach (var roomId in roomsToRemove)
            {
                RemoveRoomItem(roomId);
            }

            Debug.Log($"[RoomListUI] 同步完成，当前房间数: {_roomItems.Count}");
        }

        /// <summary>
        /// 刷新房间列表（公开方法，可由按钮调用）
        /// </summary>
        public void RefreshRoomList()
        {
            LoadRoomList();
        }
    }
}
