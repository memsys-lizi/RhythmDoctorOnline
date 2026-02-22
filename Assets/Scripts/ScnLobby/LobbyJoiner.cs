using UnityEngine;
using RDOnline.Network;

namespace RDOnline.Lobby
{
    /// <summary>
    /// 大厅加入器 - 场景加载时自动加入大厅
    /// </summary>
    public class LobbyJoiner : MonoBehaviour
    {
        private void Start()
        {
            JoinLobby();
        }

        /// <summary>
        /// 加入大厅
        /// </summary>
        private void JoinLobby()
        {
            if (!WebSocketManager.Instance.IsConnected)
            {
                Debug.LogWarning("[LobbyJoiner] 未连接服务器，无法加入大厅");
                return;
            }

            WebSocketManager.Instance.Send("lobby/join", new { }, (res) =>
            {
                if (res.Success)
                    Debug.Log($"[LobbyJoiner] {res.Message}");
                else
                    Debug.LogError($"[LobbyJoiner] 加入大厅失败: {res.Message}");
            });
        }
    }
}
