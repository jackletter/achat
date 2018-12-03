(function (_name, _$, _layerui) {
    var $ = _$;
    var layui = _layerui;
    var layer = layui.layer;
    var chat = window[_name] = {};
    //是否登录
    chat.hasLogin = false;
    //服务端推送所有消息管理
    chat.msgContext = {};
    //消息类型
    chat.MSG_TYPE = {
        //错误消息
        b2c_Error: -1,
        //历史消息
        c2b_History: 0,
        //获取当前的用户组织树
        c2b_UserTree: 1,
        //发送给单个人的消息
        c2b_UserMsg: 2,
        //发送给群组的消息
        c2b_GroupMsg: 3,
        //发送给全体
        c2b_GlobalMsg: 4,
        //推送个人消息
        b2c_UserMsg: 5,
        //推送群组消息
        b2c_GroupMsg: 6,
        //推送全局消息
        b2c_GlobalMsg: 7,
        //用户上线广播
        b2c_BroadUserOnLine: 8,
        //推送当前的用户组织树
        b2c_UserTree: 9,
        //推送当前用户的历史消息
        b2c_History: 10,
        //用户下线广播
        b2c_BroadUserOffLine: 11
    }
    //消息对象
    chat.Msg = function (msg) {
        /**
        Msg_Time:消息产生时间,
        Msg_Type:消息类型
        Msg_TypeStr:消息类型描述
        Msg_Text:消息文本
        Msg_SrcUID:消息来自人员
        Msg_ToGID:消息的目的群组ID
        Msg_Feedback:消息是否是服务反馈的(个人对话中)
        */
        //获取消息类型
        var index = msg.indexOf("#");
        this.MsgTime = msg.substring(0, index);
        msg = msg.substring(index + 1);
        index = msg.indexOf("#");
        this.Msg_Type = msg.substring(0, index);
        for (var i in chat.MSG_TYPE) {
            if (chat.MSG_TYPE[i] == this.Msg_Type) {
                this.Msg_TypeStr = i;
            }
        }
        msg = msg.substring(index + 1);
        if (this.Msg_Type == chat.MSG_TYPE.b2c_UserMsg) {
            //推送个人消息
            var index = msg.indexOf("#");
            this.Msg_SrcUID = msg.substring(0, index);
            if (this.Msg_SrcUID.indexOf("-") > 0) {
                this.Msg_Feedback = true;
                this.Msg_SrcUID = this.Msg_SrcUID.split("-")[1];
            }
            this.Msg_Text = msg.substring(index + 1);
        } else if (this.Msg_Type == chat.MSG_TYPE.b2c_GroupMsg) {
            //推送群组消息
            var index = msg.indexOf("#");
            this.Msg_SrcUID = msg.substring(0, index);
            msg = msg.substring(index + 1);
            index = msg.indexOf("#");
            this.Msg_ToGID = msg.substring(0, index);
            msg = msg.substring(index + 1);
            index = msg.indexOf("#");
            this.Msg_Text = msg.substring(index);
        } else if (this.Msg_Type == chat.MSG_TYPE.b2c_GlobalMsg) {
            //推送全局消息
            var index = msg.indexOf("#");
            this.Msg_SrcUID = msg.substring(0, index);
            this.Msg_Text = msg.substring(index + 1);
        } else if (this.Msg_Type == chat.MSG_TYPE.b2c_BroadUserOnLine
            || this.Msg_Type == chat.MSG_TYPE.b2c_BroadUserOffLine) {
            //用户上线、下线消息
            this.Msg_SrcUID = msg;
        } else if (this.Msg_Type == chat.MSG_TYPE.b2c_Error
            || this.Msg_Type == chat.MSG_TYPE.b2c_UserTree
            || this.Msg_Type == chat.MSG_TYPE.b2c_History) {
            //推送报错消息、用户树、历史消息
            this.Msg_Text = msg;
        }
    }
    chat.options = {
        url: "ws://" + window.location.hostname + ":4649/Chat",
        userInfo: null
    };
    //用户树
    chat.userTree = [];
    //用户树{user.ID0:user}
    chat.userTree_flat = {};
    chat.init = function (arg) {
        $.extend(chat.options, arg);
    }
    chat.login = function (username, password) {
        //var index = layer.load(1, {
        //    shade: [0.3, '#000'] //0.1透明度的白色背景
        //});
        var def = $.Deferred();
        chat.options.userInfo = {
            LoginName: username
        };
        var socket = chat.socket = new WebSocket(chat.options.url + "?user=" + username + "&pass=" + password);
        socket.onopen = function (res) {
            socket.onmessage = chat.onmessage;
            socket.onclose = chat.onclose;
            socket.onerror = chat.onerror;
        };
        return def;
    }
    chat.onmessage = function (res) {
        var msg = new chat.Msg(res.data);
        console.log(msg);
        //接收到广播自己的上线消息,认为上线成功
        if (msg.Msg_Type == chat.MSG_TYPE.b2c_BroadUserOnLine) {
            if (msg.Msg_SrcUID == chat.options.userInfo.LoginName) {
                //成功上线
                if (chat.hasLogin) {
                    //当前用户的其他客户端登录 不作处理
                    return;
                }
                chat.hasLogin = true;
                chat.socket.send(chat.MSG_TYPE.c2b_UserTree + "#");
                return;
            }
        }
        //还未登录时不接收任何消息
        if (!chat.hasLogin) return;
        if (msg.Msg_Type == chat.MSG_TYPE.b2c_UserTree) {
            //获取到了用户树
            chat.userTree = JSON.parse(msg.Msg_Text);
            $("#login").hide("fast");
            console.log(chat.userTree);
            fillUsers(chat.userTree);
            $("#userwin").show("fast");
        } else if (msg.Msg_Type == chat.MSG_TYPE.b2c_BroadUserOnLine) {
            //其他人上线
            $("#tabUser .user").each(function (index, item) {
                var user = $(this).data("data");
                if (user.LoginName == msg.Msg_SrcUID) {
                    if (user.isOnline) {
                        //用户已经在线，这是其他设备在登录
                        return false;
                    }
                    //记录当前用户已经上线
                    user.isOnline = true;
                    //设置头像透明度
                    $(item).find("img").css("opacity", "1");
                    var count_ele = $(item).parent().parent().find(".user-group-line>.user-group-count");
                    //更新分组上线用户计数
                    if (count_ele) {
                        var str = count_ele.html();
                        var arr = str.substring(1, str.length - 1).split("/");
                        count_ele.html("(" + (window.parseInt(arr[0]) + 1) + "/" + arr[1] + ")");
                    }
                    //将这个用户向上移动
                    var ul = $(item).parent();
                    var lis = ul.find(">li");
                    for (var i = 0; i < lis.length; i++) {
                        if (!lis.eq(i).data("data").isOnline) {
                            //找到一个不在线的 移动到这个前面
                            $(item).insertBefore(lis.eq(i));
                            break;
                        }
                    }
                    //修改对话窗口的好友状态
                    if (chat.msgContext["user_" + user.ID0] && chat.msgContext["user_" + user.ID0].dialog) {
                        var dialog = chat.msgContext["user_" + user.ID0].dialog;
                        var title = dialog.find(".talk-header>.talk-title");
                        title.css("opacity", 1);
                        title.find(".destUserName").html(title.find(".destUserName").html().replace("(离线)", ""));
                    }
                    return false;
                }
            })
        } else if (msg.Msg_Type == chat.MSG_TYPE.b2c_BroadUserOffLine) {
            //其他人下线
            $("#tabUser .user").each(function (index, item) {
                var user = $(this).data("data");
                if (user.ID0 == msg.Msg_SrcUID) {
                    //设置头像透明度
                    $(item).find("img").css("opacity", "0.3");
                    var count_ele = $(item).parent().parent().find(".user-group-line>.user-group-count");
                    //更新分组上线用户计数
                    if (count_ele) {
                        var str = count_ele.html();
                        var arr = str.substring(1, str.length - 1).split("/");
                        count_ele.html("(" + (window.parseInt(arr[0]) - 1) + "/" + arr[1] + ")");
                    }
                    //将这个用户直接移动到最后
                    $(item).parent().append($(item));
                    //将用户标记为下线
                    user.isOnline = false;
                    //修改对话窗口的好友状态
                    if (chat.msgContext["user_" + user.ID0] && chat.msgContext["user_" + user.ID0].dialog) {
                        var dialog = chat.msgContext["user_" + user.ID0].dialog;
                        var title = dialog.find(".talk-header>.talk-title");
                        title.css("opacity", 0.5);
                        title.find(".destUserName").html(title.find(".destUserName").html() + "(离线)");
                    }
                    return false;
                }
            })
        } else if (msg.Msg_Type == chat.MSG_TYPE.b2c_UserMsg) {
            chat.dealUserMsg(msg);
        }
    }
    //根据返回的用户树填充用户列表
    function fillUsers(userTree) {
        var container = $("#tabUser>ul.user-container");
        container.html("");
        _fillUsers(container, userTree);
        //绑定事件 分组展开事件
        $("#tabUser").delegate(".user-group-line", "click", function () {
            var icon_ele = $(this).find(">.layui-icon");
            var isclose = icon_ele.hasClass("layui-icon-right");
            if (isclose) {
                $(this).parent().find(">ul").show();
                icon_ele.removeClass("layui-icon-right").addClass("layui-icon-down");
            } else {
                $(this).parent().find(">ul").hide();
                icon_ele.removeClass("layui-icon-down").addClass("layui-icon-right");
            }
        });
        //绑定点击用户事件
        $("#tabUser").delegate("li.user", "click", function () {
            var user = $(this).data("data");
            if (user.ID0 == chat.options.userInfo.ID0) {
                //自己不能向自己会话
                return;
            }
            //隐藏掉未读消息显示
            $(this).find(".user-news-count").hide();
            //取消闪烁
            $(this).removeClass("animated infinite flash");
            initMsgContext(user);
            var dialog = chat.msgContext["user_" + user.ID0].dialog;
            if (!dialog) {
                //还没有打开过对话框
                dialog = chat.msgContext["user_" + user.ID0].dialog = $(".talk-win.for-clone").clone().appendTo(document.body).data("data", user).removeClass("for-clone");
                var hei = dialog.height();
                var wid = dialog.width();
                var screenHei = window.innerHeight;
                var screenWid = window.innerWidth;
                var top = (screenHei - hei) / 2;
                var left = (screenWid - wid) / 2;
                dialog.css({
                    "left": left + "px",
                    "top": top + "px"
                });
                dialog.find(".destUserImg").attr("src", user.UImg || "img/user1.png");
                dialog.find(".destUserName").html(user.UserName);
                dialog.find(".destUserDesc").html(user.UDesc);
                if (!user.isOnline) {
                    dialog.find(".talk-title").css({
                        "opacity": "0.5"
                    });
                }
                //给对话框绑定关闭事件
                dialog.find(".talk-header>.talk-win-handle>a.icon-close,.talk-edit>.talk-op>.talk-btns>.talk-btns-close").click(function () {
                    $(this).parents(".talk-win").hide();
                });
                //给对话框绑定发送事件
                dialog.find(".talk-edit>.talk-op>.talk-btns>.talk-btns-send").click(function () {
                    var dialog = $(this).parents(".talk-win");
                    var user = dialog.data("data");
                    var text = dialog.find(".talk-edit textarea").val();
                    if ($.trim(text) == "") {
                        layer.msg('不能发送空消息!');
                        return;
                    }
                    chat.sendToUser(user, text);
                });
                //给对话框绑定移动事件
                dialog.find(".talk-header").mousedown(function (evt) {
                    var startx = evt.pageX;
                    var starty = evt.pageY;
                    var moveFunc = function (evt) {
                        var startx2 = evt.pageX;
                        var starty2 = evt.pageY;
                        var dx = startx2 - startx;
                        var dy = starty2 - starty;
                        var top = parseInt(dialog.css("top")) + dy
                        var left = parseInt(dialog.css("left")) + dx;
                        startx = startx2;
                        starty = starty2;
                        if (top < 0 || left < 0) {
                            //防止对话框移出
                            return;
                        }
                        dialog.css({
                            "top": top,
                            "left": left
                        })
                    }
                    var upFunc = function () {
                        $(document).off('mousemove', moveFunc).off('mouseup', upFunc);
                    }
                    $(document).mousemove(moveFunc);
                    $(document).mouseup(upFunc);
                });
                //给输入框绑定回车和监测代码
                dialog.find(".talk-edit textarea").keydown(function (evt) {
                    if (event.keyCode == 13) {
                        if (evt.shiftKey || evt.ctrlKey) {
                            //如果同时按下shift键表示换行
                            $(this).val($(this).val() + "\r\n");
                        } else {
                            dialog.find(".talk-edit>.talk-op>.talk-btns>.talk-btns-send").click();
                        }
                        evt.preventDefault();
                    }
                });
            }
            dialog.show();
            fillMsg2Chat(user);
        })
        //填充当前登录人信息
        $("#currUserName").html(chat.options.userInfo.UserName);
        $("#currUserDesc").val(chat.options.userInfo.UDesc);
        $("#currUserImg").attr("src",(chat.options.userInfo.UImg || "img/user1.png"));
    }
    //给指定的用户初始化对话上下文
    function initMsgContext(user) {
        chat.msgContext["user_" + user.ID0] = chat.msgContext["user_" + user.ID0] || { dialog: null, user: user, msgs: [] };
    }
    function _fillUsers(container, arr) {
        if (arr.length > 0) {
            for (var i = 0; i < arr.length; i++) {
                var obj = arr[i];
                if (obj.LoginName) {
                    //是用户
                    var li = $('<li class="user" />').data("data", obj).appendTo(container);
                    var img = $('<img />').attr("src", obj.UImg || "img/user1.png").appendTo(li);
                    if (!obj.isOnline) {
                        img.css("opacity", "0.3");
                    }
                    var name = $('<span />').text(obj.UserName).appendTo(li);
                    var desc = $('<p />').text(obj.UDesc).appendTo(li);
                    var count = $('<span class="user-news-count" />').text(obj.UDesc).appendTo(li);
                    if (obj.LoginName == chat.options.userInfo.LoginName) {
                        //是自己
                        chat.options.userInfo = obj;
                        li.css("background-color", "#9ff3c4");
                    }
                    chat.userTree_flat[obj.ID0] = obj;
                } else {
                    //是分组
                    var li = $('<li class="user-group-container" />').appendTo(container);
                    var div = $('<div class="user-group-line" />').appendTo(li).data("data", obj);
                    var i_ele = $('<i class="layui-icon layui-icon-right" />').appendTo(div);
                    var text = $('<span class="user-group-text" />').text(obj.Name).appendTo(div);
                    if (obj.Children.length == 0) {
                        var onlineCount = 0;
                        for (var j in obj.Users) {
                            if (obj.Users[j].isOnline) {
                                onlineCount++;
                            }
                        }
                        var count = $('<span class="user-group-count" />').text("(" + onlineCount + "/" + obj.Users.length + ")").appendTo(div);
                    }
                    var ul = $('<ul />').appendTo(li);
                    if (obj.Children.length > 0) {
                        _fillUsers(ul, obj.Children);
                    }
                    if (obj.Users.length > 0) {
                        //上线的用户排序靠前
                        if (obj.Users.length > 0) {
                            var users = [];
                            for (var j = obj.Users.length - 1 ; j >= 0; j--) {
                                if (obj.Users[j].isOnline) {
                                    users.push(obj.Users[j]);
                                    obj.Users.splice(j, 1);
                                }
                            }
                            users = users.concat(obj.Users);
                            obj.Users = users;
                        }
                        _fillUsers(ul, obj.Users);
                    }
                }
            }
        }
    }
    chat.dealError = function () {

    }
    chat.dialogContext = {};
    //给个人发送消息
    chat.sendToUser = function (user, msg) {
        chat.socket.send(chat.MSG_TYPE.c2b_UserMsg + "#" + user.ID0 + "#" + msg);
    }
    //处理接收的个人消息
    chat.dealUserMsg = function (msg) {
        console.log("个人消息:", msg);
        var msgContex;
        var destUser = chat.userTree_flat[msg.Msg_SrcUID];
        initMsgContext(destUser);
        var context = chat.msgContext["user_" + msg.Msg_SrcUID];
        context.msgs.push(msg);
        fillMsg2Chat(destUser);
    }

    //将未读的消息写入对话框
    function fillMsg2Chat(user) {
        var msgContext = chat.msgContext["user_" + user.ID0];
        var dialog = msgContext.dialog;
        var msgs = msgContext.msgs;
        if (dialog && dialog.is(":visible")) {
            //已经打开了这个人的对话窗口并且显示
            for (var i in msgs) {
                if (msgs[i].hasDisplay) continue;
                var msg = msgs[i];
                var timestr = msg.MsgTime.substring(0, msg.MsgTime.indexOf("."));
                if (msg.Msg_Feedback) {
                    //反馈的消息要清空输入框
                    var edit = dialog.find(".talk-text>textarea");
                    if (msg.Msg_Text == edit.val()) {
                        edit.val("");
                    }
                }
                //写入显示列表
                var content = dialog.find(".talk-content");
                var ul = content.find(">ul");
                var li = $("<li class='talk-msg " + (msg.Msg_Feedback ? "talk-mine" : "") + "' />").appendTo(ul);
                var user2 = $('<div class="talk-msg-user" />').appendTo(li);
                var img = $('<img src="' + (user.UImg || 'img/user1.png') + '" />').appendTo(user2);
                if (msg.Msg_Feedback) {
                    var text = $('<cite><i>' + timestr + '</i>' + chat.options.userInfo.UserName + '</cite>').appendTo(user2);
                } else {
                    var text = $('<cite>' + user.UserName + '<i>' + timestr + '</i></cite>').appendTo(user2);
                }
                var str = msg.Msg_Text.replace(/\n/g, "<br />");
                var msg2 = $('<div class="talk-msg-text">' + str + '</div>').appendTo(li);
                content.scrollTop(ul.height());
                //表示已经展示给了用户
                msg.hasDisplay = true;
            }
        } else {
            //还未打开对话窗口,更新提示面板
            //找到未读消息的数量
            var count = 0;
            for (var i = msgs.length - 1; i >= 0; i--) {
                if (!msgs[i].hasDisplay) {
                    count++;
                } else {
                    break;
                }
            }
            if (count >= 100) count = 99;
            //更新提示
            $("#tabUser .user").each(function (index, item) {
                var user2 = $(this).data("data");
                if (user2.ID0 == user.ID0) {
                    if (count > 0) $(this).find("span.user-news-count").html(count).show();
                    $(this).addClass('animated infinite flash');
                    return false;
                }
            })
        }
    }

})('chat', $, layui);