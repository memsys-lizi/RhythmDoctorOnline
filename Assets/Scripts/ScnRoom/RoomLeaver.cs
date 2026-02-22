using UnityEngine;
using UnityEngine.UI;
using RDOnline.Network;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 房间离开器 - 处理离开房间并返回大厅
    /// </summary>
    public class RoomLeaver : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("离开房间按钮")]
        public Button LeaveButton;

        private bool _isLeaving = false;

        private void Start()
        {
            // 绑定按钮事件
            if (LeaveButton != null)
            {
                LeaveButton.onClick.AddListener(OnLeaveButtonClick);
            }
        }

        /// <summary>
        /// 离开按钮点击事件
        /// </summary>
        private void OnLeaveButtonClick()
        {
            if (_isLeaving)
            {
                ScrAlert.Show("正在离开房间，请稍候", true);
                return;
            }

            LeaveRoom();
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        private void LeaveRoom()
        {
            // 检查连接状态
            if (!WebSocketManager.Instance.IsConnected)
            {
                Debug.LogError("[RoomLeaver] 未连接服务器");
                ScrAlert.Show("未连接服务器", true);
                return;
            }

            _isLeaving = true;

            Debug.Log("[RoomLeaver] 开始离开房间");

            // 发送离开房间请求
            WebSocketManager.Instance.Send("room/leave", new { }, (res) =>
            {
                _isLeaving = false;

                if (res.success)
                {
                    Debug.Log("[RoomLeaver] 离开房间成功");

                    // 清除房间数据
                    if (RoomData.Instance != null)
                    {
                        RoomData.Instance.Clear();
                    }

                    ScrAlert.Show("已离开房间", true);

                    // 跳转到大厅场景（LobbyJoiner 会自动加入大厅）
                    ScnLoading.LoadScenes("ScnLobby");
                }
                else
                {
                    Debug.LogError($"[RoomLeaver] 离开房间失败: {res.message}");
                    ScrAlert.Show($"离开房间失败: {res.message}", true);
                }
            });
        }
    }
}
