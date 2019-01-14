(function (_name, _$, _layerui) {
    var $ = _$;
    var layui = _layerui;
    var layer = layui.layer;
    var chat = window[_name] = {};
    //是否登录
    chat.hasLogin = false;
    //服务端推送所有消息管理
    chat.msgContext = {
        box_useradd: []
    };
    chat.options = {
        url: "ws://" + window.location.hostname + ":4649/Chat",
        userInfo: null
    };
    //用户树
    chat.userTree = [];
    //用户树{user.ID0:user}
    chat.userTree_flat = {};
    //群组树
    chat.groupTree_flat = {};
    chat.init = function (arg) {
        $.extend(chat.options, arg);
    }
    chat.login = function (username, password) {
        chat.login_shadow = layer.load(1, {
            shade: [0.3, '#000'] //0.1透明度的白色背景
        });
        var def = $.Deferred();
        chat.options.userInfo = {
            LoginName: username
        };
        var socket = chat.socket = new WebSocket(chat.options.url + "?user=" + username + "&pwd=" + password);
        socket.onopen = function (res) {
            socket.onmessage = chat.onmessage;
            socket.onclose = chat.onclose;
            socket.onerror = chat.onerror;
        };
        return def;
    }
    chat.MsgPack = function (data) {
        var index = data.indexOf("\r\n");
        var line = data.substring(0, index);
        this.type = line.split(" ")[0];
        this.path = line.split(" ")[1];
        data = data.substring(index + 2);
        index = data.indexOf("\r\n\r\n");
        var head;
        if (index > 0) {
            head = data.substring(0, index);
            var body = data.substring(index + 4);
            this.data = body;
            try {
                this.data_json = JSON.parse(body);
            } catch (ex) { }
        } else {
            head = data;
        }
        head = head.split("\r\n");
        this.headers = {};
        for (var i = 0; i < head.length; i++) {
            this.headers[head[i].split(":")[0]] = head[i].split(":")[1];
        }
    }
    chat.onmessage = function (res) {
        var pack = new chat.MsgPack(res.data);
        if (pack.type == "reply") {
            var guid = pack.headers["guid"];
            if (guid && typeof (chat._props[guid]) == "function") {
                chat._props[guid](pack);
            }
            return;
        }
        var func = chat._funcs[pack.path];
        if (typeof (func) == "function") {
            func(pack);
        }
        return;
        if (msg.Msg_Type == chat.MSG_TYPE.b2c_UserAdd) {
            //返回的好友添加请求
            chat.dealUserAdd(msg);
        }
    }
    //根据返回的用户树填充用户列表
    function fillUsers(userTree) {
        var container_user = $("#tabUser>ul.user-container");
        var container_group = $("#tabGroup>ul.user-container");
        _fillUsers(container_user, userTree.inner_groups);
        _fillGroups(container_group, userTree.groups);
        //绑定事件 好友分组展开事件
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
        //绑定点击好友事件
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
            initUserMsgContext(user);
            var dialog = chat.msgContext["user_" + user.ID0].dialog;
            if (!dialog) {
                //还没有打开过对话框
                dialog = chat.msgContext["user_" + user.ID0].dialog = $(".talk-win.talk-user.for-clone").clone().appendTo(document.body).data("data", user).removeClass("for-clone");
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
                dialog.find(".destUserImg").attr("src", user.UIMG || "img/user1.png");
                dialog.find(".destUserName").html(user.UNAME);
                dialog.find(".destUserDesc").html(user.UDESC);
                if (!user.IsOnline) {
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
            fillUserMsg2Chat(user);
        });
        //绑定点击群组事件
        $("#tabGroup").delegate("li.user", "click", function () {
            var group = $(this).data("data");
            //隐藏掉未读消息显示
            $(this).find(".user-news-count").hide();
            //取消闪烁
            $(this).removeClass("animated infinite flash");
            initGroupMsgContext(group);
            var dialog = chat.msgContext["group_" + group.ID0].dialog;
            if (!dialog) {
                //还没有打开过对话框
                dialog = chat.msgContext["group_" + group.ID0].dialog = $(".talk-win.talk-group.for-clone").clone().appendTo(document.body).data("data", group).removeClass("for-clone");
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
                dialog.find(".destUserImg").attr("src", group.GIMG || "img/user1.png");
                dialog.find(".destUserName").html(group.GNAME);
                dialog.find(".destUserDesc").html(group.GDESC);
                //添加显示群组的所有人员
                var ul = dialog.find(".talk-right>ul");
                var onlineCount = 0;
                for (var i = 0; i < group.Users.length; i++) {
                    var user = group.Users[i];
                    if (user.IsOnline) {
                        onlineCount++;
                    }
                    var li = $("<li />").appendTo(ul);
                    var img = $("<img />").attr("src", user.UIMG || "img/user1.png").appendTo(li);
                    if (!user.IsOnline) img.css("opacity", "0.5");
                    var span = $("<span />").html(user.UNAME).appendTo(li);
                    var span = $("<span />").html("(" + user.LOGINNAME + ")").appendTo(li);
                }
                dialog.find(".talk-right>div>span").html(onlineCount + "/" + group.Users.length);
                //给对话框绑定关闭事件
                dialog.find(".talk-header>.talk-win-handle>a.icon-close,.talk-edit>.talk-op>.talk-btns>.talk-btns-close").click(function () {
                    $(this).parents(".talk-win").hide();
                });
                //给对话框绑定发送事件
                dialog.find(".talk-edit>.talk-op>.talk-btns>.talk-btns-send").click(function () {
                    var dialog = $(this).parents(".talk-win");
                    var group = dialog.data("data");
                    var text = dialog.find(".talk-edit textarea").val();
                    if ($.trim(text) == "") {
                        layer.msg('不能发送空消息!');
                        return;
                    }
                    chat.sendToGroup(group, text);
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
            fillGroupMsg2Chat(group);
        });
        //填充当前登录人信息
        $("#currUserName").html(chat.options.userInfo.UNAME);
        $("#currUserDesc").val(chat.options.userInfo.UDESC);
        $("#currUserImg").attr("src", (chat.options.userInfo.UIMG || "img/user1.png"));
    }
    //给指定的用户初始化对话上下文
    function initUserMsgContext(user) {
        chat.msgContext["user_" + user.ID0] = chat.msgContext["user_" + user.ID0] || { dialog: null, user: user, msgs: [] };
    }
    //给指定的群组初始化对话上下文
    function initGroupMsgContext(group) {
        chat.msgContext["group_" + group.ID0] = chat.msgContext["group_" + group.ID0] || { dialog: null, group: group, msgs: [] };
    }
    function _fillGroups(container_groups, groups) {
        container_groups.html("");
        if (groups.length > 0) {
            for (var i = 0; i < groups.length; i++) {
                var group = groups[i];
                //是分组
                var li = $('<li class="user" />').data("data", group).appendTo(container_groups);
                var img = $('<img />').attr("src", group.GIMG || "img/user1.png").appendTo(li);
                var name = $('<span />').text(group.GNAME).appendTo(li);
                var desc = $('<p />').text(group.GDESC).appendTo(li);
                var count = $('<span class="user-news-count" />').text("").appendTo(li);
                chat.groupTree_flat[group.ID0] = group;
            }
        }
    }
    function _fillUsers(container_user, inner_groups) {
        container_user.html("");
        if (inner_groups.length > 0) {
            for (var i = 0; i < inner_groups.length; i++) {
                var group = inner_groups[i];
                //是分组
                var li = $('<li class="user-group-container" />').appendTo(container_user);
                var div = $('<div class="user-group-line" />').appendTo(li).data("data", group);
                var i_ele = $('<i class="layui-icon layui-icon-right" />').appendTo(div);
                var text = $('<span class="user-group-text" />').text(group.GNAME).appendTo(div);
                var onlineCount = 0;
                for (var j in group.Users) {
                    if (group.Users[j].IsOnline) {
                        onlineCount++;
                    }
                }
                var count = $('<span class="user-group-count" />').text("(" + onlineCount + "/" + group.Users.length + ")").appendTo(div);
                var ul = $('<ul />').appendTo(li);
                if (group.Users.length > 0) {
                    //上线的用户排序靠前
                    var users = [];
                    for (var j = group.Users.length - 1; j >= 0; j--) {
                        if (group.Users[j].IsOnline) {
                            users.push(group.Users[j]);
                            group.Users.splice(j, 1);
                        }
                    }
                    users = users.concat(group.Users);
                    group.Users = users;
                    var len = group.Users.length;
                    for (var j = 0; j < len; j++) {
                        //是用户
                        var user = group.Users[j];
                        var li = $('<li class="user" />').data("data", user).appendTo(ul);
                        var img = $('<img />').attr("src", user.UIMG || "img/user1.png").appendTo(li);
                        if (!user.IsOnline) {
                            img.css("opacity", "0.3");
                        }
                        var name = $('<span />').text(user.UNAME).appendTo(li);
                        var desc = $('<p />').text(user.UDESC).appendTo(li);
                        var count = $('<span class="user-news-count" />').text(user.UDESC).appendTo(li);
                        if (user.LOGINNAME == chat.options.userInfo.LoginName) {
                            //是自己
                            chat.options.userInfo = user;
                            li.css("background-color", "#9ff3c4");
                        }
                        chat.userTree_flat[user.ID0] = user;
                    }
                }
            }
        }
    }
    chat.dealError = function () {

    }
    chat.dialogContext = {};
    //给个人发送消息
    chat.sendToUser = async function (user, msg) {
        var packRes = await chat.send("/msg2User", null, JSON.stringify({ destUID: user.ID0, msg: msg }));
        if (!packRes.data_json.Success) {
            layer.msg(packRes.data_json.Data);
        }
    }
    //给群组发送消息
    chat.sendToGroup = async function (group, msg) {
        var packRes = await chat.send("/msg2Group", null, JSON.stringify({ destGID: group.ID0, msg: msg }));
        if (!packRes.data_json.Success) {
            layer.msg(packRes.data_json.Data);
        }
    }
    //发送添加好友请求
    chat.sendToAddUser = function (user, msg) {
        chat.send("/addUser", null, JSON.stringify({ destUID: user.ID0, msg: msg }));
    }
    //发送查询消息
    chat.sendToSearch = async function (txt, type) {
        var packRes = await chat.send("/search", null, JSON.stringify({ type, msg: txt }));
        if (!packRes.data_json.Success) {
            layer.msg(packRes.data_json.Data);
            return;
        }
        var win = $(".userfind");
        var ul = win.find(".userfind-user>.userfind-result>ul");
        ul.empty();
        var res = packRes.data_json.Data;
        for (var i = 0; i < res.length; i++) {
            var li = $("<li />").appendTo(ul);
            var img = $("<img />").attr("src", res[i].UIMG || "img/user1.png").appendTo(li);
            var span = $("<span />").html(res[i].UNAME).appendTo(li);
            var button = $("<button class='layui-btn layui-btn-xs' />").html("添加").appendTo(li).data("data", res[i]);
            button.click(function () {
                //添加好友
                var user = $(this).data("data");
                var _this = this;
                layer.prompt({ title: '请输入验证信息', formType: 2 }, function (msg, index) {
                    layer.close(index);
                    chat.sendToAddUser(user, msg);
                    $(_this).html("已发送").off("click").addClass("layui-btn-disabled");
                });
            });
        }
    }
    //发送接收好友添加请求
    chat.sendAcceptUserAdd = function (uid, remark) {
        chat.send("/agreeUserAdd", null, { uid, remark });
    }
    //发送拒绝好友添加请求
    chat.sendRejectUserAdd = function (msg, reason) {
        chat.socket.send(chat.MSG_TYPE.c2b_AcceptUserAdd + "#" + msg.ID0 + "#" + reason);
    }
    //打开添加用户窗口
    function bottomWinUser() {
        var win = $(".userfind");
        if (!chat._isInitAddUser) {
            //窗口居中
            var wid = win.width();
            var hei = win.height();
            win.css({
                top: (document.body.clientHeight - hei) / 2 + "px"
            });
            win.css({
                left: (document.body.clientWidth - wid) / 2 + "px"
            });
            //注册关闭按钮事件
            win.find(".talk-win-handle .icon-close").click(function () {
                win.hide();
            });
            //注册搜索事件
            var serachBtn = win.find("#userfindSearch");
            serachBtn.click(searchFunc);
            win.find("#userfindText").keydown(function (evt) {
                if (evt.keyCode == 13) {
                    serachBtn.click();
                }
            })
            chat._isInitAddUser = true;
        }
        //初始化搜索面板
        win.find("#userfindText").val("");
        win.find(".userfind-result>ul").empty();
        //注册搜索类型切换事件
        win.find(".userfind-dest>div").click(function () {
            win.find(".userfind-dest>div").removeClass("active");
            $(this).addClass("active");
        })
        win.show();
        function searchFunc() {
            var dest = win.find(".userfind-dest>div.active").attr("data-kind");
            var txt = win.find("#userfindText").val();
            var type = "user";
            if (dest == "user") {
                type = "user";
            } else if (dest == "group") {
                type = "group";
            }
            chat.sendToSearch(txt, type);
        }
    }
    //打开消息盒子
    function bottomWinMsg() {
        var win = $(".msgbox");
        if (!chat._isInitMsgBox) {
            //窗口居中
            var wid = win.width();
            var hei = win.height();
            win.css({
                top: (document.body.clientHeight - hei) / 2 + "px"
            });
            win.css({
                left: (document.body.clientWidth - wid) / 2 + "px"
            });
            //注册关闭按钮事件
            win.find(".talk-win-handle .icon-close").click(function () {
                win.hide();
            });
            chat._isInitMsgBox = true;
        }
        var msgs = chat.msgContext.box_useradd;
        var ul = $(".msgbox .msg-container>ul");
        ul.empty();
        for (var i = 0; i < msgs.length; i++) {
            var user = chat.userTree_flat[msgs[i].FROMUSERID];
            var li = $("<li />").appendTo(ul);
            var a = $("<a href='#' />").appendTo(li);
            var img = $("<img />").attr("src", user.UIMG || "img/user1.png").appendTo(a);
            var p = $('<p class="msg-text" />').appendTo(li);
            $('<span />').html(user.UNAME).appendTo(p);
            $('<span />').html(msgs[i].CREATETIME.split(" ")[0]).appendTo(p);
            p = $('<p class="msg-text" />').appendTo(li);
            $('<span>申请添加你为好友</span>').appendTo(p);
            $('<span></span>').html("附言:" + msgs[i].MSGTEXT).appendTo(p);
            p = $('<p class="msg-btns" />').appendTo(li);
            var btn_sure = $('<button class="layui-btn layui-btn-small">同意</button>').data("data", msgs[i]).appendTo(p);
            btn_sure.click(function () {
                var data = $(this).data("data");
                layer.prompt({ title: '请输入备注', formType: 0 }, function (msg, index) {
                    layer.close(index);
                    console.log(data, msg);
                    chat.sendAcceptUserAdd(data.FROMUSERID, msg);
                });
            });
            var btn_reject = $('<button class="layui-btn layui-btn-small layui-btn-primary">拒绝</button>').data("data", msgs[i]).appendTo(p);
            btn_reject.click(function () {
                var msg = $(this).data("data");
                chat.sendRejectUserAdd(msg, "默认拒绝了");
            })
        }
        win.show();
    }
    //处理好友添加请求
    chat.dealUserAdd = function (msg) {
        var dom = $("#userwin .win-bottom .msg-count");
        var count = parseInt(dom.html() || "0") + 1;
        dom.html(count);
        if (!dom.hasClass("flash")) {
            dom.show().addClass("animated infinite flash");
        }
    }
    //将未读的个人消息写入对话框
    function fillUserMsg2Chat(user) {
        var msgContext = chat.msgContext["user_" + user.ID0];
        var dialog = msgContext.dialog;
        var msgs = msgContext.msgs;
        if (dialog && dialog.is(":visible")) {
            //已经打开了这个人的对话窗口并且显示
            for (var i in msgs) {
                if (msgs[i].hasDisplay) continue;
                var msg = msgs[i];
                var timestr = msg.Msg_Time.split(".")[0];
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
                var img = $('<img src="' + (user.UIMG || 'img/user1.png') + '" />').appendTo(user2);
                if (msg.Msg_Feedback) {
                    var text = $('<cite><i>' + timestr + '</i>' + chat.options.userInfo.UNAME + '</cite>').appendTo(user2);
                } else {
                    var text = $('<cite>' + user.UNAME + '<i>' + timestr + '</i></cite>').appendTo(user2);
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
    //将未读的群组消息写入对话框
    function fillGroupMsg2Chat(group) {
        var msgContext = chat.msgContext["group_" + group.ID0];
        var dialog = msgContext.dialog;
        var msgs = msgContext.msgs;
        if (dialog && dialog.is(":visible")) {
            //已经打开了这个群组的对话窗口并且显示
            for (var i in msgs) {
                if (msgs[i].hasDisplay) continue;
                var msg = msgs[i];
                var fromUser = chat.userTree_flat[msg.Msg_SrcUID];
                var timestr = msg.Msg_Time.split(".")[0];
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
                var group2 = $('<div class="talk-msg-user" />').appendTo(li);
                var img = $('<img src="' + (fromUser.UIMG || 'img/user1.png') + '" />').appendTo(group2);
                if (msg.Msg_Feedback) {
                    var text = $('<cite><i>' + timestr + '</i>' + chat.options.userInfo.UNAME + '</cite>').appendTo(group2);
                } else {
                    var text = $('<cite>' + fromUser.UNAME + '<i>' + timestr + '</i></cite>').appendTo(group2);
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
            $("#tabGroup .user").each(function (index, item) {
                var group2 = $(this).data("data");
                if (group2.ID0 == group.ID0) {
                    if (count > 0) $(this).find("span.user-news-count").html(count).show();
                    $(this).addClass('animated infinite flash');
                    return false;
                }
            })
        }
    }
    chat._funcs = {};
    //注册后台消息的接收函数
    chat.accept = function (path, func) {
        chat._funcs[path] = func;
    }
    chat.accept("/BroadUserOnLine", async function (pack) {
        var loginname = pack.data_json.loginname;
        if (loginname == chat.options.userInfo.LoginName) {
            //自己成功上线
            if (chat.hasLogin) {
                //当前用户的其他客户端登录 不作处理
                return;
            }
            chat.hasLogin = true;
            layer.close(chat.login_shadow);
            //发送获取用户树消息
            var packRes = await chat.send("/getUserTree");
            chat.userTree = packRes.data_json.Data;
            $("#login").hide("fast");
            fillUsers(chat.userTree);
            $("#userwin").show("fast");
            //注册页面底部按钮事件
            $("#userwin .win-bottom .bottom-module").click(function () {
                var databtn = $(this).attr("data-btn");
                if (databtn == "adduser") {
                    //添加好友
                    bottomWinUser();
                } else if (databtn == "msgbox") {
                    //消息盒子
                    bottomWinMsg();
                }
            });
            //请求历史消息
            var packRes = await chat.send("/msgHistory");
            var msgObj = packRes.data_json.Data;
            if (msgObj.useradd.length > 0) {
                //有未读的好友添加请求
                var dom = $("#userwin .win-bottom .msg-count");
                var count = parseInt(dom.html() || "0") + msgObj.useradd.length;
                dom.html(count);
                if (!dom.hasClass("flash")) {
                    dom.show().addClass("animated infinite flash");
                }
                chat.msgContext["box_useradd"] = msgObj.useradd;
            }
            var user = msgObj.user;
            for (var i in user) {
                if (i.indexOf("user_") == 0) {
                    //好友消息
                    var msgs = user[i];
                    if (msgs && msgs.length > 0) {
                        var uid = i.substr(5);
                        var destUser = chat.userTree_flat[uid];
                        initUserMsgContext(destUser);
                        var context = chat.msgContext["user_" + uid];
                        for (var j = msgs.length - 1; j >= 0; j--) {
                            var tmp = {
                                Msg_Time: msgs[j].CREATETIME,
                                Msg_Feedback: msgs[j].FROMUSERID == chat.options.userInfo.ID0,
                                Msg_Text: msgs[j].MSGTEXT
                            };
                            context.msgs.push(tmp);
                        }
                        fillUserMsg2Chat(destUser);
                    }
                }
            }
            var group = msgObj.group;
            for (var i in group) {
                if (i.indexOf("group_") == 0) {
                    //群组消息
                    var msgs = group[i];
                    if (msgs && msgs.length > 0) {
                        var gid = i.substr(6);
                        var destGroup = chat.groupTree_flat[gid];
                        initGroupMsgContext(destGroup);
                        var context = chat.msgContext["group_" + gid];
                        for (var j = 0; j < msgs.length; j++) {
                            var tmp = {
                                Msg_SrcUID: msgs[j].FROMUSERID,
                                Msg_Time: msgs[j].CREATETIME,
                                Msg_Feedback: msgs[j].FROMUSERID == chat.options.userInfo.ID0,
                                Msg_Text: msgs[j].MSGTEXT
                            };
                            context.msgs.push(tmp);
                        }
                        fillGroupMsg2Chat(destGroup);
                    }
                }
            }
            return;
        } else {
            //其他人上线
            $("#tabUser .user").each(function (index, item) {
                var user = $(this).data("data");
                if (user.LOGINNAME == loginname) {
                    if (user.IsOnline) {
                        //用户已经在线，这是其他设备在登录
                        return false;
                    }
                    //记录当前用户已经上线
                    user.IsOnline = true;
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
                        if (!lis.eq(i).data("data").IsOnline) {
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
        }
    });
    chat.accept("/BroadUserOffLine", function (pack) {
        var uid = pack.data_json.uid;
        $("#tabUser .user").each(function (index, item) {
            var user = $(this).data("data");
            if (user.ID0 == uid) {
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
                user.IsOnline = false;
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
    });
    chat.accept("/msg2User", function (pack) {
        var otheruserid = pack.data_json.from;
        if (pack.data_json.from == chat.options.userInfo.ID0) {
            //反馈的消息
            otheruserid = pack.data_json.to;
            pack.data_json.Msg_Feedback = true;
        }
        var destUser = chat.userTree_flat[otheruserid];
        initUserMsgContext(destUser);
        var context = chat.msgContext["user_" + otheruserid];
        context.msgs.push(pack.data_json);
        fillUserMsg2Chat(destUser);
    });
    chat.accept("/msg2Group", function (pack) {
        var destGroup = chat.groupTree_flat[pack.data_json.to];
        initGroupMsgContext(destGroup);
        var context = chat.msgContext["group_" + pack.data_json.to];
        pack.data_json.Msg_SrcUID = pack.data_json.from;
        if (pack.data_json.from == chat.options.userInfo.ID0) {
            //反馈的消息
            pack.data_json.Msg_Feedback = true;
        }
        context.msgs.push(pack.data_json);
        fillGroupMsg2Chat(destGroup);
    });
    var _msguid = 1;
    //向后台发送消息
    chat.send = function (path, headers, msg) {
        var guid = _msguid++;
        var data = `request ${path}
guid:${guid}
`;
        if (headers && headers.length > 0) {
            for (var i = 0; i < header.length; i++) {
                data += headers[i].key + ":" + headers[i].value + "\r\n";
            }
        }
        if (msg) {
            data += "\r\n\r\n" + msg;
        }
        chat.socket.send(data);
        return new Promise(createReply(guid));
    }
    chat._props = {};
    function createReply(guid) {
        return function (res, rej) {
            chat._props[guid] = function (pack) {
                res(pack);
            };
            return chat._props[guid]
        };
    }
})('chat', $, layui);