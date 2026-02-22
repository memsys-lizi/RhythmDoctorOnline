using UnityEngine;
using RDOnline.Network;
using RDOnline.Lobby;

namespace RDOnline.Auth
{
    /// <summary>
    /// 场景导航器 - 玩家进入本物体触发区（Collider2D isTrigger）时切换场景。
    /// </summary>
    public class SceneNavigator : MonoBehaviour
    {
        [Header("场景设置")]
        [Tooltip("目标场景名称")]
        public string TargetSceneName = "scnMenu";
        [Tooltip("切换场景前是否断开服务器连接")]
        public bool DisconnectBeforeNavigate = true;

        private void Start()
        {
            var c = GetComponent<Collider2D>();
            if (c != null && !c.isTrigger)
                Debug.LogWarning("[SceneNavigator] 请将本物体上的 Collider2D 勾选 Is Trigger。");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<LobbyPlayerController>() == null)
                return;
            DoNavigate();
        }

        /// <summary>
        /// 执行离开：断开连接（可选）并加载目标场景
        /// </summary>
        public void DoNavigate()
        {
            if (string.IsNullOrEmpty(TargetSceneName))
            {
                Debug.LogError("[SceneNavigator] 目标场景名称为空");
                return;
            }

            Debug.Log($"[SceneNavigator] 切换到场景: {TargetSceneName}");

            if (DisconnectBeforeNavigate && WebSocketManager.Instance != null && WebSocketManager.Instance.IsConnected)
            {
                Debug.Log("[SceneNavigator] 断开服务器连接");
                WebSocketManager.Instance.Disconnect();
            }

            ScnLoading.LoadScenes(TargetSceneName);
        }
    }
}

