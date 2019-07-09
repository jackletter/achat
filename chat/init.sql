--drop table CHAT_USER
create table CHAT_USER(
	ID0 int primary key,
	LOGINNAME varchar(200),--用户登录名
	PWD varchar(200),--用户密码
	UNAME varchar(200),--用户昵称
	USEX varchar(10),--用户性别(男|女)
	UAGE int,--用户年龄
	UIMG varchar(500),--用户头像
	UDES varchar(500),--用户个性签名
	ULOCATE varchar(50),--用户所属行政区划代码
	ULOCATENAME varchar(200),--用户所属行政区划名称
	CREATETIME datetime--用户注册时间
);
insert into CHAT_USER(ID0,LOGINNAME,PWD,UNAME,USEX,UAGE,UIMG,UDES,ULOCATE,ULOCATENAME,CREATETIME) VALUES(1,'test1','1','测试用户1','男',20,'img/user1.png','默认个性签名','110000','北京','2018-12-18');
insert into CHAT_USER(ID0,LOGINNAME,PWD,UNAME,USEX,UAGE,UIMG,UDES,ULOCATE,ULOCATENAME,CREATETIME) VALUES(2,'test2','1','测试用户2','男',20,'img/user1.png','默认个性签名','110000','北京','2018-12-18');
insert into CHAT_USER(ID0,LOGINNAME,PWD,UNAME,USEX,UAGE,UIMG,UDES,ULOCATE,ULOCATENAME,CREATETIME) VALUES(3,'test3','1','测试用户3','男',20,'img/user1.png','默认个性签名','110000','北京','2018-12-18');
insert into CHAT_USER(ID0,LOGINNAME,PWD,UNAME,USEX,UAGE,UIMG,UDES,ULOCATE,ULOCATENAME,CREATETIME) VALUES(4,'test4','1','测试用户4','男',20,'img/user1.png','默认个性签名','110000','北京','2018-12-18');
insert into CHAT_USER(ID0,LOGINNAME,PWD,UNAME,USEX,UAGE,UIMG,UDES,ULOCATE,ULOCATENAME,CREATETIME) VALUES(5,'test5','1','测试用户5','男',20,'img/user1.png','默认个性签名','110000','北京','2018-12-18');

--drop table CHAT_GROUP
create table CHAT_GROUP(
	ID0 int primary key,
	GNAME varchar(200),--群名称
	GIMG varchar(200),--群图标
	GDESC varchar(500),--群描述
	GOWNER int,--群主ID
	GMANAGERS varchar(500),--群管理员
	CREATETIME datetime--群创建时间
);


insert into CHAT_GROUP(ID0,GNAME,GIMG,GOWNER,GMANAGERS,CREATETIME) values(1,'测试群1','img/user1.png',1,'2','2018-12-18');
insert into CHAT_GROUP(ID0,GNAME,GIMG,GOWNER,GMANAGERS,CREATETIME) values(2,'测试群2','img/user1.png',2,null,'2018-12-18');

create table CHAT_USER_GROUP(
	ID0 int primary key,
	USERID int,--用户ID
	GID int--组ID
)

insert into CHAT_USER_GROUP(ID0,USERID,GID) values(1,1,1);
insert into CHAT_USER_GROUP(ID0,USERID,GID) values(2,2,1);
insert into CHAT_USER_GROUP(ID0,USERID,GID) values(3,3,1);
insert into CHAT_USER_GROUP(ID0,USERID,GID) values(4,4,1);
insert into CHAT_USER_GROUP(ID0,USERID,GID) values(5,2,2);
insert into CHAT_USER_GROUP(ID0,USERID,GID) values(6,1,2);

create table CHAT_MSG_USER(
	ID0 int primary key,
	DESTUSERID int,
	FROMUSERID int,
	MSGTYPE int,
	MSGTEXT varchar(max),
	CREATETIME datetime
);
--drop table CHAT_MSG_GROUP
create table CHAT_MSG_GROUP(
	ID0 int primary key,
	DESTGID int,
	FROMUSERID int,
	MSGTYPE int,
	MSGTEXT varchar(max),
	CREATETIME datetime
);

--drop table CHAT_USER_RELATION
create table CHAT_USER_RELATION(
	ID0 int primary key,
	USERID1 int,--用户ID1,用户2是用户1的好友
	USERID2 int,--用户ID2,
	INNERGID int,--用户2在用户1的分组里
	UREMARK varchar(50),--用户1对用户2的昵称备注
	CREATETIME datetime--好友创建时间
);

