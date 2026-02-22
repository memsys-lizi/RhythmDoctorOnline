using System;
using UnityEngine;
using UnityEngine.UI;
using RDOnline.Network;
using RDOnline.Utils;
using System.Diagnostics;
using System.IO;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 准备管理器 - 负责处理玩家准备、倒计时和游戏开始
    /// </summary>
    public class ReadyManager : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("准备按钮")]
        public Button ReadyButton;
        [Tooltip("等待其他玩家完成游戏的面板")]
        public GameObject WaitingPanel;

        private bool _isReady = false;
        private bool _isCountingDown = false;
        private Coroutine _countdownCoroutine = null;

        public static bool IsWantToPlay;

        private void Start()
        {
            IsWantToPlay = false;
            // 初始化等待面板为隐藏状态
            if (WaitingPanel != null)
            {
                WaitingPanel.SetActive(false);
            }

            // 绑定按钮事件
            if (ReadyButton != null)
            {
                ReadyButton.onClick.AddListener(OnReadyButtonClick);
            }

            // 注册 WebSocket 事件监听
            RegisterWebSocketEvents();

            // 更新按钮状态
            UpdateButtonState();

            // 检查是否从游戏返回
            if (RoomData.IsReturningFromGame)
            {
                SendFinishRequest();
            }
        }

        private void OnDestroy()
        {
            // 取消注册事件
            UnregisterWebSocketEvents();
        }

        /// <summary>
        /// 准备按钮点击事件
        /// </summary>
        private void OnReadyButtonClick()
        {
            // 切换准备状态
            _isReady = !_isReady;

            // 发送准备请求
            SendReadyRequest(_isReady);
        }

        /// <summary>
        /// 发送准备请求
        /// </summary>
        private void SendReadyRequest(bool ready)
        {
            if (WebSocketManager.Instance == null || !WebSocketManager.Instance.IsConnected)
            {
                ScrAlert.Show("未连接服务器", true);
                return;
            }

            Debug.Log($"[ReadyManager] 发送准备请求: {ready}");

            var data = new { ready = ready };

            WebSocketManager.Instance.Send("room/ready", data, (res) =>
            {
                if (res.success)
                {
                    Debug.Log($"[ReadyManager] 准备状态更新成功: {ready}");
                    UpdateButtonState();
                }
                else
                {
                    Debug.LogError($"[ReadyManager] 准备失败: {res.message}");
                    ScrAlert.Show($"准备失败: {res.message}", true);
                    // 恢复状态
                    _isReady = !_isReady;
                    UpdateButtonState();
                }
            });
        }

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        private void UpdateButtonState()
        {
            if (ReadyButton == null) return;

            // 更新按钮文本
            var buttonText = ReadyButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = _isReady ? "取消准备" : "准备";
            }
        }

        /// <summary>
        /// 注册 WebSocket 事件监听
        /// </summary>
        private void RegisterWebSocketEvents()
        {
            if (WebSocketManager.Instance == null)
            {
                Debug.LogWarning("[ReadyManager] WebSocketManager 实例不存在，无法注册事件");
                return;
            }

            WebSocketManager.Instance.Register("room/countdown", OnCountdown);
            WebSocketManager.Instance.Register("room/countdownCancel", OnCountdownCancel);
            WebSocketManager.Instance.Register("room/gameStart", OnGameStart);
            WebSocketManager.Instance.Register("room/playerFinish", OnPlayerFinish);
            WebSocketManager.Instance.Register("room/gameEnd", OnGameEnd);

            Debug.Log("[ReadyManager] WebSocket 事件监听已注册");
        }

        /// <summary>
        /// 取消注册 WebSocket 事件监听
        /// </summary>
        private void UnregisterWebSocketEvents()
        {
            if (WebSocketManager.Instance == null) return;

            WebSocketManager.Instance.Unregister("room/countdown", OnCountdown);
            WebSocketManager.Instance.Unregister("room/countdownCancel", OnCountdownCancel);
            WebSocketManager.Instance.Unregister("room/gameStart", OnGameStart);
            WebSocketManager.Instance.Unregister("room/playerFinish", OnPlayerFinish);
            WebSocketManager.Instance.Unregister("room/gameEnd", OnGameEnd);

            Debug.Log("[ReadyManager] WebSocket 事件监听已取消注册");
        }

        /// <summary>
        /// 处理倒计时事件
        /// </summary>
        private void OnCountdown(ResponseMessage msg)
        {
            if (msg.data == null) return;

            try
            {
                int seconds = (msg.data["seconds"] as Newtonsoft.Json.Linq.JToken)?.ToObject<int>() ?? 5;
                Debug.Log($"[ReadyManager] 收到倒计时事件: {seconds}秒");

                _isCountingDown = true;

                // 停止之前的倒计时协程（如果有）
                if (_countdownCoroutine != null)
                {
                    StopCoroutine(_countdownCoroutine);
                }

                // 启动倒计时协程
                _countdownCoroutine = StartCoroutine(CountdownCoroutine(seconds));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ReadyManager] 处理倒计时事件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 倒计时协程 - 每秒更新倒计时提示
        /// </summary>
        private System.Collections.IEnumerator CountdownCoroutine(int seconds)
        {
            for (int i = seconds; i > 0; i--)
            {
                // 显示倒计时提示，不自动隐藏
                ScrAlert.Show($"所有玩家已准备，{i}秒后开始游戏", false);
                yield return new WaitForSeconds(1f);
            }

            _countdownCoroutine = null;
        }

        /// <summary>
        /// 处理倒计时取消事件
        /// </summary>
        private void OnCountdownCancel(ResponseMessage msg)
        {
            Debug.Log("[ReadyManager] 倒计时已取消");

            _isCountingDown = false;

            // 停止倒计时协程
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            // 显示取消提示
            ScrAlert.Show("有玩家取消准备，倒计时已取消", true);
        }

        /// <summary>
        /// 处理游戏开始事件
        /// </summary>
        private void OnGameStart(ResponseMessage msg)
        {
            Debug.Log("[ReadyManager] 游戏开始");

            _isCountingDown = false;

            // 停止倒计时协程
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            // 显示游戏开始提示
            ScrAlert.Show("游戏开始！", true);

            RoomData.IsReturningFromGame = true;

            // 默认加载找到的第一个 .rdlevel 文件（仅用于校验与日志，实际进游戏逻辑保持原样）
            string chartDir = ChartDownloader.Instance != null
                ? ChartDownloader.Instance.GetChartDirectory()
                : Path.Combine(Application.persistentDataPath, "Chart");
            string levelPath = ChartDownloader.GetFirstRdlevelPath(chartDir);
            if (string.IsNullOrEmpty(levelPath))
            {
                UnityEngine.Debug.LogError("[ReadyManager] 未找到 .rdlevel 谱面，无法进入游戏");
                ScrAlert.Show("未找到谱面文件", true);
                return;
            }
            Debug.Log("Enter level：" + levelPath);

            GameUtils.LoadLevel(levelPath);
        }

        /// <summary>
        /// 发送游戏完成请求
        /// </summary>
        private void SendFinishRequest()
        {
            if (WebSocketManager.Instance == null || !WebSocketManager.Instance.IsConnected)
            {
                Debug.LogError("[ReadyManager] 未连接服务器，无法发送完成请求");
                return;
            }

            // 获取玩家分数
            string score = "";
            if (!string.IsNullOrEmpty(Patches.RankScore.score))
            {
                score = Patches.RankScore.ToString();
                Debug.Log($"[ReadyManager] 玩家分数: {score}");
            }
            else
            {
                Debug.LogWarning("[ReadyManager] CurrentScore 为空，使用空分数");
            }

            Debug.Log("[ReadyManager] 发送游戏完成请求");

            // 显示等待面板
            if (WaitingPanel != null)
            {
                WaitingPanel.SetActive(true);
            }

            WebSocketManager.Instance.Send("room/finish", new { score = score }, (res) =>
            {
                if (res.success)
                {
                    Debug.Log("[ReadyManager] 游戏完成请求发送成功");
                }
                else
                {
                    Debug.LogError($"[ReadyManager] 游戏完成请求失败: {res.message}");
                    ScrAlert.Show($"发送完成失败: {res.message}", true);
                }
            });
        }

        /// <summary>
        /// 游戏结束后的清理逻辑
        /// </summary>
        private void OnGameEndCleanup()
        {
            Debug.Log("[ReadyManager] 执行游戏结束清理");

            // 隐藏等待面板
            if (WaitingPanel != null)
            {
                WaitingPanel.SetActive(false);
            }

            // 重置标志位
            RoomData.IsReturningFromGame = false;

            // 重置准备状态
            _isReady = false;
            UpdateButtonState();

            // 显示提示
            ScrAlert.Show("所有玩家已完成游戏", true);
        }

        /// <summary>
        /// 处理玩家完成游戏事件
        /// </summary>
        private void OnPlayerFinish(ResponseMessage msg)
        {
            if (msg.data == null) return;

            try
            {
                int userId = (msg.data["userId"] as Newtonsoft.Json.Linq.JToken)?.ToObject<int>() ?? 0;
                Debug.Log($"[ReadyManager] 玩家 {userId} 已完成游戏");

                // 可以在这里显示其他玩家完成的提示（可选）
                // ScrAlert.Show($"玩家 {userId} 已完成游戏", true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ReadyManager] 处理玩家完成事件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理游戏结束事件
        /// </summary>
        private void OnGameEnd(ResponseMessage msg)
        {
            Debug.Log("[ReadyManager] 收到游戏结束事件");

            // 解析分数数据
            if (msg.data != null && msg.data.ContainsKey("scores"))
            {
                try
                {
                    var scoresArray = msg.data["scores"] as Newtonsoft.Json.Linq.JArray;
                    if (scoresArray != null)
                    {
                        var scoreList = new System.Collections.Generic.List<ScoreData>();

                        foreach (var scoreItem in scoresArray)
                        {
                            int userId = scoreItem["userId"]?.ToObject<int>() ?? 0;
                            string username = scoreItem["username"]?.ToString();
                            string nameColor = scoreItem["nameColor"]?.ToString();
                            string score = scoreItem["score"]?.ToString();

                            // 解析精准度用于排序
                            float accuracy = ScorePanelController.ParseAccuracy(score);

                            scoreList.Add(new ScoreData
                            {
                                UserId = userId,
                                Username = username,
                                NameColor = nameColor,
                                Score = score,
                                Accuracy = accuracy
                            });

                            Debug.Log($"[ReadyManager] 玩家分数 - {username}: {score} (精准度: {accuracy})");
                        }

                        // 显示分数面板
                        if (ScorePanelController.Instance != null)
                        {
                            ScorePanelController.Instance.ShowPanel(scoreList);
                        }
                        else
                        {
                            Debug.LogError("[ReadyManager] ScorePanelController 实例不存在");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[ReadyManager] 解析分数数据失败: {e.Message}");
                }
            }

            // 执行清理逻辑（隐藏等待面板、重置状态）
            OnGameEndCleanup();
        }
    }
}
