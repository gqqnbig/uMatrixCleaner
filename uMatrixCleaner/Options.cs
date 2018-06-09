using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;


namespace uMatrixCleaner
{
	class Options
	{
		[Value(0, HelpText = "uMatrix规则文件", Required = true)]
		public string InputFilePath { get; set; }

		[Value(1, HelpText = "保存到的路径", Required = false)]
		public string OutputFilePath { get; set; }

		[Option('l', "LogFilePath", Required = false, HelpText = "日志路径。如果为空，则不保存日志。如果使用默认值，则按日期保存在当前目录。", Default = "d")]
		public string LogFilePath { get; set; }

		[Option('m', "MergeThreshold", HelpText = "具有多少个相似规则时允许合并。", Required = false, Default = 3)]
		public int MergeThreshold { get; set; }
	}
}
