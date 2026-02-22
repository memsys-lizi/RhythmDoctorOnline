using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using RDOnline.Network;

namespace RDOnline.Auth
{
    /// <summary>
    /// 注册界面
    /// </summary>
    public class RegisterUI : MonoBehaviour
    {
        [Header("面板")]
        public RectTransform Panel;
        public CanvasGroup CanvasGroup;

        [Header("输入框")]
        public TMP_InputField InputUsername;
        public TMP_InputField InputEmail;
        public TMP_InputField InputPassword;
        public TMP_InputField InputPassword2;
        public TMP_InputField InputCode;

        [Header("按钮")]
        public Button BtnSendCode;
        public Button BtnRegister;
        public Button BtnBackToLogin;

        [Header("提示")]
        public TMP_Text TxtMessage;

        [Header("验证码冷却时间(秒)")]
        public int CodeCooldown = 60;

        [Header("登录面板引用")]
        public LoginUI LoginPanel;

        [Header("动画设置")]
        public float AnimDuration = 0.3f;
        public float MoveOffset = 50f;

        private float _cooldownTimer;
        private TMP_Text _btnSendCodeText;
        private Vector2 _originPos;

        private void Awake()
        {
            _originPos = Panel.anchoredPosition;

            // 默认隐藏
            CanvasGroup.alpha = 0;
            CanvasGroup.blocksRaycasts = false;
        }

        private void Start()
        {
            BtnSendCode.onClick.AddListener(OnSendCodeClick);
            BtnRegister.onClick.AddListener(OnRegisterClick);
            BtnBackToLogin.onClick.AddListener(OnBackToLoginClick);
            _btnSendCodeText = BtnSendCode.GetComponentInChildren<TMP_Text>();
        }

        private void Update()
        {
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_btnSendCodeText != null)
                {
                    _btnSendCodeText.text = $"{Mathf.CeilToInt(_cooldownTimer)}s";
                }
                if (_cooldownTimer <= 0)
                {
                    BtnSendCode.interactable = true;
                    if (_btnSendCodeText != null)
                    {
                        _btnSendCodeText.text = "发送验证码";
                    }
                }
            }
        }

        private void OnSendCodeClick()
        {
            var email = InputEmail.text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                ShowMessage("请输入邮箱");
                return;
            }

            if (!WebSocketManager.Instance.IsConnected)
            {
                ShowMessage("未连接服务器");
                return;
            }

            BtnSendCode.interactable = false;
            ShowMessage("发送中...");

            WebSocketManager.Instance.Send("auth/sendCode", new { email, type = 1 }, (res) =>
            {
                if (res.success)
                {
                    ShowMessage("验证码已发送");
                    _cooldownTimer = CodeCooldown;
                }
                else
                {
                    ShowMessage(res.message ?? "发送失败");
                    BtnSendCode.interactable = true;
                }
            });
        }

        private void OnRegisterClick()
        {
            var username = InputUsername.text.Trim();
            var email = InputEmail.text.Trim();
            var password = InputPassword.text;
            var password2 = InputPassword2.text;
            var code = InputCode.text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                ShowMessage("请输入用户名");
                return;
            }
            if (string.IsNullOrEmpty(email))
            {
                ShowMessage("请输入邮箱");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                ShowMessage("请输入密码");
                return;
            }
            if (password != password2)
            {
                ShowMessage("两次密码不一致");
                return;
            }
            if (string.IsNullOrEmpty(code))
            {
                ShowMessage("请输入验证码");
                return;
            }

            if (!WebSocketManager.Instance.IsConnected)
            {
                ShowMessage("未连接服务器");
                return;
            }

            BtnRegister.interactable = false;
            ShowMessage("注册中...");

            WebSocketManager.Instance.Send("auth/register", new { username, email, password, code }, (res) =>
            {
                BtnRegister.interactable = true;

                if (res.success)
                {
                    ShowMessage("注册成功");
                    OnBackToLoginClick();
                }
                else
                {
                    ShowMessage(res.message ?? "注册失败");
                }
            });
        }

        private void OnBackToLoginClick()
        {
            Hide();
            LoginPanel.Show();
        }

        public void Show(TweenCallback onComplete = null)
        {
            Panel.anchoredPosition = _originPos + Vector2.right * MoveOffset;
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
                .Join(Panel.DOAnchorPos(_originPos + Vector2.left * MoveOffset, AnimDuration).SetEase(Ease.InCubic))
                .Join(CanvasGroup.DOFade(0, AnimDuration))
                .OnComplete(onComplete);
        }

        private void ShowMessage(string msg)
        {
            if (TxtMessage != null)
            {
                TxtMessage.text = msg;
            }
            Debug.Log($"[Register] {msg}");
        }
    }
}
