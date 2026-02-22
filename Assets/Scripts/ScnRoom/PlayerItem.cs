using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RDOnline.Utils;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 玩家项组件 - 显示单个玩家的信息
    /// </summary>
    public class PlayerItem : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("玩家头像")]
        public RawImage AvatarImage;
        [Tooltip("头像框（URL 为空时禁用）")]
        public RawImage AvatarFrameImage;
        [Tooltip("玩家用户名")]
        public TMP_Text UsernameText;
        [Tooltip("房主图标")]
        public Image OwnerIcon;
        [Tooltip("准备图标")]
        public Image ReadyIcon;
        [Tooltip("转让房主按钮")]
        public Button TransferOwnerButton;
        [Tooltip("踢出玩家按钮")]
        public Button KickButton;

        private int _userId;
        private bool _isOwner;
        private bool _isReady;

        // 确认状态
        private bool _transferOwnerConfirming = false;
        private bool _kickConfirming = false;

        /// <summary>
        /// 设置玩家数据
        /// </summary>
        public void SetPlayerData(int userId, string username, string avatar, string avatarFrame, bool isOwner, bool isReady, string nameColor = null)
        {
            _userId = userId;
            _isOwner = isOwner;
            _isReady = isReady;

            // 更新用户名 + name_color
            if (UsernameText != null)
            {
                UsernameText.text = username;
                NameColorHelper.ApplyNameColor(UsernameText, nameColor);
            }

            // 更新房主图标
            if (OwnerIcon != null)
                OwnerIcon.gameObject.SetActive(isOwner);

            // 更新准备图标
            if (ReadyIcon != null)
                ReadyIcon.gameObject.SetActive(isReady);

            // 加载头像
            if (!string.IsNullOrEmpty(avatar))
            {
                LoadAvatar(avatar);
            }

            // 头像框：有 URL 则加载并显示，否则禁用
            if (AvatarFrameImage != null)
            {
                if (!string.IsNullOrEmpty(avatarFrame))
                {
                    AvatarFrameImage.gameObject.SetActive(true);
                    LoadAvatarFrame(avatarFrame);
                }
                else
                {
                    AvatarFrameImage.gameObject.SetActive(false);
                }
            }

            // 初始化按钮
            InitializeButtons();
        }

        /// <summary>
        /// 更新准备状态
        /// </summary>
        public void UpdateReadyState(bool isReady)
        {
            _isReady = isReady;
            if (ReadyIcon != null)
                ReadyIcon.gameObject.SetActive(isReady);
        }

        /// <summary>
        /// 获取玩家ID
        /// </summary>
        public int GetUserId()
        {
            return _userId;
        }

        /// <summary>
        /// 刷新按钮显示状态（用于房主变更时）
        /// </summary>
        public void RefreshButtonsVisibility()
        {
            InitializeButtons();
        }

        /// <summary>
        /// 加载头像
        /// </summary>
        private void LoadAvatar(string avatarUrl)
        {
            if (AvatarImage == null) return;

            StartCoroutine(ResourceLoader.LoadTexture(avatarUrl,
                (texture) =>
                {
                    if (AvatarImage != null)
                    {
                        AvatarImage.texture = texture;
                    }
                },
                (error) =>
                {
                    Debug.LogWarning($"[PlayerItem] 加载头像失败: {error}");
                }
            ));
        }

        /// <summary>
        /// 加载头像框
        /// </summary>
        private void LoadAvatarFrame(string url)
        {
            if (AvatarFrameImage == null) return;

            StartCoroutine(ResourceLoader.LoadTexture(url,
                (texture) =>
                {
                    if (AvatarFrameImage != null)
                    {
                        AvatarFrameImage.texture = texture;
                    }
                },
                (error) =>
                {
                    Debug.LogWarning($"[PlayerItem] 加载头像框失败: {error}");
                    if (AvatarFrameImage != null)
                    {
                        AvatarFrameImage.gameObject.SetActive(false);
                    }
                }
            ));
        }

        /// <summary>
        /// 转让房主按钮点击事件
        /// </summary>
        private void OnTransferOwnerButtonClick()
        {
            if (!_transferOwnerConfirming)
            {
                // 第一次点击，显示确认提示
                _transferOwnerConfirming = true;
                ScrAlert.Show("再次点击转让房主", true);

                // 3秒后重置确认状态
                StartCoroutine(ResetTransferOwnerConfirm());
            }
            else
            {
                // 第二次点击，执行转让
                _transferOwnerConfirming = false;
                SendTransferOwnerRequest();
            }
        }

        /// <summary>
        /// 踢出玩家按钮点击事件
        /// </summary>
        private void OnKickButtonClick()
        {
            if (!_kickConfirming)
            {
                // 第一次点击，显示确认提示
                _kickConfirming = true;
                ScrAlert.Show("再次点击踢出玩家", true);

                // 3秒后重置确认状态
                StartCoroutine(ResetKickConfirm());
            }
            else
            {
                // 第二次点击，执行踢出
                _kickConfirming = false;
                SendKickRequest();
            }
        }

        /// <summary>
        /// 初始化按钮
        /// </summary>
        private void InitializeButtons()
        {
            // 判断当前登录用户是否是房主
            bool isCurrentUserOwner = (RoomData.Instance != null &&
                                       RDOnline.UserData.Instance != null &&
                                       RoomData.Instance.OwnerId == RDOnline.UserData.Instance.Id);

            // 判断是否是自己
            bool isSelf = (RDOnline.UserData.Instance != null && _userId == RDOnline.UserData.Instance.Id);

            // 只有当前用户是房主，且不是自己时，才显示按钮
            bool shouldShowButtons = isCurrentUserOwner && !isSelf;

            // 设置按钮显示状态
            if (TransferOwnerButton != null)
            {
                TransferOwnerButton.gameObject.SetActive(shouldShowButtons);
                TransferOwnerButton.onClick.RemoveAllListeners();
                TransferOwnerButton.onClick.AddListener(OnTransferOwnerButtonClick);
            }

            if (KickButton != null)
            {
                KickButton.gameObject.SetActive(shouldShowButtons);
                KickButton.onClick.RemoveAllListeners();
                KickButton.onClick.AddListener(OnKickButtonClick);
            }
        }

        /// <summary>
        /// 发送转让房主请求
        /// </summary>
        private void SendTransferOwnerRequest()
        {
            if (RDOnline.Network.WebSocketManager.Instance == null || !RDOnline.Network.WebSocketManager.Instance.IsConnected)
            {
                ScrAlert.Show("未连接服务器", true);
                return;
            }

            Debug.Log($"[PlayerItem] 发送转让房主请求: {_userId}");

            var data = new { userId = _userId };

            RDOnline.Network.WebSocketManager.Instance.Send("room/transferOwner", data, (res) =>
            {
                if (res.success)
                {
                    Debug.Log("[PlayerItem] 转让房主成功");
                    ScrAlert.Show("房主已转移", true);
                }
                else
                {
                    Debug.LogError($"[PlayerItem] 转让房主失败: {res.message}");
                    ScrAlert.Show($"转让失败: {res.message}", true);
                }
            });
        }

        /// <summary>
        /// 发送踢出玩家请求
        /// </summary>
        private void SendKickRequest()
        {
            if (RDOnline.Network.WebSocketManager.Instance == null || !RDOnline.Network.WebSocketManager.Instance.IsConnected)
            {
                ScrAlert.Show("未连接服务器", true);
                return;
            }

            Debug.Log($"[PlayerItem] 发送踢出玩家请求: {_userId}");

            var data = new { userId = _userId };

            RDOnline.Network.WebSocketManager.Instance.Send("room/kick", data, (res) =>
            {
                if (res.success)
                {
                    Debug.Log("[PlayerItem] 踢出玩家成功");
                    ScrAlert.Show("玩家已被踢出", true);
                }
                else
                {
                    Debug.LogError($"[PlayerItem] 踢出玩家失败: {res.message}");
                    ScrAlert.Show($"踢出失败: {res.message}", true);
                }
            });
        }

        /// <summary>
        /// 重置转让房主确认状态
        /// </summary>
        private System.Collections.IEnumerator ResetTransferOwnerConfirm()
        {
            yield return new WaitForSeconds(3f);
            _transferOwnerConfirming = false;
        }

        /// <summary>
        /// 重置踢出玩家确认状态
        /// </summary>
        private System.Collections.IEnumerator ResetKickConfirm()
        {
            yield return new WaitForSeconds(3f);
            _kickConfirming = false;
        }
    }
}

