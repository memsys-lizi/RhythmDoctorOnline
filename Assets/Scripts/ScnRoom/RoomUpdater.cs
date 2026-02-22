using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RDOnline;
using RDOnline.Component;
using RDOnline.Network;
using RDOnline.ScnLobby;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 房间更新器 - 处理更新房间信息的UI和逻辑（仅房主可用）
    /// </summary>
    public class RoomUpdater : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("房间名称输入框")]
        public TMP_InputField RoomNameInput;
        [Tooltip("房间人数显示文本")]
        public TMP_Text PlayerCountText;
        [Tooltip("房间人数调整滑块")]
        public Slider PlayerCountSlider;
        [Tooltip("房间密码输入框")]
        public TMP_InputField PasswordInput;
        [Tooltip("更新房间按钮")]
        public Button UpdateButton;

        [Header("引用")]
        [Tooltip("谱面预览（显示选中的社区关卡信息）")]
        public ChartPreview ChartPreview;

        private bool _isUpdating = false;

        private void Start()
        {
            // 初始化 Slider（与服务端最大人数 120 一致）
            if (PlayerCountSlider != null)
            {
                PlayerCountSlider.minValue = 1;
                PlayerCountSlider.maxValue = 120;
                PlayerCountSlider.wholeNumbers = true;
                PlayerCountSlider.onValueChanged.AddListener(OnPlayerCountChanged);
            }

            // 绑定更新按钮
            if (UpdateButton != null)
                UpdateButton.onClick.AddListener(OnUpdateButtonClick);

            // 加载当前房间信息到输入框
            LoadCurrentRoomInfo();
        }

        /// <summary>
        /// 加载当前房间信息到输入框
        /// </summary>
        private void LoadCurrentRoomInfo()
        {
            if (RoomData.Instance == null)
            {
                Debug.LogError("[RoomUpdater] RoomData 实例不存在");
                return;
            }

            // 加载房间名称
            if (RoomNameInput != null)
            {
                RoomNameInput.text = RoomData.Instance.RoomName;
            }

            // 加载最大人数（滑块上限至少为服务器下发的 MaxPlayers，避免后台改 80 后被 clamp 成 8 误发）
            if (PlayerCountSlider != null)
            {
                PlayerCountSlider.maxValue = Mathf.Max(120, RoomData.Instance.MaxPlayers);
                PlayerCountSlider.value = RoomData.Instance.MaxPlayers;
            }

            // 加载密码（如果有）
            if (PasswordInput != null)
            {
                PasswordInput.text = RoomData.Instance.Password ?? "";
            }

            // 更新人数显示
            UpdatePlayerCountText();

            Debug.Log("[RoomUpdater] 已加载当前房间信息到输入框");
        }

        /// <summary>
        /// 人数滑块变化事件
        /// </summary>
        private void OnPlayerCountChanged(float value)
        {
            UpdatePlayerCountText();
        }

        /// <summary>
        /// 更新人数显示文本
        /// </summary>
        private void UpdatePlayerCountText()
        {
            if (PlayerCountText != null && PlayerCountSlider != null)
            {
                int selectedPlayers = (int)PlayerCountSlider.value;
                int maxVal = (int)PlayerCountSlider.maxValue;
                PlayerCountText.text = $"{selectedPlayers}/{maxVal}";
            }
        }

        /// <summary>
        /// 更新房间按钮点击事件
        /// </summary>
        private void OnUpdateButtonClick()
        {
            if (_isUpdating)
            {
                ScrAlert.Show("正在更新中，请稍候", true);
                return;
            }

            // 验证输入
            if (!ValidateInput())
                return;

            // 获取数据
            string roomName = RoomNameInput.text.Trim();
            int maxPlayers = (int)PlayerCountSlider.value;
            string password = PasswordInput.text.Trim();

            // 检查是否有谱面更换（用户新选了社区关卡时）
            string chartUrl = null;
            string chartName = null;
            if (ChartPreview != null && !string.IsNullOrEmpty(ChartPreview.UploadedChartUrl) &&
                SelectedLevel.Current != null && !string.IsNullOrEmpty(SelectedLevel.ChartName))
            {
                chartUrl = ChartPreview.UploadedChartUrl;
                chartName = SelectedLevel.ChartName;
            }

            // 发送更新房间请求
            UpdateRoom(roomName, maxPlayers, password, chartUrl, chartName);
        }

        /// <summary>
        /// 验证输入
        /// </summary>
        private bool ValidateInput()
        {
            // 检查房间名称
            if (RoomNameInput == null || string.IsNullOrWhiteSpace(RoomNameInput.text))
            {
                ScrAlert.Show("请输入房间名称", true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 更新房间
        /// </summary>
        private void UpdateRoom(string roomName, int maxPlayers, string password, string chartUrl, string chartName)
        {
            _isUpdating = true;

            // 构建请求数据（只包含修改的字段）
            var data = new System.Collections.Generic.Dictionary<string, object>();

            // 房间名称
            if (roomName != RoomData.Instance.RoomName)
            {
                data["name"] = roomName;
            }

            // 最大人数
            if (maxPlayers != RoomData.Instance.MaxPlayers)
            {
                data["maxPlayers"] = maxPlayers;
            }

            // 密码
            string currentPassword = RoomData.Instance.Password ?? "";
            if (password != currentPassword)
            {
                data["password"] = password;
            }

            // 谱面URL和名称（如果有更换谱面）
            if (!string.IsNullOrEmpty(chartUrl))
            {
                data["chartUrl"] = chartUrl;
                data["chartName"] = chartName;
            }

            // 检查是否有修改
            if (data.Count == 0)
            {
                _isUpdating = false;
                ScrAlert.Show("没有修改任何信息", true);
                return;
            }

            Debug.Log($"[RoomUpdater] 开始更新房间，修改了 {data.Count} 个字段");

            // 发送请求
            WebSocketManager.Instance.Send("room/update", data, (res) =>
            {
                _isUpdating = false;

                if (res.success)
                {
                    Debug.Log("[RoomUpdater] 房间更新成功");
                    ScrAlert.Show("房间信息已更新", true);
                }
                else
                {
                    Debug.LogError($"[RoomUpdater] 房间更新失败: {res.message}");
                    ScrAlert.Show($"更新失败: {res.message}", true);
                }
            });
        }
    }
}
