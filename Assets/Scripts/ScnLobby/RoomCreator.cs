using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RDOnline;
using RDOnline.Component;
using RDOnline.Network;

namespace RDOnline.ScnLobby
{
    /// <summary>
    /// 房间创建器 - 处理创建房间的UI和逻辑
    /// </summary>
    public class RoomCreator : MonoBehaviour
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
        [Tooltip("创建房间按钮")]
        public Button CreateButton;

        [Header("引用")]
        [Tooltip("谱面预览（显示选中的社区关卡信息）")]
        public ChartPreview ChartPreview;

        private void Start()
        {
            // 初始化 Slider（与服务端最大人数 120 一致）
            if (PlayerCountSlider != null)
            {
                PlayerCountSlider.minValue = 1;
                PlayerCountSlider.maxValue = 120;
                PlayerCountSlider.wholeNumbers = true;
                PlayerCountSlider.value = 8;
                PlayerCountSlider.onValueChanged.AddListener(OnPlayerCountChanged);
            }

            // 初始化人数显示
            UpdatePlayerCountText();

            // 绑定创建按钮
            if (CreateButton != null)
                CreateButton.onClick.AddListener(OnCreateButtonClick);
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
        /// 创建房间按钮点击事件
        /// </summary>
        private void OnCreateButtonClick()
        {
            // 验证输入
            if (!ValidateInput())
                return;

            // 立即禁用按钮，防止重复点击
            if (CreateButton != null)
                CreateButton.interactable = false;

            string roomName = RoomNameInput.text.Trim();
            int maxPlayers = (int)PlayerCountSlider.value;
            string password = PasswordInput.text.Trim();
            string chartUrl = ChartPreview.UploadedChartUrl;
            string chartName = SelectedLevel.Current != null ? SelectedLevel.ChartName : (ChartPreview.CurrentChartName ?? "");

            if (string.IsNullOrEmpty(chartName))
            {
                ScrAlert.Show("无法获取谱面名称", true);
                if (CreateButton != null)
                    CreateButton.interactable = true;
                return;
            }

            CreateRoom(roomName, maxPlayers, password, chartUrl, chartName);
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

            if (ChartPreview == null || string.IsNullOrEmpty(ChartPreview.UploadedChartUrl))
            {
                ScrAlert.Show("请先选择关卡或上传谱面", true);
                return false;
            }

            bool hasChartName = (SelectedLevel.Current != null && !string.IsNullOrEmpty(SelectedLevel.ChartName))
                || !string.IsNullOrEmpty(ChartPreview.CurrentChartName);
            if (!hasChartName)
            {
                ScrAlert.Show("请先在左侧选择关卡或通过文件浏览选择谱面并上传", true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        private void CreateRoom(string roomName, int maxPlayers, string password, string chartUrl, string chartName)
        {
            // 构建请求数据
            var data = new
            {
                name = roomName,
                maxPlayers = maxPlayers,
                password = string.IsNullOrEmpty(password) ? "" : password,
                chartUrl = chartUrl,
                chartName = chartName
            };

            // 发送请求
            WebSocketManager.Instance.Send("room/create", data, (res) =>
            {
                if (res.Success)
                {
                    // 获取房间ID
                    string roomId = res.Data["roomId"]?.ToString();
                    Debug.Log($"[RoomCreator] 房间创建成功，房间ID: {roomId}");

                    // 保存房间数据到 RoomData
                    if (RoomData.Instance != null)
                    {
                        bool hasPassword = !string.IsNullOrEmpty(password);
                        RoomData.Instance.SetBasicRoomInfo(roomId, roomName, maxPlayers, hasPassword, password, chartUrl, chartName);
                        Debug.Log($"[RoomCreator] 房间数据已保存到 RoomData");
                    }

                    ScrAlert.Show("房间创建成功！", true);

                    // 跳转到房间场景（成功时不需要重新启用按钮，因为要切换场景了）
                    ScnLoading.LoadScenes("ScnRoom");
                }
                else
                {
                    ScrAlert.Show($"创建失败: {res.Message}", true);

                    // 失败时重新启用按钮
                    if (CreateButton != null)
                        CreateButton.interactable = true;
                }
            });
        }
    }
}
