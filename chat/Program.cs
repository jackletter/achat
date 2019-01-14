using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using System.Threading;
using System.Configuration;
using System.Collections.Concurrent;
using System.Collections;
using System.Data;
using System.IO;


using Newtonsoft.Json;
using WebSocketSharp.Net.WebSockets;

namespace chat
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = ConfigurationManager.AppSettings["sysroot"];
            string port = ConfigurationManager.AppSettings["port"];
            var httpsv = new HttpServer(int.Parse(port));
            httpsv.DocumentRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, str);
            httpsv.OnGet += Httpsv_OnGet;
            httpsv.OnPost += Httpsv_OnPost;
            httpsv.AddWebSocketService<Chat>("/Chat");
            httpsv.Start();
            if (httpsv.IsListening)
            {
                Console.WriteLine("Listening on port {0}, and providing WebSocket services:", httpsv.Port);
                foreach (var path in httpsv.WebSocketServices.Paths)
                    Console.WriteLine("- {0}", path);
            }

            Console.WriteLine("\nPress Enter key to stop the server...");
            Console.ReadLine();
            httpsv.Stop();
        }

        private static void Httpsv_OnGet(object sender, HttpRequestEventArgs e)
        {
            var req = e.Request;
            var res = e.Response;

            var path = req.RawUrl;
            if (path == "/")
                path += "index.html";

            byte[] contents;
            if (path.Contains("?"))
            {
                path = path.Substring(0, path.IndexOf("?"));
            }
            if (!e.TryReadFile(path, out contents))
            {
                res.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            if (path.EndsWith(".html"))
            {
                res.ContentType = "text/html";
                res.ContentEncoding = Encoding.UTF8;
            }
            else if (path.EndsWith(".js"))
            {
                res.ContentType = "application/javascript";
                res.ContentEncoding = Encoding.UTF8;
            }
            if (path.EndsWith(".css"))
            {
                res.ContentType = "text/css";
                res.ContentEncoding = Encoding.UTF8;
            }

            res.WriteContent(contents);
        }

        private static void Httpsv_OnPost(object sender, HttpRequestEventArgs e)
        {
            var req = e.Request;
            var res = e.Response;
            var buffer = new byte[1024 * 1024];
            int len;
            string reqBody = "";
            while ((len = req.InputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                reqBody += System.Text.Encoding.UTF8.GetString(buffer, 0, len);
            }

            UserRegister user = Newtonsoft.Json.JsonConvert.DeserializeObject<UserRegister>(reqBody);

            var path = req.RawUrl;
            if (path.Contains("?"))
            {
                path = path.Substring(0, path.IndexOf('?'));
            }
            if (path == "/api/register")
            {
                DBUtil.IDbAccess iDb = Util.iDb_read.NewIDB();
                if (iDb.GetFirstColumnString("select count(1) from CHAT_USER where LOGINNAME=" + iDb.paraPrefix + "user", new IDbDataParameter[] { iDb.CreatePara("user", user.LOGINNAME) }) != "0")
                {
                    res.WriteContent(System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        success = false,
                        data = "已存在这个登录名,请重新填写!"
                    })));
                }
                else
                {
                    Hashtable ht = new Hashtable();
                    int userid = iDb.IDSNOManager.NewID(iDb, "CHAT_USER", "ID0");
                    string str_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    ht.Add("ID0", userid);
                    ht.Add("LOGINNAME", user.LOGINNAME);
                    ht.Add("UNAME", user.UNAME);
                    ht.Add("USEX", user.USEX);
                    ht.Add("UAGE", user.UAGE);
                    ht.Add("UIMG", user.UIMG);
                    ht.Add("UDES", user.UDES);
                    ht.Add("ULOCATE", user.ULOCATE);
                    ht.Add("ULOCATENAME", user.ULOCATENAME);
                    ht.Add("CREATETIME", str_time);
                    ht.Add("PWD", user.PWD);
                    iDb.AddData("CHAT_USER", ht);
                    ht.Clear();
                    int inner_gid = iDb.IDSNOManager.NewID(iDb, "CHAT_INNER_GROUP", "ID0");
                    ht.Add("ID0", inner_gid);
                    ht.Add("USERID", userid);
                    ht.Add("GNAME", "我的好友");
                    ht.Add("GORDER", 1);
                    iDb.AddData("CHAT_INNER_GROUP", ht);
                    ht.Clear();
                    ht.Add("ID0", iDb.IDSNOManager.NewID(iDb, "CHAT_USER_RELATION", "ID0"));
                    ht.Add("USERID1", userid);
                    ht.Add("USERID2", userid);
                    ht.Add("INNERGID", inner_gid);
                    ht.Add("CREATETIME", str_time);
                    iDb.AddData("CHAT_USER_RELATION", ht);

                    res.WriteContent(System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        success = true,
                        data = ""
                    })));
                }
            }

        }

    }
    class UserRegister
    {
        public string LOGINNAME { set; get; }
        public string PWD { set; get; }
        public string UNAME { set; get; }
        public string USEX { set; get; }
        public string UAGE { set; get; }
        public string UIMG { set; get; }
        public string UDES { set; get; }
        public string ULOCATE { set; get; }
        public string ULOCATENAME { set; get; }
    }
}
