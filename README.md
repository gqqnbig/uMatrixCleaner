

<h1> <img src="https://github.com/gqqnbig/uMatrixCleaner/raw/master/uMatrixCleaner/icon_128.png" width="32" /> μMatrix规则清理器 </h1>

## 命令行参数

### --Help
显示帮助

### -Log x
x为字符串，要么为"d"，要么为文件路径。

保存XML日志。如果x="d"，则日志保存在*输入文件路径*的同级目录，文件名是uMatrix-_日期_.xml。如果x是其他值，则该值指定日志的完整路径。

如果本选项未出现，则禁用保存日志的功能。

### --MergeThreshold [x]
x为整数，默认值为3。

设置合并类似规则的阀值。

当阀值为3时，

    google.com facebook.com * block
    google.com youtube.com * block
    google.com twitter.com * block

可以合并为

    google.com * * block

。

当阀值为2时，

    google.com www.facebook.com script block
    google.com login.facebook.com script block

可以合并为

    google.com facebook.com script block

。

### --RandomDelete [x]
x为整数，默认值为5。

设置随机删除百分之x的规则。

如果本选项未出现，则禁用随机删除的功能。

### --CheckLog [s]
s为路径，默认值为*输入文件路径*的同级目录。

设置读取历史记录的文件夹。uMatrixCleaner将读入里面的`*.xml`文件。该功能的意义在于，如果一条规则以前被删除了，如今又出现了，则说明uMatrixCleaner不应该把它删除。

如果本选项未出现，则禁用检查历史记录的功能。

### --Verbose
在命令行中输出详细信息

### 位置参数
输入文件路径 [输出文件路径]
#### 输入文件路径
保存μMatrix规则的文件的路径
#### [输出文件路径]
可选。清理后的规则文件的路径。如果不指定，则保存在*输入文件路径*的同级目录，文件名是uMatrix-*日期*.txt。
