using UnityEngine;
using RDOnline.Network;

namespace RDOnline.Auth
{
    /// <summary>
    /// 场景导航器 - 供按钮等调用 DoNavigate() 切换场景（如返回菜单）。
    /// </summary>
    public class SceneNavigator : MonoBehaviour
    {
        [Header("场景设置")]
        [Tooltip("目标场景名称")]
        public string TargetSceneName = "scnMenu";
        [Tooltip("切换场景前是否断开服务器连接")]
        public bool DisconnectBeforeNavigate = true;

        /// <summary>
        /// 执行导航：断开连接（可选）并加载目标场景。可由按钮 OnClick 调用。
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

