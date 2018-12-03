using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Net.WebSockets;

namespace chat
{
    //消息
    public class Msg
    {
        /// <summary>这条消息的上下文环境
        /// </summary>
        public WebSocketContext Context { set; get; }

        /// <summary>消息来自的客户端对象
        /// </summary>
        public Client From { set; get; }

        /// <summary>消息类型
        /// </summary>
        public MsgType MsgType { set; get; }

        /// <summary>目的用户对象(如果是发送给用户的消息)
        /// </summary>
        public User User { set; get; }

        /// <summary>目的群组对象(如果是发送给群组的消息)
        /// </summary>
        public Group Group { set; get; }

        /// <summary>消息内容
        /// </summary>
        public string Content { set; get; }

        /// <summary>消息是否被处理(仅在发送给用户消息时有用)
        /// </summary>
        public bool HasDeal { set; get; }

        /// <summary>服务端接收这条消息的时间
        /// </summary>
        public DateTime CreateTime { set; get; }

        /// <summary>服务端接收这条消息的时间
        /// </summary>
        public string CreateTime_str
        {
            get
            {
                return this.CreateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            }
        }
    }
}
