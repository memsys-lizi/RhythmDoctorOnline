using UnityEngine;
using RDOnline.Network;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 房间加入器 - 场景加载时自动加入房间
    /// </summary>
    public class RoomJoiner : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("玩家列表UI")]
        public PlayerListUI PlayerListUI;

        private void Start()
        {
            RoomData.IsInRoomFullScreen = Screen.fullScreen;
            scnGame.pauseBlocked = false;
            // 检查是否从游戏场景返回
            if (RoomData.IsReturningFromGame)
            {
                Debug.Log("[RoomJoiner] 从游戏场景返回，跳过加入房间");
                // 不需要加入房间，ReadyManager 会自动发送 room/finish
                // 但是需要初始化 RoomManager 来下载谱面
                InitializeRoomManager();
                return;
            }

            // 正常加入房间流程
            JoinRoom();
        }

        /// <summary>
        /// 加入房间
        /// </summary>
        private void JoinRoom()
        {
            // 检查连接状态
            if (!WebSocketManager.Instance.IsConnected)
            {
                Debug.LogError("[RoomJoiner] 未连接服务器，无法加入房间");
                ScrAlert.Show("未连接服务器", true);
                return;
            }

            // 检查 RoomData 是否有效
            if (RoomData.Instance == null || !RoomData.Instance.IsInRoom)
            {
                Debug.LogError("[RoomJoiner] RoomData 无效，无法加入房间");
                ScrAlert.Show("房间数据无效", true);
                return;
            }

            // 从 RoomData 获取房间信息
            string roomId = RoomData.Instance.RoomId;
            string password = RoomData.Instance.Password;

            Debug.Log($"[RoomJoiner] 开始加入房间: {roomId}");

            // 构建请求数据
            var data = new
            {
                roomId = roomId,
                password = password
            };

            // 发送加入房间请求
            WebSocketManager.Instance.Send("room/join", data, (res) =>
            {
                if (res.success)
                {
                    Debug.Log($"[RoomJoiner] 加入房间成功: {res.message}");
                    ScrAlert.Show("成功加入房间", true);

                    // 加入成功后，主动获取房间完整信息
                    // 这样可以确保获取到最新的玩家列表（特别是房主创建房间的情况）
                    FetchRoomInfo(roomId);
                }
                else
                {
                    Debug.LogError($"[RoomJoiner] 加入房间失败: {res.message}");
                    ScrAlert.Show($"加入房间失败: {res.message}", true);
                    // TODO: 返回大厅场景
                }
            });
        }

        /// <summary>
        /// 获取房间完整信息
        /// </summary>
        private void FetchRoomInfo(string roomId)
        {
            var data = new
            {
                roomId = roomId
            };

            Debug.Log($"[RoomJoiner] 开始获取房间信息: {roomId}");

            WebSocketManager.Instance.Send("room/info", data, (res) =>
            {
                if (res.success)
                {
                    Debug.Log("[RoomJoiner] 获取房间信息成功");

                    if (res.data != null)
                    {
                        // 更新房间信息
                        if (res.data.ContainsKey("room"))
                        {
                            var room = res.data["room"] as Newtonsoft.Json.Linq.JObject;
                            if (room != null)
                            {
                                UpdateRoomData(room);
                            }
                        }

                        // 解析玩家列表
                        if (res.data.ContainsKey("players"))
                        {
                            var players = res.data["players"] as Newtonsoft.Json.Linq.JArray;
                            if (players != null)
                            {
                                Debug.Log($"[RoomJoiner] 房间内有 {players.Count} 个玩家");
                                ParseAndSavePlayers(players);

                                // 通知 PlayerListUI 重新加载玩家列表
                                NotifyPlayerListUI();
                            }
                        }
                    }

                    // 获取房间信息成功后，初始化 RoomManager
                    InitializeRoomManager();
                }
                else
                {
                    Debug.LogError($"[RoomJoiner] 获取房间信息失败: {res.message}");
                }
            });
        }

        /// <summary>
        /// 更新房间数据
        /// </summary>
        private void UpdateRoomData(Newtonsoft.Json.Linq.JObject room)
        {
            if (RoomData.Instance == null) return;

            // 从响应中提取房间信息
            string roomId = room["id"]?.ToString();
            string roomName = room["name"]?.ToString();
            int maxPlayers = room["maxPlayers"]?.ToObject<int>() ?? 0;
            bool hasPassword = room["hasPassword"]?.ToObject<bool>() ?? false;
            string chartUrl = room["chartUrl"]?.ToString();
            string chartName = room["chartName"]?.ToString();
            int ownerId = room["ownerId"]?.ToObject<int>() ?? 0;
            string status = room["status"]?.ToString();
            int playerCount = room["playerCount"]?.ToObject<int>() ?? 0;

            // 更新 RoomData（保留原有的密码）
            string password = RoomData.Instance.Password;
            RoomData.Instance.SetCurrentRoom(
                roomId, roomName, maxPlayers, hasPassword, password,
                chartUrl, chartName, ownerId, status, playerCount
            );

            Debug.Log($"[RoomJoiner] 房间数据已更新: {roomName} ({playerCount}/{maxPlayers})");
        }

        /// <summary>
        /// 解析并保存玩家列表
        /// </summary>
        private void ParseAndSavePlayers(Newtonsoft.Json.Linq.JArray players)
        {
            if (RoomData.Instance == null) return;

            var playerList = new System.Collections.Generic.List<RoomPlayerData>();

            foreach (var player in players)
            {
                int userId = player["userId"]?.ToObject<int>() ?? 0;
                string username = player["username"]?.ToString();
                string avatar = player["avatar"]?.ToString();
                string avatarFrame = player["avatarFrame"]?.ToString();
                string nameColor = player["nameColor"]?.ToString();

                // 注意: room/info 接口返回的玩家列表可能包含 ready 字段(room/join)，也可能不包含(room/info)
                // 如果不包含，默认为 false
                bool ready = false;
                if (player["ready"] != null)
                {
                    ready = player["ready"].ToObject<bool>();
                }

                playerList.Add(new RoomPlayerData
                {
                    UserId = userId,
                    Username = username,
                    NameColor = nameColor,
                    Avatar = avatar,
                    AvatarFrame = avatarFrame,
                    Ready = ready
                });

                Debug.Log($"[RoomJoiner] 解析玩家: {userId} - {username}, ready={ready}");
            }

            RoomData.Instance.SetPlayers(playerList);
            Debug.Log($"[RoomJoiner] 玩家列表已保存到 RoomData，共 {playerList.Count} 个玩家");
        }

        /// <summary>
        /// 通知 PlayerListUI 重新加载玩家列表
        /// </summary>
        private void NotifyPlayerListUI()
        {
            if (PlayerListUI != null)
            {
                PlayerListUI.LoadPlayersFromRoomData();
                Debug.Log("[RoomJoiner] 已通知 PlayerListUI 重新加载玩家列表");
            }
            else
            {
                Debug.LogWarning("[RoomJoiner] PlayerListUI 引用未设置");
            }
        }

        /// <summary>
        /// 初始化 RoomManager
        /// </summary>
        private void InitializeRoomManager()
        {
            if (RoomManager.Instance == null)
            {
                Debug.LogError("[RoomJoiner] RoomManager 实例不存在");
                return;
            }

            Debug.Log("[RoomJoiner] 初始化 RoomManager");
            RoomManager.Instance.Initialize();
        }
    }
}


