using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RDOnline.ScnLobby
{
    /// <summary>
    /// 大厅面板管理器 - 管理主面板和创建房间面板的Tab切换
    /// </summary>
    public class LobbyPanelManager : MonoBehaviour
    {
        [Header("面板引用")]
        [Tooltip("主面板（房间列表）")]
        public CanvasGroup MainPanel;
        [Tooltip("创建房间面板")]
        public CanvasGroup CreateRoomPanel;

        [Header("Tab按钮引用")]
        [Tooltip("大厅Tab按钮")]
        public Button LobbyTabButton;
        [Tooltip("创建房间Tab按钮")]
        public Button CreateRoomTabButton;

        [Header("动画设置")]
        [Tooltip("面板切换动画时长")]
        public float FadeDuration = 0.3f;

        private void Start()
        {
            InitializePanels();
            BindTabButtons();
        }

        /// <summary>
        /// 绑定Tab按钮事件
        /// </summary>
        private void BindTabButtons()
        {
            // 大厅Tab按钮
            if (LobbyTabButton != null)
                LobbyTabButton.onClick.AddListener(ShowMainPanel);

            // 创建房间Tab按钮
            if (CreateRoomTabButton != null)
                CreateRoomTabButton.onClick.AddListener(ShowCreateRoomPanel);
        }

        /// <summary>
        /// 初始化面板状态（仅用 CanvasGroup 控制显示隐藏）
        /// </summary>
        private void InitializePanels()
        {
            if (MainPanel != null)
            {
                MainPanel.alpha = 1f;
                MainPanel.interactable = true;
                MainPanel.blocksRaycasts = true;
            }

            if (CreateRoomPanel != null)
            {
                CreateRoomPanel.alpha = 0f;
                CreateRoomPanel.interactable = false;
                CreateRoomPanel.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// 显示创建房间面板（淡入淡出切换，仅 CanvasGroup 隐藏）
        /// </summary>
        public void ShowCreateRoomPanel()
        {
            if (CreateRoomPanel == null || MainPanel == null) return;

            MainPanel.DOFade(0f, FadeDuration).OnComplete(() =>
            {
                MainPanel.interactable = false;
                MainPanel.blocksRaycasts = false;
            });

            CreateRoomPanel.alpha = 0f;
            CreateRoomPanel.interactable = true;
            CreateRoomPanel.blocksRaycasts = true;
            CreateRoomPanel.DOFade(1f, FadeDuration);
        }

        /// <summary>
        /// 显示主面板（淡入淡出切换，仅 CanvasGroup 隐藏）
        /// </summary>
        public void ShowMainPanel()
        {
            if (MainPanel == null || CreateRoomPanel == null) return;

            CreateRoomPanel.DOFade(0f, FadeDuration).OnComplete(() =>
            {
                CreateRoomPanel.interactable = false;
                CreateRoomPanel.blocksRaycasts = false;
            });

            MainPanel.alpha = 0f;
            MainPanel.interactable = true;
            MainPanel.blocksRaycasts = true;
            MainPanel.DOFade(1f, FadeDuration);
        }
    }
}
