using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace chat
{
    public class _Group
    {
        public int ID0 { set; get; }
        public string GNAME { set; get; }
        public string GIMG { set; get; }
        public string GDESC { set; get; }
        public int GORDER { set; get; }
        public List<_User> Users = new List<_User>();
    }
    public class _User
    {
        public int ID0 { set; get; }
        public string LOGINNAME { set; get; }
        public string UNAME { set; get; }
        public string UDES { set; get; }
        public string UIMG { set; get; }
        public string ULOCATE { set; get; }
        public string ULOCATENAME { set; get; }
        public string CREATETIME { set; get; }
        public bool IsOnline { set; get; }
    }

    public class Result
    {
        public Boolean Success { set; get; }
        public Object Data { set; get; }
    }

    public class BaseContext
    {
        /// <summary>消息路径
        /// </summary>
        public string Path { set; get; }

        /// <summary>消息来自的客户端对象
        /// </summary>
        public Client From { set; get; }

        /// <summary>这条消息的上下文环境
        /// </summary>
        public WebSocketContext SocketContext { set; get; }

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

        /// <summary>消息头
        /// </summary>
        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        /// <summary>消息体
        /// </summary>
        public dynamic Content { set; get; }
    }
    public class Chat : WebSocketBehavior
    {
        /// <summary>记录所有的连接ID与映射
        /// </summary>
        private static ConcurrentDictionary<string, WebSocket> WebSockets = new ConcurrentDictionary<string, WebSocket>();

        /// <summary>记录所有的处理逻辑
        /// </summary>
        private static Dictionary<string, Func<BaseContext, Result>> _dic = new Dictionary<string, Func<BaseContext, Result>>();

        /// <summary>注册消息接收处理逻辑
        /// </summary>
        /// <param name="path"></param>
        /// <param name="func"></param>
        public static void Accept(string path, Func<BaseContext, Result> func)
        {
            _dic.Add(path, func);
        }


        static Chat()
        {
            Accept("/getUserTree", DealUserTreeMsg);
            Accept("/msgHistory", DealHistoryMsg);
            Accept("/msg2User", DealUserMsg);
            Accept("/msg2Group", DealGroupMsg);
            Accept("/search", DealSearch);
            Accept("/addUser", DealUserAdd);
            Accept("/agreeUserAdd", DealAgreeUserAdd);
        }

        /// <summary>回复消息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="res"></param>
        public static void Reply(BaseContext ctx, Result res)
        {
            WebSocket socket;
            if (WebSockets.TryGetValue(ctx.From.ID, out socket))
            {
                if (socket != null && socket.IsAlive)
                {
                    try
                    {
                        socket.Send(string.Format(@"
reply {0}
guid:{1}

{2}", ctx.Path, ctx.Headers["guid"], Newtonsoft.Json.JsonConvert.SerializeObject(res)).TrimStart());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
        }

        /// <summary>发送给个人用户
        /// </summary>
        public static void SendUser(string path, int uid, string msg)
        {
            OnLineData onlinedata = OnLineData.GetReleaseModelReadOnly();
            try
            {
                User u = null;
                if (onlinedata.Uid_Users.TryGetValue(uid, out u))
                {
                    foreach (var i in u.Clients.Values)
                    {
                        WebSocket sock = null;
                        if (WebSockets.TryGetValue(i.ID, out sock))
                        {
                            sock.Send(string.Format(@"request {0}

guid:1

{1}", path, msg));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>群组消息
        /// </summary>
        public static void SendGroup(string path, int gid, string msg)
        {
            OnLineData onlinedata = OnLineData.GetReleaseModelReadOnly();
            try
            {
                Group g = null;
                if (onlinedata.Gid_Groups.TryGetValue(gid, out g))
                {
                    foreach (var i in g.Users)
                    {
                        foreach (var j in i.Value.Clients)
                        {
                            WebSocket sock = null;
                            if (WebSockets.TryGetValue(j.Value.ID, out sock))
                            {
                                sock.Send(string.Format(@"request {0}

guid:1

{1}", path, msg));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>广播消息
        /// </summary>
        public static void SendBroadCast(string path, string msg)
        {

            OnLineData onlinedata = OnLineData.GetReleaseModelReadOnly();
            try
            {
                List<WebSocket> list = WebSockets.Values.ToList();
                foreach (var i in list)
                {
                    i.Send(string.Format(@"request {0}
guid:1

{1}", path, msg));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>处理获取用户组织树的消息请求
        /// </summary>
        private static Result DealUserTreeMsg(BaseContext ctx)
        {
            DBUtil.IDbAccess iDb = Util.iDb_read;
            OnLineData online = OnLineData.GetReleaseModelReadOnly();
            int userid = ctx.From.USERID;
            //获取到这个用户加入的所有分组
            DataTable dt_user_groups = iDb.GetDataTable("select b.* from CHAT_USER_GROUP a left join CHAT_GROUP b on a.GID=b.ID0 where a.USERID='" + userid + "'");
            //获取到这个用户所有的好友和分组
            DataTable dt_users = iDb.GetDataTable("select b.*,a.INNERGID from CHAT_USER_RELATION a left join CHAT_USER b on a.USERID2=b.ID0 where a.USERID1='" + userid + "'");
            //获取这个用户所有的内部分组
            DataTable dt_inner_groups = iDb.GetDataTable("select * from CHAT_INNER_GROUP where USERID='" + userid + "' order by GORDER");

            //组装当前用户所在的群
            List<_Group> groups = new List<_Group>();
            int i = 0, count = 0;
            for (i = 0, count = dt_user_groups.Rows.Count; i < count; i++)
            {
                _Group g = new _Group();
                g.ID0 = int.Parse(dt_user_groups.Rows[i]["ID0"].ToString());
                g.GNAME = dt_user_groups.Rows[i]["GNAME"].ToString();
                g.GDESC = dt_user_groups.Rows[i]["GDESC"].ToString();
                g.GIMG = dt_user_groups.Rows[i]["GIMG"].ToString();
                groups.Add(g);
                //获取这个分组内的所有人员
                DataTable dt = iDb.GetDataTable("select b.* from CHAT_USER_GROUP a left join CHAT_USER b on a.USERID=b.ID0 where a.GID='" + g.ID0 + "'");

                for (int j = 0, len = dt.Rows.Count; j < len; j++)
                {
                    _User u = new _User();
                    u.ID0 = int.Parse(dt.Rows[j]["ID0"].ToString());
                    u.LOGINNAME = dt.Rows[j]["LOGINNAME"].ToString();
                    u.UDES = dt.Rows[j]["UDES"].ToString();
                    u.UIMG = dt.Rows[j]["UIMG"].ToString();
                    u.ULOCATE = dt.Rows[j]["ULOCATE"].ToString();
                    u.ULOCATENAME = dt.Rows[j]["ULOCATENAME"].ToString();
                    u.UNAME = dt.Rows[j]["UNAME"].ToString();
                    //判断当前用户是否在线
                    if (online.Uid_Users.ContainsKey(u.ID0))
                    {
                        u.IsOnline = true;
                    }
                    g.Users.Add(u);
                }

            }
            //组装当前用户所有的分组好友
            List<_Group> inner_groups = new List<_Group>();
            for (i = 0, count = dt_inner_groups.Rows.Count; i < count; i++)
            {
                _Group g = new _Group();
                g.ID0 = int.Parse(dt_inner_groups.Rows[i]["ID0"].ToString());
                g.GNAME = dt_inner_groups.Rows[i]["GNAME"].ToString();
                g.GORDER = int.Parse(dt_inner_groups.Rows[i]["GORDER"].ToString());
                inner_groups.Add(g);
                int j = 0, jcount = 0;
                for (j = 0, jcount = dt_users.Rows.Count; j < jcount; j++)
                {
                    int INNERGID = int.Parse(dt_users.Rows[j]["INNERGID"].ToString());
                    if (INNERGID == g.ID0)
                    {
                        _User u = new _User();
                        u.ID0 = int.Parse(dt_users.Rows[j]["ID0"].ToString());
                        u.LOGINNAME = dt_users.Rows[j]["LOGINNAME"].ToString();
                        u.UDES = dt_users.Rows[j]["UDES"].ToString();
                        u.UIMG = dt_users.Rows[j]["UIMG"].ToString();
                        u.ULOCATE = dt_users.Rows[j]["ULOCATE"].ToString();
                        u.ULOCATENAME = dt_users.Rows[j]["ULOCATENAME"].ToString();
                        u.UNAME = dt_users.Rows[j]["UNAME"].ToString();
                        //判断当前用户是否在线
                        if (online.Uid_Users.ContainsKey(u.ID0))
                        {
                            u.IsOnline = true;
                        }
                        g.Users.Add(u);
                    }
                }
            }
            Result res = new Result();
            res.Success = true;
            res.Data = new
            {
                groups = groups,
                inner_groups = inner_groups
            };
            return res;
        }

        /// <summary>处理获取用户的历史消息
        /// </summary>
        /// <returns></returns>
        public static Result DealHistoryMsg(BaseContext ctx)
        {
            DBUtil.IDbAccess iDb = Util.iDb_read.NewIDB();
            Dictionary<string, Object> dic = new Dictionary<string, object>();
            //1.读取每个群组消息(默认读取10条)
            DataTable dt = iDb.GetDataTable(string.Format("exec PROC_GROUP_MSG_HISTORY {0},10", ctx.From.USERID));
            int count = dt.Rows.Count;
            Dictionary<string, List<Object>> dic_group = new Dictionary<string, List<object>>();
            List<Object> list = null;
            if (count > 0)
            {
                list = new List<object>();
                string gid = "-1";
                for (int i = 0; i < count; i++)
                {
                    string _gid = dt.Rows[i]["DESTGID"].ToString();
                    Object obj = new
                    {
                        ID0 = dt.Rows[i]["ID0"].ToString(),
                        DESTGID = dt.Rows[i]["DESTGID"].ToString(),
                        FROMUSERID = dt.Rows[i]["FROMUSERID"].ToString(),
                        MSGTYPE = dt.Rows[i]["MSGTYPE"].ToString(),
                        MSGTEXT = dt.Rows[i]["MSGTEXT"].ToString(),
                        CREATETIME = dt.Rows[i]["CREATETIME"].ToString()
                    };
                    if (gid == "-1")
                    {
                        //第一行记录
                        list.Add(obj);
                        gid = _gid;
                        dic_group.Add("group_" + _gid, list);
                    }
                    else if (gid == _gid)
                    {
                        //非第一行,与前一条是相同的群组消息
                        list.Add(obj);
                    }
                    else if (gid != _gid)
                    {
                        //非第一行,与前一条不是相同的群组消息
                        list = new List<object>();
                        gid = _gid;
                        dic_group.Add("group_" + _gid, list);
                        list.Add(obj);
                    }
                }
            }

            //2.读取未阅读个人消息
            dt = iDb.GetDataTable(string.Format("exec PROC_USER_MSG_HISTORY {0},10", ctx.From.USERID));
            count = dt.Rows.Count;
            Dictionary<string, List<Object>> dic_user = new Dictionary<string, List<object>>();
            if (count > 0)
            {
                list = new List<object>();
                string uid = "-1";
                for (int i = 0; i < count; i++)
                {
                    string _uid = dt.Rows[i]["FROMUSERID"].ToString() == ctx.From.USERID.ToString() ? dt.Rows[i]["DESTUSERID"].ToString() : dt.Rows[i]["FROMUSERID"].ToString();
                    Object obj = new
                    {
                        ID0 = dt.Rows[i]["ID0"].ToString(),
                        DESTUSERID = dt.Rows[i]["DESTUSERID"].ToString(),
                        FROMUSERID = dt.Rows[i]["FROMUSERID"].ToString(),
                        MSGTYPE = dt.Rows[i]["MSGTYPE"].ToString(),
                        MSGTEXT = dt.Rows[i]["MSGTEXT"].ToString(),
                        CREATETIME = dt.Rows[i]["CREATETIME"].ToString()
                    };
                    if (uid == "-1")
                    {
                        //第一行记录
                        list.Add(obj);
                        uid = _uid;
                        dic_user.Add("user_" + _uid, list);
                    }
                    else if (uid == _uid)
                    {
                        //非第一行,与前一条是相同的群组消息
                        list.Add(obj);
                    }
                    else if (uid != _uid)
                    {
                        //非第一行,与前一条不是相同的群组消息
                        list = new List<object>();
                        uid = _uid;
                        dic_user.Add("user_" + _uid, list);
                        list.Add(obj);
                    }
                }
            }

            //3.读取好友添加请求消息(只返回前10条)
            List<Object> dic_useradd = new List<Object>();
            string sql = string.Format("select * from CHAT_USERADD where DESTUSERID ='{0}'", ctx.From.USERID);
            dt = iDb.GetDataTable(iDb.GetSqlForPageSize(sql, " order by ID0 DESC", 10, 1));
            count = dt.Rows.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    string id0 = dt.Rows[i]["ID0"].ToString();
                    Object obj = new
                    {
                        ID0 = id0,
                        DESTUSERID = dt.Rows[i]["DESTUSERID"].ToString(),
                        FROMUSERID = dt.Rows[i]["FROMUSERID"].ToString(),
                        MSGTEXT = dt.Rows[i]["MSGTEXT"].ToString(),
                        CREATETIME = dt.Rows[i]["CREATETIME"].ToString(),
                        DEALTYPE = dt.Rows[i]["DEALTYPE"].ToString(),
                        BACKMSGTEXT = dt.Rows[i]["BACKMSGTEXT"].ToString()
                    };
                    dic_useradd.Add(obj);
                }
            }

            dic.Add("group", dic_group);
            dic.Add("user", dic_user);
            dic.Add("useradd", dic_useradd);
            Result res = new Result();
            res.Success = true;
            res.Data = dic;
            return res;
        }

        /// <summary>处理发送的个人消息
        /// </summary>
        /// <returns></returns>
        public static Result DealUserMsg(BaseContext ctx)
        {
            int destUID = int.Parse(string.Format("{0}", ctx.Content.destUID));
            if (Util.iDb_read.GetFirstColumnString(string.Format("select count(1) from CHAT_USER_RELATION where USERID1='{0}' and USERID2='{1}'", destUID, ctx.From.USERID)) == "0")
            {
                //不在对方好友中,无法发送消息
                return new Result()
                {
                    Success = false,
                    Data = string.Format("你不在对方好友列表中,消息无法送达!")
                };
            }
            if (Util.iDb_read.GetFirstColumnString(string.Format("select count(1) from CHAT_USER where ID0='{0}'", ctx.Content.destUID)) == "0")
            {
                //不存在这个人
                return new Result()
                {
                    Success = false,
                    Data = string.Format("不存在这个人!")
                };
            }
            //发送消息给这个用户
            SendUser("/msg2User", destUID, Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                from = ctx.From.USERID,
                to = destUID,
                Msg_Text = ctx.Content.msg,
                Msg_Time = ctx.CreateTime_str

            }));
            //消息反馈
            SendUser("/msg2User", ctx.From.USERID, Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                from = ctx.From.USERID,
                to = destUID,
                Msg_Text = ctx.Content.msg,
                Msg_Time = ctx.CreateTime_str
            }));
            //将这条消息写入到数据库
            WriteUserMsg2Db(ctx);
            return null;
        }

        /// <summary>将发送的个人消息和处理情况写入数据库
        /// </summary>
        private static void WriteUserMsg2Db(BaseContext ctx)
        {
            Hashtable ht = new Hashtable();
            DBUtil.IDbAccess iDb = Util.iDb_read.NewIDB();
            ht.Add("ID0", iDb.IDSNOManager.NewID(iDb, "CHAT_MSG_USER", "ID0"));
            ht.Add("FROMUSERID", ctx.From.USERID);
            ht.Add("DESTUSERID", string.Format("{0}", ctx.Content.destUID));
            ht.Add("MSGTEXT", string.Format("{0}", ctx.Content.msg));
            ht.Add("MSGTYPE", 1);//暂时都是1
            ht.Add("CREATETIME", ctx.CreateTime_str);
            iDb.AddData("CHAT_MSG_USER", ht);
        }

        /// <summary>处理发送的群组消息
        /// </summary>
        /// <returns></returns>
        public static Result DealGroupMsg(BaseContext ctx)
        {
            int gid = ctx.Content.destGID;
            if (Util.iDb_read.GetFirstColumnString(string.Format("select count(1) from CHAT_GROUP where ID0='{0}'", gid)) == "0")
            {
                //如果不存在这个群组
                return new Result()
                {
                    Success = false,
                    Data = "不存在这个群组!"
                };
            }
            //发送消息给这个群组
            SendGroup("/msg2Group", gid, Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                from = ctx.From.USERID,
                to = gid,
                Msg_Text = ctx.Content.msg,
                Msg_Time = ctx.CreateTime_str
            }));
            //将这条消息写入到数据库
            WriteGroupMsg2Db(ctx);
            return null;
        }

        /// <summary>处理好友和群组搜索请求
        /// </summary>
        /// <returns></returns>
        public static Result DealSearch(BaseContext ctx)
        {
            //搜索好友
            DBUtil.IDbAccess iDb = Util.iDb_read;
            string type = string.Format("{0}", ctx.Content.type);
            string msg = string.Format("{0}", ctx.Content.msg);
            if (type == "user")
            {
                DataTable dt = iDb.GetDataTable(string.Format("select * from CHAT_USER where LOGINNAME like '%'+{0}loginname + '%' or UNAME like '%'+{0}uname+'%'", iDb.paraPrefix), new IDbDataParameter[] { iDb.CreatePara("loginname", msg), iDb.CreatePara("uname", msg) });
                List<Object> res = new List<object>();
                if (dt.Rows.Count > 0)
                {
                    int count = dt.Rows.Count;
                    for (int i = 0; i < count; i++)
                    {
                        res.Add(new
                        {
                            ID0 = int.Parse(dt.Rows[i]["ID0"].ToString()),
                            LOGINNAME = dt.Rows[i]["LOGINNAME"].ToString(),
                            UNAME = dt.Rows[i]["UNAME"].ToString(),
                            USEX = dt.Rows[i]["USEX"].ToString(),
                            UAGE = int.Parse(dt.Rows[i]["UAGE"].ToString() == "" ? "0" : dt.Rows[i]["UAGE"].ToString()),
                            UIMG = dt.Rows[i]["UIMG"].ToString(),
                            UDES = dt.Rows[i]["UDES"].ToString(),
                            ULOCATE = dt.Rows[i]["ULOCATE"].ToString(),
                            ULOCATENAME = dt.Rows[i]["ULOCATENAME"].ToString(),
                        });
                    }
                }
                Result result = new Result();
                result.Success = true;
                result.Data = res;
                return result;
            }
            else if (type == "group")
            {
                DataTable dt = iDb.GetDataTable(string.Format("select * from CHAT_GROUP where ID0 like '%'+{0}ID0 + '%' or GNAME like '%'+{0}GNAME+'%'", iDb.paraPrefix), new IDbDataParameter[] { iDb.CreatePara("ID0", ctx.Content.msg), iDb.CreatePara("GNAME", ctx.Content.msg) });
                List<Object> res = new List<object>();
                if (dt.Rows.Count > 0)
                {
                    int count = dt.Rows.Count;
                    for (int i = 0; i < count; i++)
                    {
                        res.Add(new
                        {
                            ID0 = int.Parse(dt.Rows[i]["ID0"].ToString()),
                            GNAME = dt.Rows[i]["GNAME"].ToString(),
                            GIMG = dt.Rows[i]["GIMG"].ToString(),
                            GOWNER = int.Parse(dt.Rows[i]["GOWNER"].ToString() == "" ? "0" : dt.Rows[i]["UAGE"].ToString()),
                            CREATETIME = dt.Rows[i]["CREATETIME"].ToString(),
                            GDESC = dt.Rows[i]["GDESC"].ToString()
                        });
                    }
                }
                Result result = new Result();
                result.Success = true;
                result.Data = res;
                return result;
            }
            return new Result()
            {
                Success = false,
                Data = "未找到搜索的类型,搜索类型必须是好友或群组"
            };

        }

        /// <summary>处理好友添加请求消息
        /// </summary>
        /// <returns></returns>
        public static Result DealUserAdd(BaseContext ctx)
        {
            string destUID = string.Format("{0}", ctx.Content.destUID);
            string msg = string.Format("{0}", ctx.Content.msg);
            SendUser("/userAdd", int.Parse(destUID), Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                FROMUSERID = ctx.From.USERID,
                CREATETIME = ctx.CreateTime_str,
                MSGTEXT = msg
            }));
            //请求添加好友
            WriteUserAdd2Db(ctx);
            return null;
        }

        /// <summary>处理接受好友添加请求
        /// </summary>
        /// <returns></returns>
        public static Result DealAgreeUserAdd(BaseContext ctx)
        {
            return null;
        }

        /// <summary>将好友添加请求写入数据库
        /// </summary>
        private static void WriteUserAdd2Db(BaseContext ctx)
        {
            string destUID = string.Format("{0}", ctx.Content.destUID);
            string msg = string.Format("{0}", ctx.Content.msg);
            Hashtable ht = new Hashtable();
            DBUtil.IDbAccess iDb = Util.iDb_read.NewIDB();
            ht.Add("ID0", iDb.IDSNOManager.NewID(iDb, "CHAT_USERADD", "ID0"));
            ht.Add("FROMUSERID", ctx.From.USERID);
            ht.Add("DESTUSERID", destUID);
            ht.Add("MSGTEXT", msg);
            ht.Add("CREATETIME", ctx.CreateTime_str);
            ht.Add("DEALTYPE", "0");
            iDb.AddData("CHAT_USERADD", ht);
        }

        /// <summary>将发送的分组消息和处理情况写入数据库
        /// </summary>
        private static void WriteGroupMsg2Db(BaseContext ctx)
        {
            Hashtable ht = new Hashtable();
            DBUtil.IDbAccess iDb = Util.iDb_read.NewIDB();
            ht.Add("ID0", iDb.IDSNOManager.NewID(iDb, "CHAT_MSG_GROUP", "ID0"));
            ht.Add("DESTGID", ctx.Content.destGID);
            ht.Add("FROMUSERID", ctx.From.USERID);
            ht.Add("MSGTYPE", "1");//暂时都是1
            ht.Add("MSGTEXT", ctx.Content.msg);
            ht.Add("CREATETIME", ctx.CreateTime_str);
            iDb.AddData("CHAT_MSG_GROUP", ht);
        }

        /// <summary>接收客户端消息
        /// </summary>
        protected override void OnMessage(MessageEventArgs e)
        {
            OnLineData online = OnLineData.GetReleaseModelReadOnly();
            //构造上下文环境
            BaseContext ctx = new BaseContext();
            ctx.CreateTime = DateTime.Now;
            ctx.SocketContext = Context;
            ctx.From = online.Link_Clients[ID];

            string str = e.Data;
            int index = str.IndexOf("\n");
            string[] arr = str.Substring(0, index).Split(' ');
            if (arr[0] == "request")
            {
                //客户端请求消息
                string path = ctx.Path = arr[1];
                str = str.Substring(index + 1);
                string[] headers = null;
                string body = null;
                if (str.Contains("\n\n") || str.Contains("\r\n\r\n"))
                {
                    string[] arr2 = str.Split(new string[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    headers = arr2[0].Split('\r', '\n');
                    body = arr2.Length == 2 ? arr2[1] : null;
                }
                else
                {
                    headers = str.Split('\r', '\n');
                }
                for (int i = 0; i < headers.Length; i++)
                {
                    string line = headers[i];
                    if (!line.Contains(":")) continue;
                    ctx.Headers.Add(line.Split(':')[0], line.Split(':')[1]);
                }
                if (!string.IsNullOrWhiteSpace(body))
                {
                    ctx.Content = Newtonsoft.Json.JsonConvert.DeserializeObject(body);
                }
                if (_dic.ContainsKey(path))
                {
                    Result res = _dic[path](ctx);
                    if (res != null)
                    {
                        Reply(ctx, res);
                    }
                }
                else
                {
                    Reply(ctx, new Result()
                    {
                        Success = false,
                        Data = string.Format("找不到路径:{0} 的处理逻辑!", path)
                    });
                }
            }
            else if (arr[0] == "reply")
            {
                //客户端回复消息

            }
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
            WebSockets.TryRemove(ID, out socket);
            User user = null;
            //是否移除用户
            bool isRemoveUser = false;
            DBUtil.IDbAccess iDb = Util.iDb_read;
            OnLineData.Edit(online =>
            {
                Client client;
                if (online.Link_Clients.TryGetValue(ID, out client))
                {
                    //1.移除客户端
                    online.Link_Clients.Remove(ID);
                }
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
                return true;
            });
            OnLineData onlinedata = OnLineData.GetReleaseModelReadOnly();
            if (user != null && isRemoveUser)
            {
                //广播下线消息(存在这个用户并且这个用户的客户端全部下线后)
                //仅将消息发送给这个用户的好友
                try
                {
                    DataTable dt = iDb.GetDataTable("select * from CHAT_USER_RELATION a left join CHAT_USER b on a.USERID2=b.ID0 where a.USERID1='" + user.ID0 + "'");
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        int userid = int.Parse(dt.Rows[i]["USERID2"].ToString());
                        SendUser("/BroadUserOffLine", userid, Newtonsoft.Json.JsonConvert.SerializeObject(new { uid = user.ID0 }));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        /// <summary>用户客户端上线
        /// </summary>
        protected override void OnOpen()
        {
            //进入锁之前将数据准备好
            string userlogin = Context.QueryString["user"];
            string password = Context.QueryString["pwd"];
            DBUtil.IDbAccess iDb = Util.iDb_read;

            //准备用户信息
            DataTable dt_user = iDb.GetDataTable(string.Format("select * from CHAT_USER where LOGINNAME={0} and PWD = {1}", iDb.paraPrefix + "uname", iDb.paraPrefix + "upwd"), new IDbDataParameter[] { iDb.CreatePara("uname", userlogin), iDb.CreatePara("upwd", password) });
            if (dt_user.Rows.Count == 0)
            {
                try
                {
                    Context.WebSocket.Send(string.Format("登录失败:用户或密码错误"));
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
            client.LOGINNAME = userlogin;
            client.UNAME = dt_user.Rows[0]["UNAME"].ToString();
            client.USERID = int.Parse(dt_user.Rows[0]["ID0"].ToString());

            //添加连接对象
            WebSockets.TryAdd(ID, Context.WebSocket);

            //准备群组用户群组对象
            DataTable dt_user_group = iDb.GetDataTable(string.Format("select CHAT_GROUP.* from CHAT_USER_GROUP left join CHAT_GROUP on CHAT_USER_GROUP.GID=CHAT_GROUP.ID0 where USERID='{0}'", client.USERID));

            OnLineData.Edit(online =>
            {
                //用户对象
                User user = null;
                online.Link_Clients.Add(ID, client);

                if (!online.Uid_Users.TryGetValue(client.USERID, out user))
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
                            int gid = int.Parse(dt_user_group.Rows[i]["ID0"].ToString());
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
                return true;
            });
            //广播用户上线通知
            SendBroadCast("/BroadUserOnLine", Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                type = "BroadUserOnLine",
                loginname = client.LOGINNAME
            }));
        }
    }
}
