# 游戏引擎原理和实践第11次作业
### 作业提交说明
本次作业因为工程比较大就没上传工程文件，因为作业要求里也只要求了可执行文件和视频，所以应该也没问题吧。
作业因为大小原因，github支持很差所以都放在百度网盘上了，分享链接如下：
链接：https://pan.baidu.com/s/1kNOtDld3CF6-JS9S9Ec79Q 
提取码：grzf 

### 游戏玩法
1. 实现了一个带网络同步的游戏。
1. 游戏内玩家操控角色在地图内跑来跑去，使用鼠标左键点击地面让角色去到对应的地方。
1. 按W键时鼠标右键可以释放一个跳斩的技能。
1. 按Q键可以普通攻击身旁的敌人。
1. 游戏目的是为了保护身旁的主基地，在地图中会不断有怪物刷新出来攻击主基地。（目前游戏一定会lose的，不要在意。。）
2. 游戏内制作了一些UI和小地图，但还不完善。

### 网络和bug
1. 使用帧同步的CS方案。
2. 实现了一个玩家加入房间一起开局的效果，进入游戏后游戏内的角色移动同步正常。
3. 但是目前很多角色的数据会有异常显示的bug，时间原因来不及修复了，以下举例说明几个。
4. 例如角色的皮肤选择，有时候会出现两个角色共用一个的情况。
5. 角色的技能显示在不同客户端之间会有错误。
6. 怪物的显示目前只能在host端看到。