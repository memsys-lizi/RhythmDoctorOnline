using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RDOnline.Network;
using RDOnline.Utils;

namespace RDOnline.Auth
{
    /// <summary>
    /// 用户信息UI - 显示头像、用户名、网络延迟、在线人数
    /// </summary>
    public class UserInfoUI : MonoBehaviour
    {
        [Header("用户信息")]
        [Tooltip("用户头像")]
        public RawImage AvatarImage;
        [Tooltip("头像框（URL 为空时禁用）")]
        public RawImage AvatarFrameImage;
        [Tooltip("用户名文本")]
        public TMP_Text UsernameText;

        [Header("网络延迟")]
        [Tooltip("延迟图标")]
        public Image PingIcon;
        [Tooltip("延迟文本")]
        public TMP_Text PingText;

        [Header("在线人数")]
        [Tooltip("在线人数文本")]
        public TMP_Text OnlineCountText;

        [Header("设置")]
        [Tooltip("默认头像")]
        public Texture2D DefaultAvatar;
        [Tooltip("在线人数更新间隔（秒）")]
        public float OnlineCountUpdateInterval = 30f;

        [Header("延迟颜色设置")]
        [Tooltip("优秀延迟阈值（ms）")]
        public int ExcellentPing = 50;
        [Tooltip("良好延迟阈值（ms）")]
        public int GoodPing = 100;
        [Tooltip("一般延迟阈值（ms）")]
        public int FairPing = 200;
        [Tooltip("优秀延迟颜色")]
        public Color ExcellentColor = Color.green;
        [Tooltip("良好延迟颜色")]
        public Color GoodColor = Color.yellow;
        [Tooltip("一般延迟颜色")]
        public Color FairColor = new Color(1f, 0.5f, 0f);
        [Tooltip("差延迟颜色")]
        public Color PoorColor = Color.red;

        private void Start()
        {
            LoadUserInfo();

            // 订阅延迟更新事件
            if (NetworkPingManager.Instance != null)
            {
                NetworkPingManager.Instance.OnPingUpdated += OnPingUpdated;
            }

            // 开始定期更新在线人数
            StartCoroutine(OnlineCountUpdateLoop());
        }

        private void OnDestroy()
        {
            // 取消订阅延迟更新事件
            if (NetworkPingManager.Instance != null)
            {
                NetworkPingManager.Instance.OnPingUpdated -= OnPingUpdated;
            }
        }

        /// <summary>
        /// 加载用户信息
        /// </summary>
        private void LoadUserInfo()
        {
            var userData = UserData.Instance;
            if (userData == null || !userData.IsLoggedIn)
            {
                Debug.LogWarning("[UserInfoUI] 用户未登录");
                return;
            }

            // 显示用户名 + name_color
            if (UsernameText != null)
            {
                UsernameText.text = userData.Username;
                NameColorHelper.ApplyNameColor(UsernameText, userData.NameColor);
            }

            // 加载头像
            if (AvatarImage != null)
            {
                if (!string.IsNullOrEmpty(userData.Avatar))
                {
                    StartCoroutine(LoadAvatar(userData.Avatar));
                }
                else
                {
                    AvatarImage.texture = DefaultAvatar;
                }
            }

            // 头像框：有 URL 则加载并显示，否则禁用
            if (AvatarFrameImage != null)
            {
                if (!string.IsNullOrEmpty(userData.AvatarFrame))
                {
                    AvatarFrameImage.gameObject.SetActive(true);
                    StartCoroutine(LoadAvatarFrame(userData.AvatarFrame));
                }
                else
                {
                    AvatarFrameImage.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 加载头像协程
        /// </summary>
        private IEnumerator LoadAvatar(string url)
        {
            yield return ResourceLoader.LoadTexture(url,
                (texture) =>
                {
                    if (AvatarImage != null)
                    {
                        AvatarImage.texture = texture;
                    }
                },
                (error) =>
                {
                    Debug.LogWarning($"[UserInfoUI] 加载头像失败: {error}");
                    if (AvatarImage != null)
                    {
                        AvatarImage.texture = DefaultAvatar;
                    }
                });
        }

        /// <summary>
        /// 加载头像框协程
        /// </summary>
        private IEnumerator LoadAvatarFrame(string url)
        {
            yield return ResourceLoader.LoadTexture(url,
                (texture) =>
                {
                    if (AvatarFrameImage != null)
                    {
                        AvatarFrameImage.texture = texture;
                    }
                },
                (error) =>
                {
                    Debug.LogWarning($"[UserInfoUI] 加载头像框失败: {error}");
                    if (AvatarFrameImage != null)
                    {
                        AvatarFrameImage.gameObject.SetActive(false);
                    }
                });
        }

        /// <summary>
        /// 延迟更新回调
        /// </summary>
        private void OnPingUpdated(int ping)
        {
            UpdatePingDisplay(ping);
        }

        /// <summary>
        /// 在线人数更新循环协程
        /// </summary>
        private IEnumerator OnlineCountUpdateLoop()
        {
            while (true)
            {
                RequestOnlineCount();
                yield return new WaitForSeconds(OnlineCountUpdateInterval);
            }
        }

        /// <summary>
        /// 更新延迟显示
        /// </summary>
        private void UpdatePingDisplay(int ping)
        {
            if (PingText != null)
            {
                PingText.text = $"{ping}ms";
            }

            if (PingIcon != null)
            {
                PingIcon.color = GetPingColor(ping);
            }
        }

        /// <summary>
        /// 根据延迟获取对应颜色
        /// </summary>
        private Color GetPingColor(int ping)
        {
            if (ping <= ExcellentPing)
                return ExcellentColor;
            else if (ping <= GoodPing)
                return GoodColor;
            else if (ping <= FairPing)
                return FairColor;
            else
                return PoorColor;
        }

        /// <summary>
        /// 请求在线人数
        /// </summary>
        private void RequestOnlineCount()
        {
            WebSocketManager.Instance.Send("auth/online", new { }, (res) =>
            {
                if (res.success && res.data != null)
                {
                    int count = res.data["count"]?.ToObject<int>() ?? 0;
                    UpdateOnlineCount(count);
                }
            });
        }

        /// <summary>
        /// 更新在线人数显示
        /// </summary>
        private void UpdateOnlineCount(int count)
        {
            if (OnlineCountText != null)
            {
                OnlineCountText.text = $"{count}在线";
            }
        }
    }
}
