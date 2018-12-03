using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using System.Collections.Concurrent;

namespace chat
{
    //消息管理类
    public class MessageManager
    {
        //发送给个人消息的一个队列
        private static BlockQueue<Msg> _userQueue = new BlockQueue<Msg>(5000);

        //发送给群组的一个队列
        private static BlockQueue<Msg> _groupQueue = new BlockQueue<Msg>(5000);

        //发送给全局的一个队列
        private static BlockQueue<Msg> _globalQueue = new BlockQueue<Msg>(5000);

        //获取用户组织树的一个队列
        private static BlockQueue<Msg> _userTreeQueue = new BlockQueue<Msg>(5000);

        //获取起始数据的一个队列
        private static BlockQueue<Msg> _historyDataQueue = new BlockQueue<Msg>(5000);

        static MessageManager()
        {
            Task.Factory.StartNew(() =>
            {
                //处理个人消息的线程
                DealUserMsg();
            });
            Task.Factory.StartNew(() =>
            {
                //处理群组消息的线程
                DealGroupMsg();
            });
            Task.Factory.StartNew(() =>
            {
                //处理全局消息的线程
                DealGlobalMsg();
            });
            Task.Factory.StartNew(() =>
            {
                //处理历史消息的线程
                DealHistoryMsg();
            });
            Task.Factory.StartNew(() =>
            {
                //处理用户组织树的线程
                DealUserTreeMsg();
            });

        }

        /// <summary>接受客户端消息分类放进队列中
        /// </summary>
        /// <param name="msg"></param>
        public static void ReceiveMsg(Msg msg)
        {
            if (msg.MsgType == MsgType.c2b_UserMsg)
            {
                _userQueue.EnQueue(msg);
            }
            else if (msg.MsgType == MsgType.c2b_GroupMsg)
            {
                _groupQueue.EnQueue(msg);
            }
            else if (msg.MsgType == MsgType.c2b_GlobalMsg)
            {
                _globalQueue.EnQueue(msg);
            }
            else if (msg.MsgType == MsgType.c2b_UserTree)
            {
                _userTreeQueue.EnQueue(msg);
            }
            else if (msg.MsgType == MsgType.c2b_History)
            {
                _historyDataQueue.EnQueue(msg);
            }
        }

