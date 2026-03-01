using System.Collections.Generic;
using RDOnline.Utils;
using UnityEngine;

namespace RDOnline
{
    /// <summary>
    /// 房间玩家数据
    /// </summary>
    [System.Serializable]
    public class RoomPlayerData
    {
        public int UserId;
        public string Username;
        public string NameColor;
        public string Avatar;
        public string AvatarFrame;
        public bool Ready;
    }

    /// <summary>
    /// 房间数据管理器，存储当前房间信息
    /// </summary>
    public class RoomData : MonoBehaviour
    {
        public static RoomData Instance { get; private set; }
        public static RankScore CurrentScore;
        /// <summary>
        /// 标记是否从游戏场景返回（用于区分首次进入房间和游戏结束返回）
        /// </summary>
        public static bool IsReturningFromGame = false;

        public static bool IsInRoomFullScreen = Screen.fullScreen;
        [Header("当前房间信息")]
        [Tooltip("房间ID")]
        public string RoomId;
        [Tooltip("房间名称")]
        public string RoomName;
        [Tooltip("最大人数")]
        public int MaxPlayers;
        [Tooltip("是否有密码")]
        public bool HasPassword;
        [Tooltip("房间密码")]
        public string Password;
        [Tooltip("谱面URL")]
        public string ChartUrl;
        [Tooltip("谱面名称")]
        public string ChartName;
        [Tooltip("房主ID")]
        public int OwnerId;
        [Tooltip("房间状态 (waiting/playing/finished)")]
        public string Status;
        [Tooltip("当前人数")]
        public int PlayerCount;

        [Header("房间玩家列表")]
        [Tooltip("房间内的玩家列表")]
        public List<RoomPlayerData> Players = new List<RoomPlayerData>();

        /// <summary>
        /// 是否在房间中
        /// </summary>
        public bool IsInRoom => !string.IsNullOrEmpty(RoomId);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
        }

        /// <summary>
        /// 设置当前房间数据
        /// </summary>
        public void SetCurrentRoom(string roomId, string roomName, int maxPlayers, bool hasPassword, string password,
            string chartUrl, string chartName, int ownerId, string status, int playerCount)
        {
            RoomId = roomId;
            RoomName = roomName;
            MaxPlayers = maxPlayers;
            HasPassword = hasPassword;
            Password = password;
            ChartUrl = chartUrl;
            ChartName = chartName;
            OwnerId = ownerId;
            Status = status;
            PlayerCount = playerCount;
        }

        /// <summary>
        /// 设置基本房间信息（创建房间时使用）
        /// </summary>
        public void SetBasicRoomInfo(string roomId, string roomName, int maxPlayers, bool hasPassword, string password,
            string chartUrl, string chartName)
        {
            RoomId = roomId;
            RoomName = roomName;
            MaxPlayers = maxPlayers;
            HasPassword = hasPassword;
            Password = password;
            ChartUrl = chartUrl;
            ChartName = chartName;
            OwnerId = UserData.Instance.Id; // 创建者就是房主
            Status = "waiting"; // 初始状态为等待
            PlayerCount = 1; // 创建者自己
        }

        /// <summary>
        /// 清除房间数据
        /// </summary>
        public void Clear()
        {
            RoomId = null;
            RoomName = null;
            MaxPlayers = 0;
            HasPassword = false;
            Password = null;
            ChartUrl = null;
            ChartName = null;
            OwnerId = 0;
            Status = null;
            PlayerCount = 0;
            Players.Clear();
        }

        /// <summary>
        /// 设置玩家列表
        /// </summary>
        public void SetPlayers(List<RoomPlayerData> players)
        {
            Players.Clear();
            if (players != null)
            {
                Players.AddRange(players);
            }
        }

        /// <summary>
        /// 添加玩家
        /// </summary>
        public void AddPlayer(RoomPlayerData player)
        {
            if (player != null && !Players.Exists(p => p.UserId == player.UserId))
            {
                Players.Add(player);
            }
        }

        /// <summary>
        /// 移除玩家
        /// </summary>
        public void RemovePlayer(int userId)
        {
            Players.RemoveAll(p => p.UserId == userId);
        }

        /// <summary>
        /// 更新玩家准备状态
        /// </summary>
        public void UpdatePlayerReady(int userId, bool ready)
        {
            var player = Players.Find(p => p.UserId == userId);
            if (player != null)
            {
                player.Ready = ready;
            }
        }
    }
}
