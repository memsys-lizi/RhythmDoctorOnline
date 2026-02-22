using System;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;

namespace RDOnline.Network
{
    public class WebSocketManager : MonoBehaviour
    {
        public static WebSocketManager Instance { get; private set; }

        private WebSocket _ws;
        private readonly Dictionary<string, Action<ResponseMessage>> _handlers = new();
        private readonly Dictionary<string, Action<ResponseMessage>> _pendingRequests = new();
        private bool _isManualDisconnect = false; // 标记是否为手动断开

        /// <summary>
        /// 连接状态
        /// </summary>
        public WebSocketState State => _ws?.State ?? WebSocketState.Closed;
        public bool IsConnected => State == WebSocketState.Open;

        /// <summary>
        /// 生命周期事件
        /// </summary>
        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnError;

        #region Unity生命周期

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _ws?.DispatchMessageQueue();
#endif
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        #endregion

        #region 连接管理

        /// <summary>
        /// 连接服务器
        /// </summary>
        public async void Connect(string url = null)
        {
            if (_ws != null && State == WebSocketState.Open)
            {
                Debug.LogWarning("[WS] 已经连接");
                return;
            }

            url ??= $"ws://{GameConfig.Instance.ServerUrl}";
            _ws = new WebSocket(url);

            _ws.OnOpen += () =>
            {
                Debug.Log("[WS] 连接成功");
                OnConnected?.Invoke();
            };

            _ws.OnClose += (code) =>
            {
                Debug.Log($"[WS] 连接关闭: {code}, 手动断开: {_isManualDisconnect}");

                // 只有在非手动断开时才触发 OnDisconnected 事件
                if (!_isManualDisconnect)
                {
                    OnDisconnected?.Invoke(code.ToString());
                }

                // 重置标志
                _isManualDisconnect = false;
            };

            _ws.OnError += (error) =>
            {
                Debug.LogError($"[WS] 错误: {error}");
                OnError?.Invoke(error);
            };

            _ws.OnMessage += OnMessageReceived;

            Debug.Log($"[WS] 正在连接: {url}");
            await _ws.Connect();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public async void Disconnect()
        {
            if (_ws == null) return;

            if (State == WebSocketState.Open)
            {
                _isManualDisconnect = true; // 标记为手动断开
                await _ws.Close();
            }
            _ws = null;
            _pendingRequests.Clear();
        }

        #endregion

        #region 消息收发

        /// <summary>
        /// 发送消息（无回调）
        /// </summary>
        public void Send(string type, object data = null)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[WS] 未连接，无法发送");
                return;
            }

            var msg = new RequestMessage { type = type, data = data };
            var json = JsonConvert.SerializeObject(msg);
            _ws.SendText(json);
        }

        /// <summary>
        /// 发送消息（带回调）
        /// </summary>
        public void Send(string type, object data, Action<ResponseMessage> callback)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[WS] 未连接，无法发送");
                callback?.Invoke(new ResponseMessage { success = false, message = "未连接" });
                return;
            }

            var requestId = Guid.NewGuid().ToString("N")[..8];
            _pendingRequests[requestId] = callback;

            var msg = new RequestMessage { type = type, data = data, requestId = requestId };
            var json = JsonConvert.SerializeObject(msg);
            _ws.SendText(json);
        }

        private void OnMessageReceived(byte[] bytes)
        {
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            ResponseMessage msg;

            try
            {
                msg = JsonConvert.DeserializeObject<ResponseMessage>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WS] 解析消息失败: {e.Message}\n{json}");
                return;
            }

            // 优先处理带requestId的响应
            if (!string.IsNullOrEmpty(msg.requestId) && _pendingRequests.TryGetValue(msg.requestId, out var callback))
            {
                _pendingRequests.Remove(msg.requestId);
                callback?.Invoke(msg);
                return;
            }

            // 触发注册的处理器
            if (_handlers.TryGetValue(msg.type, out var handler))
            {
                handler?.Invoke(msg);
            }
        }

        #endregion

        #region 事件订阅

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        public void Register(string type, Action<ResponseMessage> handler)
        {
            if (_handlers.ContainsKey(type))
            {
                _handlers[type] += handler;
            }
            else
            {
                _handlers[type] = handler;
            }
        }

        /// <summary>
        /// 取消注册
        /// </summary>
        public void Unregister(string type, Action<ResponseMessage> handler)
        {
            if (_handlers.ContainsKey(type))
            {
                _handlers[type] -= handler;
            }
        }

        /// <summary>
        /// 清除某类型的所有处理器
        /// </summary>
        public void UnregisterAll(string type)
        {
            _handlers.Remove(type);
        }

        #endregion
    }
}
