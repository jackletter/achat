using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat
{
    public class Util
    {
        public static string DbConn { set; get; }
        public static string DbType { set; get; }

        /// <summary>数据库只读对象(可共享使用)
        /// </summary>
        public static DBUtil.IDbAccess iDb_read { set; get; }

        static Util()
        {
            DbType = ConfigurationManager.AppSettings["DBType"];
            DbConn = ConfigurationManager.AppSettings["DBConn"];
            iDb_read = DBUtil.IDBFactory.CreateIDB(DbConn, DbType);
            iDb_read.IsKeepConnect = true;
        }
    }

}
