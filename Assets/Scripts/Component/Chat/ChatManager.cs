using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RDOnline.Network;
using RDOnline.Audio;

namespace RDOnline.Component
{
    /// <summary>
    /// 聊天管理器 - 管理聊天消息的接收和显示
    /// </summary>
    public class ChatManager : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("消息列表容器（ScrollView的Content）")]
        public Transform MessageListContent;
        [Tooltip("消息项预制体")]
        public GameObject MessageItemPrefab;
        [Tooltip("输入框")]
        public TMP_InputField InputField;
        [Tooltip("发送按钮")]
        public Button SendButton;
        [Tooltip("滚动视图")]
        public ScrollRect ScrollRect;

        [Header("设置")]
        [Tooltip("最大消息数量")]
        public int MaxMessageCount = 100;

        [Header("音效")]
        [Tooltip("收到消息音效")]
        public AudioClip MessageReceivedSound;

        private List<GameObject> _messageItems = new List<GameObject>();

        private void Start()
        {
            // 绑定发送按钮事件
            if (SendButton != null)
                SendButton.onClick.AddListener(SendMessage);

            // 绑定输入框回车事件
            if (InputField != null)
                InputField.onSubmit.AddListener((text) => SendMessage());

            // 注册 WebSocket 事件监听
            RegisterWebSocketEvents();
        }

        private void OnDestroy()
        {
            // 取消注册事件
            UnregisterWebSocketEvents();
        }

        /// <summary>
        /// 注册 WebSocket 事件监听
        /// </summary>
        private void RegisterWebSocketEvents()
        {
            if (WebSocketManager.Instance == null)
            {
                Debug.LogWarning("[ChatManager] WebSocketManager 实例不存在，无法注册事件");
                return;
            }

            WebSocketManager.Instance.Register("chat/message", OnChatMessage);
            Debug.Log("[ChatManager] WebSocket 事件监听已注册");
        }

        /// <summary>
        /// 取消注册 WebSocket 事件监听
        /// </summary>
        private void UnregisterWebSocketEvents()
        {
            if (WebSocketManager.Instance == null) return;

            WebSocketManager.Instance.Unregister("chat/message", OnChatMessage);
            Debug.Log("[ChatManager] WebSocket 事件监听已取消注册");
        }

        /// <summary>
        /// 处理聊天消息事件
        /// </summary>
        private void OnChatMessage(ResponseMessage msg)
        {
            if (msg.data == null) return;

            try
            {
                // 解析消息数据
                string username = msg.data["username"]?.ToString();
                string message = msg.data["message"]?.ToString();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(message))
                {
                    Debug.LogWarning("[ChatManager] 消息数据不完整");
                    return;
                }

                Debug.Log($"[ChatManager] 收到消息 - {username}: {message}");

                // 添加消息到列表
                AddMessage(username, message);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChatManager] 处理聊天消息失败: {e.Message}");
            }
        }

        /// <summary>
        /// 添加消息到列表
        /// </summary>
        private void AddMessage(string username, string message)
        {
            if (MessageItemPrefab == null || MessageListContent == null)
            {
                Debug.LogError("[ChatManager] MessageItemPrefab 或 MessageListContent 未设置");
                return;
            }

            // 播放收到消息音效
            if (MessageReceivedSound != null)
            {
                RDOnline.Audio.AudioManager.PlaySound(MessageReceivedSound);
            }

            // 实例化消息项
            GameObject itemObj = Instantiate(MessageItemPrefab, MessageListContent);
            ChatMessageItem messageItem = itemObj.GetComponent<ChatMessageItem>();

            if (messageItem != null)
            {
                messageItem.SetMessage(username, message);
                _messageItems.Add(itemObj);

                // 限制消息数量
                if (_messageItems.Count > MaxMessageCount)
                {
                    GameObject oldestItem = _messageItems[0];
                    _messageItems.RemoveAt(0);
                    Destroy(oldestItem);
                }

                // 滚动到底部
                ScrollToBottom();
            }
            else
            {
                Debug.LogError("[ChatManager] MessageItemPrefab 上没有 ChatMessageItem 组件");
                Destroy(itemObj);
            }
        }

        /// <summary>
        /// 滚动到底部
        /// </summary>
        private void ScrollToBottom()
        {
            if (ScrollRect != null)
            {
                // 延迟一帧执行，确保布局更新完成
                StartCoroutine(ScrollToBottomCoroutine());
            }
        }

        /// <summary>
        /// 滚动到底部协程
        /// </summary>
        private System.Collections.IEnumerator ScrollToBottomCoroutine()
        {
            // 等待一帧让预制体实例化完毕
            yield return null;

            // 强制刷新布局
            if (MessageListContent != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(MessageListContent as RectTransform);
            }

            // 再等待一帧让布局更新完成
            yield return null;

            // 设置滚动位置到底部
            if (ScrollRect != null)
            {
                ScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public void SendMessage()
        {
            if (InputField == null)
            {
                Debug.LogError("[ChatManager] InputField 未设置");
                return;
            }

            string message = InputField.text.Trim();

            // 去除富文本标签
            message = RemoveRichTextTags(message);

            // 检查消息是否为空
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            // 检查消息长度
            if (message.Length > 50)
            {
                ScrAlert.Show("消息长度不能超过50字", true);
                return;
            }

            // 检查连接状态
            if (WebSocketManager.Instance == null || !WebSocketManager.Instance.IsConnected)
            {
                ScrAlert.Show("未连接服务器", true);
                return;
            }

            Debug.Log($"[ChatManager] 发送消息: {message}");

            // 构建请求数据
            var data = new
            {
                message = message
            };

            // 发送消息
            WebSocketManager.Instance.Send("chat/send", data, (res) =>
            {
                if (res.success)
                {
                    Debug.Log("[ChatManager] 消息发送成功");
                    // 清空输入框
                    InputField.text = "";
                }
                else
                {
                    Debug.LogError($"[ChatManager] 消息发送失败: {res.message}");
                    ScrAlert.Show($"发送失败: {res.message}", true);
                }
            });
        }

        /// <summary>
        /// 去除富文本标签
        /// </summary>
        private string RemoveRichTextTags(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // 使用正则表达式去除所有富文本标签
            return System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
        }
    }
}
