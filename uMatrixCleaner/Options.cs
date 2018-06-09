using System;
using System.Collections.Generic;
using System.Text;
//using CommandLine;


namespace uMatrixCleaner
{
	class Options
	{
		/// <summary>
		/// uMatrix规则文件
		/// </summary>
		//[Value(0, HelpText = "uMatrix规则文件", Required = true)]
		public string InputFilePath { get; set; }

		/// <summary>
		/// 保存到的路径
		/// </summary>
		//[Value(1, HelpText = "保存到的路径", Required = false)]
		public string OutputFilePath { get; set; }

		/// <summary>
		/// 日志路径。如果为空，则不保存日志。如果使用默认值，则按日期保存在当前目录。
		/// </summary>
		//[Option('l', "LogFilePath", Required = false, HelpText = "日志路径。如果为空，则不保存日志。如果使用默认值，则按日期保存在当前目录。", Default = "d")]
		public string Log { get; set; }

		//[Option("Verbose", Default = false, HelpText = "输出详细日志")]
		public bool IsVerbose { get; set; }

		/// <summary>
		/// 具有多少个相似规则时允许合并。
		/// </summary>
		//[Option('m', "MergeThreshold", HelpText = "具有多少个相似规则时允许合并。", Required = false, Default = 3)]
		public int MergeThreshold { get; set; }

		internal static bool GetBooleanOption(List<string> args, string option)
		{
			int p = args.IndexOf(option);
			if (p != -1)
				args.RemoveAt(p);
			return p != -1;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		/// <param name="option"></param>
		/// <param name="defaultValue">当选项存在但值不存在时返回的值。如果选项本身不存在，则固定返回default(T)。</param>
		/// <returns></returns>
		internal static string GetOptionalNamedOptionArgument(List<string> args, string option, string defaultValue)
		{
			int p = args.IndexOf(option);
			if (p == -1)
			{
				return null;
			}
			else
			{
				if (p + 1 == args.Count)
				{
					args.RemoveAt(p);
					return defaultValue;
				}
				else if (args[p + 1].StartsWith("-"))
				{
					args.RemoveAt(p);
					return defaultValue;
				}
				else
				{
					var value = args[p + 1];
					args.RemoveAt(p);
					args.RemoveAt(p);
					return value;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		/// <param name="option"></param>
		/// <param name="defaultValue">当选项存在但值不存在时返回的值。如果选项本身不存在，则固定返回default(T)。</param>
		/// <returns></returns>
		internal static T GetOptionalNamedOptionArgument<T>(List<string> args, string option, T defaultValue) where T : struct
		{
			int p = args.IndexOf(option);
			if (p == -1)
			{
				return defaultValue;
			}
			else
			{
				if (p + 1 == args.Count)
				{
					args.RemoveAt(p);
					return defaultValue;
				}
				else if (args[p + 1].StartsWith("-"))
				{
					args.RemoveAt(p);
					return defaultValue;
				}
				else
				{
					var value = args[p + 1];
					args.RemoveAt(p);
					args.RemoveAt(p);
					return (T)Convert.ChangeType(value, typeof(T));
				}
			}
		}
	}
}
