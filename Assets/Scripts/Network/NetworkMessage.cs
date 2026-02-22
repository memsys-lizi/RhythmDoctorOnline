using Newtonsoft.Json.Linq;

namespace RDOnline.Network
{
    /// <summary>
    /// 发送的消息结构
    /// </summary>
    public class RequestMessage
    {
        public string type{ get; set; }

        public object data{ get; set; }

        public string requestId { get; set; } = "";
    }

    /// <summary>
    /// 接收的消息结构
    /// </summary>
    public class ResponseMessage
    {
        public string type{ get; set; }

        public bool success{ get; set; }

        public string message{ get; set; }

        public JObject data{ get; set; }

        public string requestId{ get; set; }
    }
}