insert into CHAT_USER_RELATION(ID0,USERID1,USERID2,INNERGID,CREATETIME) values(1,1,2,1,'2018-12-18');
insert into CHAT_USER_RELATION(ID0,USERID1,USERID2,INNERGID,CREATETIME) values(2,1,3,1,'2018-12-18');
insert into CHAT_USER_RELATION(ID0,USERID1,USERID2,INNERGID,CREATETIME) values(3,1,4,1,'2018-12-18');
insert into CHAT_USER_RELATION(ID0,USERID1,USERID2,INNERGID,CREATETIME) values(4,1,5,6,'2018-12-18');
insert into CHAT_USER_RELATION(ID0,USERID1,USERID2,INNERGID,CREATETIME) values(5,2,1,2,'2018-12-18');
insert into CHAT_USER_RELATION(ID0,USERID1,USERID2,INNERGID,CREATETIME) values(6,1,1,2,'2018-12-18');
insert into CHAT_USER_RELATION(ID0,USERID1,USERID2,INNERGID,CREATETIME) values(7,2,2,2,'2018-12-18');
insert into CHAT_USER_RELATION(ID0,USERID1,USERID2,INNERGID,CREATETIME) values(8,1,1,1,'2018-12-18');
insert into CHAT_USER_RELATION(ID0,USERID1,USERID2,INNERGID,CREATETIME) values(9,3,3,3,'2018-12-18');
insert into CHAT_USER_RELATION(ID0,USERID1,USERID2,INNERGID,CREATETIME) values(10,4,4,4,'2018-12-18');
insert into CHAT_USER_RELATION(ID0,USERID1,USERID2,INNERGID,CREATETIME) values(11,5,5,5,'2018-12-18');


create table CHAT_INNER_GROUP(
	ID0 int primary key,
	USERID int,
	GNAME varchar(200),
	GORDER int
);

insert into CHAT_INNER_GROUP(ID0,USERID,GNAME,GORDER) values(1,1,'我的好友',1);
insert into CHAT_INNER_GROUP(ID0,USERID,GNAME,GORDER) values(2,2,'我的好友',1);
insert into CHAT_INNER_GROUP(ID0,USERID,GNAME,GORDER) values(3,3,'我的好友',1);
insert into CHAT_INNER_GROUP(ID0,USERID,GNAME,GORDER) values(4,4,'我的好友',1);
insert into CHAT_INNER_GROUP(ID0,USERID,GNAME,GORDER) values(5,5,'我的好友',1);

insert into CHAT_INNER_GROUP(ID0,USERID,GNAME,GORDER) values(6,1,'测试组',2);


create table CHAT_USERADD(
	ID0 int primary key,
	FROMUSERID int,
	DESTUSERID int,
	MSGTEXT varchar(500),
	CREATETIME datetime,
	DEALTYPE int,--默认0,表示未阅读,1:表示同意,2:表示拒绝
	BACKMSGTEXT varchar(500)--拒绝后返回的消息
);

--drop proc PROC_GROUP_MSG_HISTORY
/*获取指定ID用户的群组消息,每个群组消息最多获取指定的数量*/
create proc PROC_GROUP_MSG_HISTORY
@userid int,@perlen int = 10
as
begin
	declare @gid int,@sql nvarchar(200)
	select * into #temp_msg_group from CHAT_MSG_GROUP where 1<0
	declare cur_gid cursor for select GID from CHAT_USER_GROUP where USERID=@userid
	--打开游标
	open cur_gid 
	--读取游标 
	fetch next from cur_gid into @gid
	while @@fetch_status=0 
	begin
		set @sql='insert into #temp_msg_group select top '+convert( varchar(3),@perlen) +' * from CHAT_MSG_GROUP where DESTGID='+convert( varchar(3),@gid)+' order by ID0 desc'	
		exec sp_executesql @sql
		fetch next from cur_gid into @gid
	end
	close cur_gid 
	deallocate cur_gid
	select * from #temp_msg_group order by DESTGID asc,ID0 asc
end
--exec PROC_GROUP_MSG_HISTORY 1,3

--drop proc PROC_USER_MSG_HISTORY 
/*获取指定ID用户的好友消息,每个好友消息最多获取指定的数量*/
create proc PROC_USER_MSG_HISTORY
@userid int,@perlen int = 10
as
begin
	declare @uid int,@sql nvarchar(200)
	select * into #temp_msg_user from CHAT_MSG_USER where 1<0
	declare cur_uid cursor for select USERID2 from CHAT_USER_RELATION where USERID1=@userid
	--打开游标
	open cur_uid 
	--读取游标 
	fetch next from cur_uid into @uid
	while @@fetch_status=0 
	begin
		set @sql='insert into #temp_msg_user select top '+convert( varchar(3),@perlen) +' * from CHAT_MSG_USER where (DESTUSERID='+convert( varchar(20),@userid)+' and FROMUSERID='+convert( varchar(20),@uid) +') or (FROMUSERID='+convert( varchar(20),@userid)+' and DESTUSERID='+convert( varchar(20),@uid) +') order by ID0 desc'		
		exec sp_executesql @sql
		fetch next from cur_uid into @uid
	end
	close cur_uid 
	deallocate cur_uid
	select * from #temp_msg_user
end
--exec PROC_USER_MSG_HISTORY 1,10