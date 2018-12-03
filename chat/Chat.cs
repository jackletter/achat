using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace chat
{
    public class Chat : WebSocketBehavior
    {
        /// <summary>接收客户端消息
        /// </summary>
        protected override void OnMessage(MessageEventArgs e)
        {
            OnLineData online = OnLineData.GetReleaseModelReadOnly();
            string str = e.Data;
            int index = str.IndexOf("#");
            string flag = str.Substring(0, index);
            Msg msg = new Msg();
            msg.HasDeal = false;
            msg.MsgType = (MsgType)Enum.Parse(typeof(MsgType), flag);
            msg.From = online.Link_Clients[ID];
            msg.CreateTime = DateTime.Now;
            if (msg.MsgType == MsgType.c2b_UserMsg)
            {
                //发送给个人消息
                string other = str.Substring(index + 1);
                index = other.IndexOf("#");
                int uid = int.Parse(other.Substring(0, index));
                other = str.Substring(index + 1);
                string content = other.Substring(index + 1);
                msg.Content = content;
                User user;
                if (online.Uid_Users.TryGetValue(uid, out user))
                {
                    //如果这个人在线
                    msg.User = user;
                }
                else
                {
                    //如果这个人不在线上
                    msg.User = new User()
                    {
                        ID0 = uid
                    };
                }
            }
            else if (msg.MsgType == MsgType.c2b_GroupMsg)
            {
                //发送给群组消息
                string other = str.Substring(index + 1);
                index = other.IndexOf("#");
                int gid = int.Parse(other.Substring(0, index));
                string content = other.Substring(index + 1);
                msg.Content = content;
                Group g;
                if (online.Gid_Groups.TryGetValue(gid, out g))
                {
                    //如果这个组在线上
                    msg.Group = g;
                }
                else
                {
                    //如果这个组不在线上
                    msg.Group = new Group()
                    {
                        ID0 = gid
                    };
                }
            }
            else if (msg.MsgType == MsgType.c2b_GlobalMsg)
            {
                //发送给全局消息
                msg.Content = str.Substring(index + 1);
            }
            MessageManager.ReceiveMsg(msg);
        }



        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {

        }

        /// <summary>用户客户端下线
        /// </summary>
        protected override void OnClose(CloseEventArgs e)
        {
            //移除连接对象
            WebSocket socket;
            MessageManager.WebSockets.TryRemove(ID, out socket);
            OnLineData.Edit(online =>
            {
                //是否移除用户
                bool isRemoveUser = false;
                Client client;
                if (online.Link_Clients.TryGetValue(ID, out client))
                {
                    //1.移除客户端
                    online.Link_Clients.Remove(ID);
                }
                User user;
                if (online.Link_Users.TryGetValue(ID, out user))
                {
                    //2.移除用户
                    online.Link_Users.Remove(ID);
                }

                if (user != null)
                {
                    if (online.Uid_Users.ContainsKey(user.ID0))
                    {
                        User user2 = online.Uid_Users[user.ID0];
                        if (user2.Clients.ContainsKey(ID))
                        {
                            //3.移除用户中的客户端
                            user2.Clients.Remove(ID);
                        }
                        if (user2.Clients.Count == 0)
                        {
                            //4.如果用户没有了客户端就移除这个用户
                            online.Uid_Users.Remove(user.ID0);
                            isRemoveUser = true;
                        }
                    }
                }
                if (isRemoveUser)
                {
                    List<int> _remove_group_keys = new List<int>();
                    foreach (var i in online.Gid_Groups)
                    {
                        if (i.Value.Users.ContainsKey(user.ID0))
                        {
                            //5.从组中移出用户
                            i.Value.Users.Remove(user.ID0);
                        }
                        if (i.Value.Users.Count == 0)
                        {
                            //6.如果组中已没有用户,就将这个组移除,记住ID后面删除
                            _remove_group_keys.Add(i.Key);
                        }
                    }
                    foreach (var i in _remove_group_keys)
                    {
                        online.Gid_Groups.Remove(i);
                    }
                }
                if (user != null && isRemoveUser)
                {
                    //广播下线消息(存在这个用户并且这个用户的客户端全部下线后)
                    Sessions.Broadcast(string.Format("{0}#{1}#{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), (int)MsgType.b2c_BroadUserOffLine, user.ID0));
                }
                return true;
            });
        }

        /// <summary>用户客户端上线
        /// </summary>
        protected override void OnOpen()
        {
            //进入锁之前将数据准备好
            string userlogin = Context.QueryString["user"];
            DBUtil.IDbAccess iDb = Util.iDb_read;
            //准备用户信息
            DataTable dt_user = iDb.GetDataTable(string.Format("select * from CHAT_SYSUSER where LOGINNAME='{0}'", userlogin));
            if (dt_user.Rows.Count == 0)
            {
                try
                {
                    Context.WebSocket.Send(string.Format("{0}#{1}#{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), (int)MsgType.b2c_Error, "登录错误,未找到用户:" + userlogin));
                    try
                    {
                        Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
                return;
            }
            //构造客户端对象
            Client client = new Client();
            client.ID = ID;
            client.StartTime = DateTime.Now;
            client.UserLoginName = userlogin;
            client.UserName = dt_user.Rows[0]["UNAME"].ToString();
            client.UID = int.Parse(dt_user.Rows[0]["ID0"].ToString());

            //添加连接对象
            MessageManager.WebSockets.TryAdd(ID, Context.WebSocket);

            //用户对象
            User user;
            //准备群组用户群组对象
            DataTable dt_user_group = iDb.GetDataTable(string.Format("select * from CHAT_USER_GROUP where USERLOGIN='{0}'", userlogin));

            OnLineData.Edit(online =>
            {
                online.Link_Clients.Add(ID, client);

                if (!online.Uid_Users.TryGetValue(client.UID, out user))
                {
                    //当前用户的第一个客户端进行登录 
                    user = new User();
                    user.Name = dt_user.Rows[0]["UNAME"].ToString();
                    user.ID0 = int.Parse(dt_user.Rows[0]["ID0"].ToString());
                    user.LoginName = dt_user.Rows[0]["LOGINNAME"].ToString();
                    user.Clients.Add(client.ID, client);
                    online.Uid_Users.Add(user.ID0, user);
                    online.Link_Users.Add(ID, user);

                    //构造群组对象
                    if (dt_user_group.Rows.Count > 0)
                    {
                        int count = dt_user_group.Rows.Count;
                        for (int i = 0; i < count; i++)
                        {
                            int gid = int.Parse(dt_user_group.Rows[i]["GROUPID"].ToString());
                            Group g;
                            if (!online.Gid_Groups.TryGetValue(gid, out g))
                            {
                                //初次建立群组对象
                                g = new Group();
                                g.Name = dt_user_group.Rows[i]["GNAME"].ToString();
                                g.ID0 = gid;
                                g.Users.Add(user.ID0, user);
                                online.Gid_Groups.Add(g.ID0, g);
                            }
                            else
                            {
                                //已经存在这个群组对象
                                if (!g.Users.ContainsKey(user.ID0))
                                {
                                    //不包含这个用户
                                    g.Users.Add(user.ID0, user);
                                }
                            }
                        }
                    }
                }
                else
                {
                    //当前用户的多个用户端登录
                    user.Clients.Add(client.ID, client);
                    online.Link_Users.Add(ID, user);
                }
                //广播用户上线通知
                Sessions.Broadcast(string.Format("{0}#{1}#{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), (int)MsgType.b2c_BroadUserOnLine, userlogin));
                return true;
            });
        }
    }
}
