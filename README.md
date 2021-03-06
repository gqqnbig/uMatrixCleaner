

<h1> <img src="https://github.com/gqqnbig/uMatrixCleaner/raw/master/uMatrixCleaner/icon_128.png" width="32" /> μMatrix规则清理器 </h1>

## 命令行参数
用中括号包起来的元素为可选项。如`--MergeThreshold [x]`表示可以输入`--MergeThreshold 4`或`--MergeThreshold`。


### --Help
显示帮助

### --Log
保存XML日志。如果本选项后带有参数"d"，则日志保存在输入文件路径的同级目录，文件名是uMatrix-_日期_.xml。如果参数是其他值，则该值指定日志的完整路径。

### --CheckLog [folder]
检查目录，读取里面的xml日志文件。如果一条记录以前被删除过，但现在又出现了，说明该规则是不可删的。如果folder被省略，则检查输入文件路径的同级目录。

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

### --Verbose
在命令行中输出详细信息

### 位置参数
输入文件路径 [输出文件路径]
#### 输入文件路径
保存μMatrix规则的文件的路径
#### [输出文件路径]
可选。清理后的规则文件的路径。如果不指定，则保存在*输入文件路径*的同级目录，文件名是uMatrix-*日期*.txt。

# 分支说明
<dl>
  <dt>master</dt>
  <dd>主分支</dd>
  <dt>native-release</dt>
  <dd>用于发布windows 10原生代码。</dd>
</dl>