        /// <summary>处理发送的个人消息
        /// </summary>
        private static void DealUserMsg()
        {
            while (true)
            {
                Msg msg = _userQueue.DeQueue();
                if (msg.User.Clients.Count > 0)
                {
                    //如果用户存在连接的客户端就直接发送到客户端
                    foreach (var i in msg.User.Clients)
                    {
                        WebSocket destSocket;
                        MessageManager.WebSockets.TryGetValue(i.Key, out destSocket);
                        if (destSocket != null && destSocket.IsAlive)
                        {
                            try
                            {
                                destSocket.Send(string.Format("{0}#{1}#{2}#{3}", msg.CreateTime_str, (int)MsgType.b2c_UserMsg, msg.From.UID, msg.Content));
                                //有一个客户端接受即消息接收成功
                                msg.HasDeal = true;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.StackTrace);
                            }
                        }
                    }
                }
                else
                {
                    if (Util.iDb_read.GetFirstColumnString(string.Format("select count(1) from CHAT_SYSUSER where ID0='{0}'", msg.User.ID0)) == "0")
                    {
                        //如果不存在这个人
                        WebSocket socket;
                        if (MessageManager.WebSockets.TryGetValue(msg.From.ID, out socket))
                        {
                            if (socket != null && socket.IsAlive)
                            {
                                try
                                {
                                    socket.Send(string.Format("{0}#{1}#{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), (int)MsgType.b2c_Error, "未找到指定的人:" + msg.User.ID0));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                        }
                        return;
                    }
                }
                //将消息发送给生产方
                OnLineData onlinedata = OnLineData.GetReleaseModelReadOnly();
                User user;
                if (onlinedata.Uid_Users.TryGetValue(msg.From.UID, out user))
                {
                    foreach (var i in user.Clients)
                    {
                        WebSocket destSocket;
                        MessageManager.WebSockets.TryGetValue(i.Key, out destSocket);
                        if (destSocket != null && destSocket.IsAlive)
                        {
                            try
                            {
                                destSocket.Send(string.Format("{0}#{1}#{2}#{3}", msg.CreateTime_str, (int)MsgType.b2c_UserMsg, msg.From.UID+"-"+msg.User.ID0, msg.Content));
                                //有一个客户端接受即消息接收成功
                                msg.HasDeal = true;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.StackTrace);
                            }
                        }
                    }
                }
                //将这条消息写入到数据库
                WriteUserMsg2Db(msg);
            }
        }

        /// <summary>将发送的个人消息和处理情况写入数据库
        /// </summary>
        private static void WriteUserMsg2Db(Msg msg)
        {
            Hashtable ht = new Hashtable();
            DBUtil.IDbAccess iDb = Util.iDb_read.NewIDB();
            ht.Add("ID0", iDb.IDSNOManager.NewID(iDb, "CHAT_MSG_USER", "ID0"));
            ht.Add("MFROMLOGIN", msg.From.UserLoginName);
            ht.Add("MFROMNAME", msg.From.UserName);
            ht.Add("MFROMID", msg.From.UID);
            ht.Add("MTO", msg.User.ID0);
            ht.Add("MSG", msg.Content);
            ht.Add("HASDEAL", msg.HasDeal ? 1 : 0);
            ht.Add("MTIME", msg.CreateTime_str);
            iDb.AddData("CHAT_MSG_USER", ht);
        }

        /// <summary>处理群组消息
        /// </summary>
        private static void DealGroupMsg()
        {
            while (true)
            {
                Msg msg = _groupQueue.DeQueue();
                Group group = msg.Group;
                int dealCount = 0;
                string id0s = "";

                if (group.Users.Count > 0)
                {
                    //如果用户存在连接的客户端就直接发送到客户端
                    foreach (var i in group.Users)
                    {
                        bool successUser = false;
                        foreach (var j in i.Value.Clients)
                        {
                            WebSocket socket;
                            if (MessageManager.WebSockets.TryGetValue(j.Key, out socket))
                            {
                                try
                                {
                                    socket.Send(string.Format("{0}#{1}#{2}#{3}", msg.CreateTime_str, (int)MsgType.b2c_GroupMsg, msg.From.UID, msg.Content));
                                    successUser = true;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                        }
                        if (successUser)
                        {
                            id0s += "," + i.Key;
                        }
                        dealCount++;
                    }
                }
                else
                {
                    if (Util.iDb_read.GetFirstColumnString(string.Format("select count(1) from CHAT_SYSGROUP where ID0='{0}'", group.ID0)) == "0")
                    {
                        //如果不存在这个群组
                        WebSocket socket;
                        if (MessageManager.WebSockets.TryGetValue(msg.From.ID, out socket))
                        {
                            if (socket != null && socket.IsAlive)
                            {
                                try
                                {
                                    socket.Send(string.Format("{0}#{1}#{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), (int)MsgType.b2c_Error, "未找到指定的分组:" + group.ID0));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                        }
                        return;
                    }
                }
                //将这条消息写入到数据库
                WriteGroupMsg2Db(msg, dealCount, id0s);
            }
        }

        /// <summary>将发送的分组消息和处理情况写入数据库
        /// </summary>
        private static void WriteGroupMsg2Db(Msg msg, int dealCount, string id0s)
        {
            Hashtable ht = new Hashtable();
            DBUtil.IDbAccess iDb = Util.iDb_read.NewIDB();
            ht.Add("ID0", iDb.IDSNOManager.NewID(iDb, "CHAT_MSG_GROUP", "ID0"));
            ht.Add("GID", msg.Group.ID0);
            ht.Add("MFROMLOGIN", msg.From.UserLoginName);
            ht.Add("MFROMNAME", msg.From.UserName);
            ht.Add("MFROMID", msg.From.UID);
            ht.Add("CCOUNT", iDb.GetFirstColumn(string.Format("select count(1) from CHAT_USER_GROUP where GROUPID ='{0}'", msg.Group.ID0)));
            ht.Add("MSG", msg.Content);
            ht.Add("READCOUNT", dealCount);
            ht.Add("UIDS", id0s);
            ht.Add("MTIME", msg.CreateTime_str);
            iDb.AddData("CHAT_MSG_GROUP", ht);
        }

        public class _Group
        {
            public int ID0 { set; get; }
            public int PID { set; get; }
            public string Name { set; get; }

            public List<_Group> Children = new List<_Group>();
            public List<_User> Users = new List<_User>();
        }
        public class _User
        {
            public int ID0 { set; get; }
            public string LoginName { set; get; }
            public string UserName { set; get; }
            public string UDesc { set; get; }
            public string UImg { set; get; }
            public bool isOnline { set; get; }
        }

        /// <summary>递归组装用户组织树
        /// </summary>
        private static void _createUserTree(List<_Group> groups, DataRowCollection rows, DataRowCollection users, OnLineData online)
        {
            if (groups == null || groups.Count == 0) return;
            for (int i = 0; i < groups.Count; i++)
            {
                //首先寻找用户
                for (var h = users.Count - 1; h >= 0; h--)
                {
                    int groupid = int.Parse(users[h]["GROUPID"].ToString());
                    if (groupid == groups[i].ID0)
                    {
                        _User user = new _User()
                        {
                            UserName = users[h]["USERNAME"].ToString(),
                            LoginName = users[h]["USERLOGIN"].ToString(),
                            UDesc = users[h]["UDESC"].ToString(),
                            UImg = users[h]["UIMG"].ToString(),
                            ID0 = int.Parse(users[h]["ID0"].ToString())
                        };
                        groups[i].Users.Add(user);
                        if (online.Uid_Users.ContainsKey(user.ID0))
                        {
                            user.isOnline = true;
                        }
                        users.RemoveAt(h);
                    }
                }

                //寻找下层分组
                for (var j = rows.Count - 1; j >= 0; j--)
                {
                    int pid = int.Parse(rows[j]["PID"].ToString());
                    if (pid == groups[i].ID0)
                    {
                        groups[i].Children.Add(new _Group()
                        {
                            ID0 = int.Parse(rows[j]["ID0"].ToString()),
                            PID = pid,
                            Name = rows[j]["GNAME"].ToString()
                        });
                        rows.RemoveAt(j);
                    }
                }
                _createUserTree(groups[i].Children, rows, users, online);
            }
        }

        /// <summary>处理获取用户组织树的消息请求
        /// </summary>
        private static void DealUserTreeMsg()
        {
            DBUtil.IDbAccess iDb = Util.iDb_read;
            while (true)
            {
                Msg msg = _userTreeQueue.DeQueue();
                OnLineData online = OnLineData.GetReleaseModelReadOnly();
                string userlogin = msg.From.UserName;
                DataTable dt = iDb.GetDataTable("select * from CHAT_SYSGROUP");
                DataTable dt_users = iDb.GetDataTable("select CHAT_USER_GROUP.*,CHAT_SYSUSER.UDESC,CHAT_SYSUSER.UIMG from CHAT_USER_GROUP left join CHAT_SYSUSER on CHAT_USER_GROUP.USERID=CHAT_SYSUSER.ID0");
                List<_Group> groups = new List<_Group>();
                int count = dt.Rows.Count;
                for (var i = count - 1; i >= 0; i--)
                {
                    if (dt.Rows[i]["PID"] == DBNull.Value || dt.Rows[i]["PID"] == null || string.IsNullOrWhiteSpace(dt.Rows[i]["PID"].ToString()))
                    {
                        _Group g = new _Group();
                        groups.Add(g);
                        g.ID0 = int.Parse(dt.Rows[i]["ID0"].ToString());
                        g.Name = dt.Rows[i]["GNAME"].ToString();
                        dt.Rows.RemoveAt(i);
                    }
                }
                _createUserTree(groups, dt.Rows, dt_users.Rows, online);
                string content = Newtonsoft.Json.JsonConvert.SerializeObject(groups);
                WebSocket socket;
                if (MessageManager.WebSockets.TryGetValue(msg.From.ID, out socket))
                {
                    if (socket != null && socket.IsAlive)
                    {
                        try
                        {
                            socket.Send(string.Format("{0}#{1}#{2}", msg.CreateTime_str, (int)MsgType.b2c_UserTree, content));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                        }
                    }

                }
            }
        }

        /// <summary>处理获取用户的历史消息
        /// </summary>
        private static void DealHistoryMsg()
        {
            DBUtil.IDbAccess iDb = Util.iDb_read.NewIDB();
            while (true)
            {
                Msg msg = _historyDataQueue.DeQueue();
                //1.读取未阅读全局消息
                DataTable dt = iDb.GetDataTable(string.Format("select * from CHAT_MSG_GLOBAL where UIDS not like '%,{0},%' and UIDS not like '{0},%' and UIDS not like '%,{0}'  order by ID0 asc", msg.From.UID));
                List<object> list = new List<object>();
                int count = dt.Rows.Count;
                string id0s = "";
                string id0s_g = "";
                string id0s_u = "";
                for (int i = 0; i < count; i++)
                {
                    if (id0s == "")
                    {
                        id0s += dt.Rows[i]["ID0"].ToString();
                    }
                    else
                    {
                        id0s += "," + dt.Rows[i]["ID0"].ToString();
                    }
                    list.Add(new
                    {
                        MFROMID = dt.Rows[i]["MFROMID"].ToString(),
                        MFROMLOGIN = dt.Rows[i]["MFROMLOGIN"].ToString(),
                        MFROMNAME = dt.Rows[i]["MFROMNAME"].ToString(),
                        MTIME = dt.Rows[i]["MTIME"].ToString(),
                        MSG = dt.Rows[i]["MSG"].ToString(),
                    });
                }
                //2.读取未阅读群组消息
                dt = iDb.GetDataTable(string.Format("select * from CHAT_MSG_GROUP where UIDS not like '%,{0},%' and UIDS not like '{0},%' and UIDS not like '%,{0}'  order by ID0 asc", msg.From.UID));
                count = dt.Rows.Count;
                List<object> list_g = new List<object>();
                for (int i = 0; i < count; i++)
                {
                    if (id0s_g == "")
                    {
                        id0s_g += dt.Rows[i]["ID0"].ToString();
                    }
                    else
                    {
                        id0s_g += "," + dt.Rows[i]["ID0"].ToString();
                    }
                    list_g.Add(new
                    {
                        GID = dt.Rows[i]["GID"].ToString(),
                        MFROMID = dt.Rows[i]["MFROMID"].ToString(),
                        MFROMLOGIN = dt.Rows[i]["MFROMLOGIN"].ToString(),
                        MFROMNAME = dt.Rows[i]["MFROMNAME"].ToString(),
                        MTIME = dt.Rows[i]["MTIME"].ToString(),
                        MSG = dt.Rows[i]["MSG"].ToString(),
                    });
                }
                //3.读取未阅读个人消息
                dt = iDb.GetDataTable(string.Format("select * from CHAT_MSG_USER where MTO ='{0}' and HASDEAL=0 order by ID0 asc", msg.From.UID));
                count = dt.Rows.Count;
                List<object> list_u = new List<object>();
                for (int i = 0; i < count; i++)
                {
                    if (id0s_u == "")
                    {
                        id0s_u += dt.Rows[i]["ID0"].ToString();
                    }
                    else
                    {
                        id0s_u += "," + dt.Rows[i]["ID0"].ToString();
                    }
                    list_u.Add(new
                    {
                        MFROMID = dt.Rows[i]["MFROMID"].ToString(),
                        MFROMLOGIN = dt.Rows[i]["MFROMLOGIN"].ToString(),
                        MFROMNAME = dt.Rows[i]["MFROMNAME"].ToString(),
                        MTIME = dt.Rows[i]["MTIME"].ToString(),
                        MSG = dt.Rows[i]["MSG"].ToString(),
                    });
                }
                Dictionary<string, List<object>> dic = new Dictionary<string, List<object>>();
                dic.Add("global", list);
                dic.Add("group", list_g);
                dic.Add("user", list_u);
                string str = string.Format("{0}#{1}#{2}", msg.CreateTime_str, (int)MsgType.b2c_History, Newtonsoft.Json.JsonConvert.SerializeObject(dic));
                WebSocket socket;
                if (MessageManager.WebSockets.TryGetValue(msg.From.ID, out socket))
                {
                    if (socket != null && socket.IsAlive)
                    {
                        try
                        {
                            socket.Send(str);
                            if (!string.IsNullOrWhiteSpace(id0s_u))
                            {
                                //更新用户
                                iDb.ExecuteSql(string.Format("update CHAT_MSG_USER set HASDEAL=1 where ID0 in ({0})", id0s_u));
                            }
                            if (!string.IsNullOrWhiteSpace(id0s_g))
                            {
                                //更新群组
                                iDb.ExecuteSql(string.Format("update CHAT_MSG_GROUP set READCOUNT=READCOUNT+1,UIDS=UIDS+',{0}' where ID0 in ({1})", msg.From.UID, id0s_g));
                            }
                            if (!string.IsNullOrWhiteSpace(id0s))
                            {
                                //更新全局
                                iDb.ExecuteSql(string.Format("update CHAT_MSG_GLOBAL set READCOUNT=READCOUNT+1,UIDS=UIDS+',{0}' where ID0 in ({1})", msg.From.UID, id0s));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                        }
                    }
                }
            }
        }

        /// <summary>处理发送的全局消息
        /// </summary>
        private static void DealGlobalMsg()
        {
            while (true)
            {
                Msg msg = _globalQueue.DeQueue();
                OnLineData online = OnLineData.GetReleaseModelReadOnly();
                int dealCount = 0;
                string id0s = "";
                if (online.Link_Users.Count > 0)
                {
                    foreach (var i in online.Link_Users)
                    {
                        bool successUser = false;
                        foreach (var j in i.Value.Clients)
                        {
                            WebSocket socket;
                            if (MessageManager.WebSockets.TryGetValue(j.Key, out socket))
                            {
                                if (socket != null && socket.IsAlive)
                                {
                                    try
                                    {
                                        socket.Send(string.Format("{0}#{1}#{2}#{3}", msg.CreateTime_str, (int)MsgType.b2c_GlobalMsg, msg.From.UID, msg.Content));
                                        successUser = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                        Console.WriteLine(ex.StackTrace);
                                    }
                                }
                            }
                        }
                        if (successUser)
                        {
                            id0s += "," + i.Value.ID0;
                            dealCount++;
                        }
                    }
                }
                //将这条消息写入到数据库
                WriteGlobalMsg2Db(msg, dealCount, id0s);
            }
        }

        /// <summary>将全局消息和已处理情况写入数据库
        /// </summary>
        private static void WriteGlobalMsg2Db(Msg msg, int dealCount, string id0s)
        {
            Hashtable ht = new Hashtable();
            DBUtil.IDbAccess iDb = Util.iDb_read.NewIDB();
            ht.Add("ID0", iDb.IDSNOManager.NewID(iDb, "CHAT_MSG_GLOBAL", "ID0"));
            ht.Add("MFROMLOGIN", msg.From.UserLoginName);
            ht.Add("MFROMNAME", msg.From.UserName);
            ht.Add("MFROMID", msg.From.UID);
            ht.Add("CCOUNT", iDb.GetFirstColumn(string.Format("select count(1) from CHAT_SYSUSER")));
            ht.Add("MSG", msg.Content);
            ht.Add("READCOUNT", dealCount);
            ht.Add("UIDS", id0s);
            ht.Add("MTIME", msg.CreateTime_str);
            iDb.AddData("CHAT_MSG_GLOBAL", ht);
        }

        /// <summary>记录所有的连接ID与映射
        /// </summary>
        public static ConcurrentDictionary<string, WebSocket> WebSockets = new ConcurrentDictionary<string, WebSocket>();
    }
}
