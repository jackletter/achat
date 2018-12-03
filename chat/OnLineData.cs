using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace chat
{
    /// <summary>客户端
    /// </summary>
    public class Client
    {
        /// <summary>连接ID
        /// </summary>
        public string ID { set; get; }

        /// <summary>用户ID
        /// </summary>
        public int UID { set; get; }

        /// <summary>用户昵称
        /// </summary>
        public string UserName { set; get; }

        /// <summary>用户登录名称
        /// </summary>
        public string UserLoginName { set; get; }

        public DateTime StartTime { set; get; }
    }

    /// <summary>分组
    /// </summary>
    public class Group
    {
        public int ID0 { set; get; }
        public string Name { set; get; }
        public Dictionary<int, User> Users = new Dictionary<int, User>();
    }

    /// <summary>用户
    /// </summary>
    public class User
    {
        public string Name { set; get; }
        public string LoginName { set; get; }

        public int ID0 { set; get; }
        public Dictionary<string, Client> Clients = new Dictionary<string, Client>();
    }

    /// <summary>高并发数据(在线数据)
    /// </summary>
    public class OnLineData
    {
        private static OnLineData _releaseObj = new OnLineData();
        private static string _releaseStr = null;

        public static OnLineData GetReleaseModelReadOnly()
        {
            return OnLineData._releaseObj;
        }

        private static OnLineData GetReadWriteModel()
        {
            string str = Newtonsoft.Json.JsonConvert.SerializeObject(OnLineData._releaseObj);
            OnLineData _obj = Newtonsoft.Json.JsonConvert.DeserializeObject<OnLineData>(str);
            return _obj;
        }

        private Dictionary<string, Client> _link_Clients = new Dictionary<string, Client>();
        public Dictionary<string, Client> Link_Clients
        {
            get
            {
                return _link_Clients;
            }
        }

        private Dictionary<string, User> _link_Users = new Dictionary<string, User>();
        public Dictionary<string, User> Link_Users
        {
            get
            {
                return _link_Users;
            }
        }

        private Dictionary<int, Group> _gid_Groups = new Dictionary<int, Group>();
        public Dictionary<int, Group> Gid_Groups
        {
            get
            {
                return _gid_Groups;
            }
        }


        private Dictionary<int, User> _uid_Users = new Dictionary<int, User>();
        public Dictionary<int, User> Uid_Users
        {
            get
            {
                return _uid_Users;
            }
        }

        public static void Edit(Func<OnLineData, bool> func)
        {
            lock (typeof(OnLineData))
            {
                OnLineData obj = OnLineData.GetReadWriteModel();
                bool b = func(obj);
                if (b)
                {
                    string str = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                    OnLineData _obj = Newtonsoft.Json.JsonConvert.DeserializeObject<OnLineData>(str);
                    OnLineData._releaseStr = str;
                    OnLineData._releaseObj = _obj;
                }
            }
        }
    }
}
