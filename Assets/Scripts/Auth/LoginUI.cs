using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using RDOnline.Network;
using Newtonsoft.Json.Linq;

namespace RDOnline.Auth
{
    /// <summary>
    /// 登录界面
    /// </summary>
    public class LoginUI : MonoBehaviour
    {
        [Header("面板")]
        public RectTransform Panel;
        public CanvasGroup CanvasGroup;

        [Header("输入框")]
        public TMP_InputField InputUsername;
        public TMP_InputField InputPassword;

        [Header("按钮")]
        public Button BtnLogin;
        public Button BtnToRegister;

        [Header("记住我")]
        public Toggle ToggleRemember;

        [Header("提示")]
        public TMP_Text TxtMessage;

        [Header("登录成功后跳转的场景")]
        public string NextScene = "ScnLobby";

        [Header("注册面板引用")]
        public RegisterUI RegisterPanel;

        [Header("动画设置")]
        public float AnimDuration = 0.3f;
        public float MoveOffset = 50f;

        private Vector2 _originPos;

        private const string KEY_REMEMBER = "login_remember";
        private const string KEY_USERNAME = "login_username";
        private const string KEY_PASSWORD = "login_password";

        private void Awake()
        {
            _originPos = Panel.anchoredPosition;
        }

        private void Start()
        {
            BtnLogin.onClick.AddListener(OnLoginClick);
            BtnToRegister.onClick.AddListener(OnToRegisterClick);

            // 加载记住的账号
            LoadSavedCredentials();

            // 监听连接状态
            WebSocketManager.Instance.OnConnected += OnConnected;
            WebSocketManager.Instance.OnError += OnError;

            // 自动连接
            if (!WebSocketManager.Instance.IsConnected)
            {
                ShowMessage("正在连接服务器...");
                WebSocketManager.Instance.Connect();
            }
            Debug.Log("HotPut!!!yesyesyesyes");
        }

        private void OnDestroy()
        {
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.OnConnected -= OnConnected;
                WebSocketManager.Instance.OnError -= OnError;
            }
        }

        private void LoadSavedCredentials()
        {
            bool remember = PlayerPrefs.GetInt(KEY_REMEMBER, 0) == 1;
            ToggleRemember.isOn = remember;

            if (remember)
            {
                InputUsername.text = PlayerPrefs.GetString(KEY_USERNAME, "");
                InputPassword.text = PlayerPrefs.GetString(KEY_PASSWORD, "");
            }
        }

        private void SaveCredentials(string username, string password)
        {
            if (ToggleRemember.isOn)
            {
                PlayerPrefs.SetInt(KEY_REMEMBER, 1);
                PlayerPrefs.SetString(KEY_USERNAME, username);
                PlayerPrefs.SetString(KEY_PASSWORD, password);
            }
            else
            {
                PlayerPrefs.SetInt(KEY_REMEMBER, 0);
                PlayerPrefs.DeleteKey(KEY_USERNAME);
                PlayerPrefs.DeleteKey(KEY_PASSWORD);
            }
            PlayerPrefs.Save();
        }

        private void OnConnected()
        {
            ShowMessage("连接成功");
        }

        private void OnError(string error)
        {
            ShowMessage($"连接失败: {error}");
        }

        private void OnLoginClick()
        {
            var username = InputUsername.text.Trim();
            var password = InputPassword.text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowMessage("请输入用户名和密码");
                return;
            }

            if (!WebSocketManager.Instance.IsConnected)
            {
                ShowMessage("未连接服务器");
                return;
            }

            BtnLogin.interactable = false;
            ShowMessage("登录中...");

            WebSocketManager.Instance.Send("auth/login", new { account = username, password }, (res) =>
            {
                BtnLogin.interactable = true;

                if (res.success)
                {
                    SaveCredentials(username, password);

                    var token = res.data["token"]?.ToString();
                    var user = res.data["user"] as JObject;

                    UserData.Instance.SetLoginData(
                        user["id"]?.ToObject<int>() ?? 0,
                        user["username"]?.ToString(),
                        user["nameColor"]?.ToString(),
                        user["email"]?.ToString(),
                        user["avatar"]?.ToString(),
                        user["avatarFrame"]?.ToString(),
                        user["qq"]?.ToString(),
                        user["role"]?.ToObject<int>() ?? 1,
                        token
                    );

                    ShowMessage("登录成功");
                    ScnLoading.LoadScenes(NextScene);
                }
                else
                {
                    ShowMessage(res.message ?? "登录失败");
                }
            });
        }

        private void OnToRegisterClick()
        {
            Hide();
            RegisterPanel.Show();
        }

        public void Show(TweenCallback onComplete = null)
        {
            Panel.anchoredPosition = _originPos + Vector2.left * MoveOffset;
            CanvasGroup.alpha = 0;
            CanvasGroup.blocksRaycasts = true;

            DOTween.Sequence()
                .Join(Panel.DOAnchorPos(_originPos, AnimDuration).SetEase(Ease.OutCubic))
                .Join(CanvasGroup.DOFade(1, AnimDuration))
                .OnComplete(onComplete);
        }

        public void Hide(TweenCallback onComplete = null)
        {
            CanvasGroup.blocksRaycasts = false;

            DOTween.Sequence()
                .Join(Panel.DOAnchorPos(_originPos + Vector2.right * MoveOffset, AnimDuration).SetEase(Ease.InCubic))
                .Join(CanvasGroup.DOFade(0, AnimDuration))
                .OnComplete(onComplete);
        }

        private void ShowMessage(string msg)
        {
            if (TxtMessage != null)
            {
                TxtMessage.text = msg;
            }
            Debug.Log($"[Login] {msg}");
        }
    }
}
