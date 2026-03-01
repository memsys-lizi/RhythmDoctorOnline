using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 房间编辑面板控制器 - 控制编辑面板的打开和关闭
    /// </summary>
    public class RoomEditPanelController : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("编辑面板容器（用于缩放动画）")]
        public Transform PanelContainer;
        [Tooltip("打开/关闭按钮")]
        public Button ToggleButton;

        [Header("动画设置")]
        [Tooltip("打开动画时长")]
        public float OpenDuration = 0.3f;
        [Tooltip("关闭动画时长")]
        public float CloseDuration = 0.2f;

        private bool _isOpen = false;

        private void Start()
        {
            // 初始化面板为关闭状态
            if (PanelContainer != null)
            {
                PanelContainer.localScale = Vector3.zero;
            }

            // 绑定按钮事件
            if (ToggleButton != null)
            {
                ToggleButton.onClick.AddListener(TogglePanel);
            }
        }

        /// <summary>
        /// 切换面板打开/关闭状态
        /// </summary>
        public void TogglePanel()
        {
            if (PanelContainer == null)
            {
                Debug.LogError("[RoomEditPanelController] PanelContainer 未设置");
                return;
            }

            if (_isOpen)
            {
                // 关闭面板
                PanelContainer.DOScale(Vector3.zero, CloseDuration).SetEase(Ease.InBack);
                _isOpen = false;
                Debug.Log("[RoomEditPanelController] 关闭编辑面板");
            }
            else
            {
                // 打开面板
                PanelContainer.DOScale(Vector3.one, OpenDuration).SetEase(Ease.OutBack);
                _isOpen = true;
                Debug.Log("[RoomEditPanelController] 打开编辑面板");
            }
        }
    }
}
