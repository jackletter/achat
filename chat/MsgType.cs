using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat
{
    //消息的类型
    public enum MsgType
    {
        //错误消息
        b2c_Error = -1,
        //历史消息
        c2b_History = 0,
        //获取当前的用户组织树
        c2b_UserTree = 1,
        //发送给单个人的消息
        c2b_UserMsg = 2,
        //发送给群组的消息
        c2b_GroupMsg = 3,
        //发送给全体
        c2b_GlobalMsg = 4,
        //推送个人消息
        b2c_UserMsg = 5,
        //推送群组消息
        b2c_GroupMsg = 6,
        //推送全局消息
        b2c_GlobalMsg = 7,
        //用户上线广播
        b2c_BroadUserOnLine = 8,
        //推送当前的用户组织树
        b2c_UserTree = 9,
        //推送当前用户的历史消息
        b2c_History = 10,
        //用户下线广播
        b2c_BroadUserOffLine = 11
    }
}
