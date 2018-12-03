using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat
{
    public static class Ext
    {
        public static DBUtil.IDbAccess NewIDB(this DBUtil.IDbAccess idb)
        {
            return DBUtil.IDBFactory.CreateIDB(idb.ConnectionString, idb.DataBaseType.ToString());
        }
    }
}
