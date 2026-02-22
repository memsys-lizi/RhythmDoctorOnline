using System.Collections.Generic;
using UnityEngine;
using RDOnline.Network;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 玩家列表UI管理器 - 管理房间内玩家列表的显示
    /// </summary>
    public class PlayerListUI : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("玩家列表容器（ScrollView的Content）")]
        public Transform PlayerListContent;
        [Tooltip("玩家项预制体")]
        public GameObject PlayerItemPrefab;

        // 玩家项字典，key为userId
        private Dictionary<int, PlayerItem> _playerItems = new Dictionary<int, PlayerItem>();

        private void Start()
        {
            // 从 RoomData 加载初始玩家列表
            LoadPlayersFromRoomData();

            // 注册 WebSocket 事件监听
            RegisterWebSocketEvents();
        }

        private void OnDestroy()
        {
            // 取消注册 WebSocket 事件监听
            UnregisterWebSocketEvents();
        }

        /// <summary>
        /// 从 RoomData 加载玩家列表
        /// </summary>
        public void LoadPlayersFromRoomData()
        {
            if (RoomData.Instance == null || RoomData.Instance.Players == null)
            {
                Debug.LogWarning("[PlayerListUI] RoomData 或玩家列表为空");
                return;
            }

            // 清空现有列表
            ClearPlayerList();

            // 创建玩家项
            foreach (var playerData in RoomData.Instance.Players)
            {
                bool isOwner = (playerData.UserId == RoomData.Instance.OwnerId);
                CreatePlayerItem(playerData.UserId, playerData.Username, playerData.Avatar, playerData.AvatarFrame, isOwner, playerData.Ready, playerData.NameColor);
            }

            Debug.Log($"[PlayerListUI] 加载了 {RoomData.Instance.Players.Count} 个玩家");
        }

        /// <summary>
        /// 清空玩家列表
        /// </summary>
        private void ClearPlayerList()
        {
            foreach (var item in _playerItems.Values)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _playerItems.Clear();
        }

        /// <summary>
        /// 创建玩家项
        /// </summary>
        private void CreatePlayerItem(int userId, string username, string avatar, string avatarFrame, bool isOwner, bool isReady, string nameColor = null)
        {
            if (PlayerItemPrefab == null || PlayerListContent == null)
            {
                Debug.LogError("[PlayerListUI] PlayerItemPrefab 或 PlayerListContent 未设置");
                return;
            }

            // 检查是否已存在
            if (_playerItems.ContainsKey(userId))
            {
                Debug.LogWarning($"[PlayerListUI] 玩家 {userId} 已存在，跳过创建");
                return;
            }

            // 实例化玩家项
            GameObject itemObj = Instantiate(PlayerItemPrefab, PlayerListContent);
            PlayerItem playerItem = itemObj.GetComponent<PlayerItem>();

            if (playerItem != null)
            {
                // 设置玩家数据
                playerItem.SetPlayerData(userId, username, avatar, avatarFrame, isOwner, isReady, nameColor);

                // 添加到字典
                _playerItems[userId] = playerItem;

                Debug.Log($"[PlayerListUI] 创建玩家项: {userId} - {username}");
            }
            else
            {
                Debug.LogError("[PlayerListUI] PlayerItemPrefab 上没有 PlayerItem 组件");
                Destroy(itemObj);
            }
        }

        /// <summary>
        /// 添加玩家（用于玩家加入事件）
        /// </summary>
        public void AddPlayer(int userId, string username, string avatar, string avatarFrame, bool isReady, string nameColor = null)
        {
            bool isOwner = (userId == RoomData.Instance.OwnerId);
            CreatePlayerItem(userId, username, avatar, avatarFrame, isOwner, isReady, nameColor);
        }

        /// <summary>
        /// 移除玩家（用于玩家离开事件）
        /// </summary>
        public void RemovePlayer(int userId)
        {
            if (_playerItems.TryGetValue(userId, out PlayerItem playerItem))
            {
                Destroy(playerItem.gameObject);
                _playerItems.Remove(userId);
                Debug.Log($"[PlayerListUI] 移除玩家项: {userId}");
            }
        }

        /// <summary>
        /// 更新玩家准备状态
        /// </summary>
        public void UpdatePlayerReady(int userId, bool isReady)
        {
            if (_playerItems.TryGetValue(userId, out PlayerItem playerItem))
            {
                playerItem.UpdateReadyState(isReady);
                Debug.Log($"[PlayerListUI] 更新玩家准备状态: {userId} - {isReady}");
            }
        }

        /// <summary>
        /// 处理玩家加入事件
        /// </summary>
        private void OnPlayerJoin(ResponseMessage msg)
        {
            if (msg.data == null) return;

            try
            {
                // 根据API文档,玩家数据嵌套在 player 对象中
                if (!msg.data.ContainsKey("player"))
                {
                    Debug.LogError("[PlayerListUI] room/playerJoin 事件缺少 player 字段");
                    return;
                }

                var playerToken = msg.data["player"] as Newtonsoft.Json.Linq.JToken;
                if (playerToken == null)
                {
                    Debug.LogError("[PlayerListUI] player 字段解析失败");
                    return;
                }

                // 解析玩家数据
                int userId = playerToken["userId"]?.ToObject<int>() ?? 0;
                string username = playerToken["username"]?.ToString();
                string avatar = playerToken["avatar"]?.ToString();
                string avatarFrame = playerToken["avatarFrame"]?.ToString();
                string nameColor = playerToken["nameColor"]?.ToString();
                // 注意: room/playerJoin 事件中没有 ready 字段,新加入的玩家默认未准备
                bool ready = false;

                Debug.Log($"[PlayerListUI] 玩家加入: {userId} - {username}");

                // 更新 RoomData
                if (RoomData.Instance != null)
                {
                    RoomData.Instance.AddPlayer(new RoomPlayerData
                    {
                        UserId = userId,
                        Username = username,
                        NameColor = nameColor,
                        Avatar = avatar,
                        AvatarFrame = avatarFrame,
                        Ready = ready
                    });
                }

                // 更新 UI
                AddPlayer(userId, username, avatar, avatarFrame, ready, nameColor);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerListUI] 处理玩家加入事件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理玩家离开事件
        /// </summary>
        private void OnPlayerLeave(ResponseMessage msg)
        {
            if (msg.data == null) return;

            try
            {
                // 解析玩家ID
                int userId = (msg.data["userId"] as Newtonsoft.Json.Linq.JToken)?.ToObject<int>() ?? 0;

                Debug.Log($"[PlayerListUI] 玩家离开: {userId}");

                // 更新 RoomData
                if (RoomData.Instance != null)
                {
                    RoomData.Instance.RemovePlayer(userId);
                }

                // 更新 UI
                RemovePlayer(userId);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerListUI] 处理玩家离开事件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理玩家准备状态变化事件
        /// </summary>
        private void OnPlayerReady(ResponseMessage msg)
        {
            if (msg.data == null) return;

            try
            {
                // 解析玩家数据
                int userId = (msg.data["userId"] as Newtonsoft.Json.Linq.JToken)?.ToObject<int>() ?? 0;
                bool ready = (msg.data["ready"] as Newtonsoft.Json.Linq.JToken)?.ToObject<bool>() ?? false;

                Debug.Log($"[PlayerListUI] 玩家准备状态变化: {userId} - {ready}");

                // 更新 RoomData
                if (RoomData.Instance != null)
                {
                    RoomData.Instance.UpdatePlayerReady(userId, ready);
                }

                // 更新 UI
                UpdatePlayerReady(userId, ready);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerListUI] 处理玩家准备状态变化事件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理房间更新事件
        /// </summary>
        private void OnRoomUpdated(ResponseMessage msg)
        {
            if (msg.data == null) return;

            try
            {
                Debug.Log("[PlayerListUI] 房间信息更新");

                // 检查是否有房主变更
                if (msg.data.ContainsKey("ownerId"))
                {
                    int newOwnerId = (msg.data["ownerId"] as Newtonsoft.Json.Linq.JToken)?.ToObject<int>() ?? 0;

                    if (RoomData.Instance != null && RoomData.Instance.OwnerId != newOwnerId)
                    {
                        Debug.Log($"[PlayerListUI] 房主变更: {RoomData.Instance.OwnerId} -> {newOwnerId}");
                        RoomData.Instance.OwnerId = newOwnerId;

                        // 刷新所有玩家项的房主图标
                        RefreshOwnerIcons(newOwnerId);
                    }
                }

                // 更新其他房间信息到 RoomData
                if (RoomData.Instance != null)
                {
                    if (msg.data.ContainsKey("status"))
                    {
                        RoomData.Instance.Status = msg.data["status"]?.ToString();
                    }
                    if (msg.data.ContainsKey("chartUrl"))
                    {
                        RoomData.Instance.ChartUrl = msg.data["chartUrl"]?.ToString();
                    }
                    if (msg.data.ContainsKey("chartName"))
                    {
                        RoomData.Instance.ChartName = msg.data["chartName"]?.ToString();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerListUI] 处理房间更新事件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 刷新所有玩家项的房主图标
        /// </summary>
        private void RefreshOwnerIcons(int newOwnerId)
        {
            foreach (var kvp in _playerItems)
            {
                int userId = kvp.Key;
                PlayerItem playerItem = kvp.Value;

                if (playerItem != null)
                {
                    bool isOwner = (userId == newOwnerId);

                    // 刷新房主图标
                    if (playerItem.OwnerIcon != null)
                    {
                        playerItem.OwnerIcon.gameObject.SetActive(isOwner);
                    }

                    // 刷新按钮状态
                    playerItem.RefreshButtonsVisibility();
                }
            }

            Debug.Log($"[PlayerListUI] 已刷新所有玩家的房主图标和按钮状态，新房主ID: {newOwnerId}");
        }

        /// <summary>
        /// 处理被踢出事件
        /// </summary>
        private void OnKicked(ResponseMessage msg)
        {
            Debug.Log("[PlayerListUI] 收到被踢出通知，即将返回大厅");

            // 显示提示
            ScrAlert.Show("你已被踢出房间", true);

            // 加载大厅场景
            ScnLoading.LoadScenes("ScnLobby");
        }

        /// <summary>
        /// 房间定时同步事件 - 每5秒接收完整的房间状态
        /// 用于解决网络丢包导致的状态不同步问题
        /// </summary>
        private void OnRoomSync(ResponseMessage msg)
        {
            if (msg.data == null) return;

            try
            {
                Debug.Log("[PlayerListUI] 收到 room/sync 定时广播");

                // 同步房间信息
                if (msg.data.ContainsKey("room"))
                {
                    var room = msg.data["room"] as Newtonsoft.Json.Linq.JObject;
                    if (room != null)
                    {
                        SyncRoomInfo(room);
                    }
                }

                // 同步玩家列表
                if (msg.data.ContainsKey("players"))
                {
                    var players = msg.data["players"] as Newtonsoft.Json.Linq.JArray;
                    if (players != null)
                    {
                        SyncPlayerList(players);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerListUI] 处理 room/sync 事件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 同步房间信息到 RoomData
        /// </summary>
        private void SyncRoomInfo(Newtonsoft.Json.Linq.JObject room)
        {
            if (RoomData.Instance == null) return;

            try
            {
                // 解析房间信息
                string roomId = room["id"]?.ToString();
                string roomName = room["name"]?.ToString();
                int maxPlayers = room["maxPlayers"]?.ToObject<int>() ?? 0;
                bool hasPassword = room["hasPassword"]?.ToObject<bool>() ?? false;
                string chartUrl = room["chartUrl"]?.ToString();
                string chartName = room["chartName"]?.ToString();
                int ownerId = room["ownerId"]?.ToObject<int>() ?? 0;
                string status = room["status"]?.ToString();

                // 检查房主是否变更
                bool ownerChanged = (RoomData.Instance.OwnerId != ownerId);

                // 更新 RoomData
                RoomData.Instance.RoomId = roomId;
                RoomData.Instance.RoomName = roomName;
                RoomData.Instance.MaxPlayers = maxPlayers;
                RoomData.Instance.HasPassword = hasPassword;
                RoomData.Instance.ChartUrl = chartUrl;
                RoomData.Instance.ChartName = chartName;
                RoomData.Instance.OwnerId = ownerId;
                RoomData.Instance.Status = status;

                // 如果房主变更，刷新房主图标
                if (ownerChanged)
                {
                    RefreshOwnerIcons(ownerId);
                }

                Debug.Log($"[PlayerListUI] 房间信息已同步: {roomName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerListUI] 同步房间信息失败: {e.Message}");
            }
        }

        /// <summary>
        /// 同步玩家列表 - 使用增量更新策略
        /// </summary>
        private void SyncPlayerList(Newtonsoft.Json.Linq.JArray players)
        {
            if (RoomData.Instance == null) return;

            try
            {
                // 收集服务器返回的所有玩家ID
                HashSet<int> serverPlayerIds = new HashSet<int>();

                foreach (var player in players)
                {
                    int userId = player["userId"]?.ToObject<int>() ?? 0;
                    if (userId == 0) continue;

                    serverPlayerIds.Add(userId);

                    string username = player["username"]?.ToString();
                    string avatar = player["avatar"]?.ToString();
                    string avatarFrame = player["avatarFrame"]?.ToString();
                    string nameColor = player["nameColor"]?.ToString();
                    bool ready = player["ready"]?.ToObject<bool>() ?? false;

                    // 如果玩家已存在，更新准备状态
                    if (_playerItems.TryGetValue(userId, out PlayerItem existingPlayer))
                    {
                        // 更新 RoomData
                        RoomData.Instance.UpdatePlayerReady(userId, ready);

                        // 更新 UI
                        existingPlayer.UpdateReadyState(ready);
                    }
                    else
                    {
                        // 玩家不存在，添加新玩家
                        RoomData.Instance.AddPlayer(new RoomPlayerData
                        {
                            UserId = userId,
                            Username = username,
                            NameColor = nameColor,
                            Avatar = avatar,
                            AvatarFrame = avatarFrame,
                            Ready = ready
                        });

                        // 创建玩家项
                        bool isOwner = (userId == RoomData.Instance.OwnerId);
                        CreatePlayerItem(userId, username, avatar, avatarFrame, isOwner, ready, nameColor);
                    }
                }

                // 移除本地存在但服务器不存在的玩家（已离开的玩家）
                List<int> playersToRemove = new List<int>();
                foreach (var userId in _playerItems.Keys)
                {
                    if (!serverPlayerIds.Contains(userId))
                    {
                        playersToRemove.Add(userId);
                    }
                }

                foreach (var userId in playersToRemove)
                {
                    RoomData.Instance.RemovePlayer(userId);
                    RemovePlayer(userId);
                }

                Debug.Log($"[PlayerListUI] 玩家列表已同步，当前玩家数: {_playerItems.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerListUI] 同步玩家列表失败: {e.Message}");
            }
        }

        /// <summary>
        /// 注册 WebSocket 事件监听
        /// </summary>
        private void RegisterWebSocketEvents()
        {
            if (WebSocketManager.Instance == null)
            {
                Debug.LogWarning("[PlayerListUI] WebSocketManager 实例不存在，无法注册事件");
                return;
            }

            WebSocketManager.Instance.Register("room/playerJoin", OnPlayerJoin);
            WebSocketManager.Instance.Register("room/playerLeave", OnPlayerLeave);
            WebSocketManager.Instance.Register("room/playerReady", OnPlayerReady);
            WebSocketManager.Instance.Register("room/updated", OnRoomUpdated);
            WebSocketManager.Instance.Register("room/kicked", OnKicked);
            WebSocketManager.Instance.Register("room/sync", OnRoomSync);

            Debug.Log("[PlayerListUI] WebSocket 事件监听已注册");
        }

        /// <summary>
        /// 取消注册 WebSocket 事件监听
        /// </summary>
        private void UnregisterWebSocketEvents()
        {
            if (WebSocketManager.Instance == null) return;

            WebSocketManager.Instance.Unregister("room/playerJoin", OnPlayerJoin);
            WebSocketManager.Instance.Unregister("room/playerLeave", OnPlayerLeave);
            WebSocketManager.Instance.Unregister("room/playerReady", OnPlayerReady);
            WebSocketManager.Instance.Unregister("room/updated", OnRoomUpdated);
            WebSocketManager.Instance.Unregister("room/kicked", OnKicked);
            WebSocketManager.Instance.Unregister("room/sync", OnRoomSync);

            Debug.Log("[PlayerListUI] WebSocket 事件监听已取消注册");
        }
    }
}
