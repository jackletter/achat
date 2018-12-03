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
            httpsv.DocumentRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,str);
            httpsv.OnGet += (sender, e) =>
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
            };
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
    }
}
