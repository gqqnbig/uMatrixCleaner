using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace uMatrixCleaner
{
	public class Program
	{
		private static readonly ILogger logger;

		static Program()
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
			var configuration = builder.Build();

			ApplicationLogging.LoggerFactory.AddConsole(configuration.GetSection("Logging"));

			logger = ApplicationLogging.CreateLogger<Program>();
		}

		static void Main(string[] args)
		{
			try
			{

				var options = new Options();
				var argList = new List<string>(args);
				options.Log = Options.GetOptionalNamedOptionArgument(argList, "--Log", "d");
				options.MergeThreshold = Options.GetOptionalNamedOptionArgument(argList, "--MergeThreshold", 3);
				options.IsVerbose = Options.GetBooleanOption(argList, "--Verbose");
				var p = argList.IndexOf("--");
				if (p > -1)
					argList.RemoveAt(p);
				var unknownOptions = argList.Where(item => item.StartsWith("-"));
				if (unknownOptions.Any())
				{
					logger.LogError("未能识别命名参数：{0}", string.Join(" ", unknownOptions));
					return;
				}
				if (argList.Count > 2)
				{
					logger.LogError("最多支持2个位置参数，而实际发现{0}个：{1}", argList.Count, string.Join(" ", argList));
					return;
				}

				options.InputFilePath = argList[0];
				if (argList.Count == 2)
					options.OutputFilePath = argList[1];


				if (options.Log != null)
				{
					var config = new NLog.Config.LoggingConfiguration();


					var logFile = new NLog.Targets.FileTarget("logfile");
					if (options.Log == "d")
					{
						logFile.FileName = "uMatrixCleaner.log";
						logFile.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day;
					}
					else
						logFile.FileName = options.OutputFilePath;
					config.AddRule(options.IsVerbose ? NLog.LogLevel.Debug : NLog.LogLevel.Info, NLog.LogLevel.Fatal, logFile);

					NLog.LogManager.Configuration = config;

					ApplicationLogging.LoggerFactory.AddNLog(new NLogProviderOptions { IgnoreEmptyEventId = true });
				}


				var outputString = Clean(File.ReadAllText(options.InputFilePath));

				if (string.IsNullOrEmpty(options.OutputFilePath))
				{
					var path = Path.GetDirectoryName(options.InputFilePath);
					path = Path.Combine(path, "uMatrixRules-" + DateTimeOffset.Now.ToString("yyyy-MM-dd") + ".txt");
					File.WriteAllText(path, outputString);
				}
				else
					File.WriteAllText(options.OutputFilePath, outputString);

			}
			finally
			{
				ApplicationLogging.LoggerFactory.Dispose();
			}
		}


		static string Clean(string input)
		{
			var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var ignoredLines = (from line in lines
								where line.StartsWith("matrix-off") || line.StartsWith("noscript-spoof") || line.Contains("#")
								select line).ToArray();
			var rules = lines.Except(ignoredLines).Select(l => new UMatrixRule(l)).ToArray();



			Predicate<UMatrixRule>[] examptedFromRemoving =
			{
                //r=>r.ToString().Contains("#") || r.ToString()=="* * script block" || r.ToString()=="* * frame block",
                //r=>r.Source.Value=="*" && r.Destination.Value!="*" && (r.Type.HasFlag(TypePredicate.Script) || r.Type.HasFlag(TypePredicate.Frame)),
                r=>r.Selector.Destination.Value=="simg.sinajs.cn"
			};
			var exemptedRules = (from rule in rules
								 where examptedFromRemoving.Any(p => p(rule))
								 select rule).ToArray();

			var workingRules = new HashSet<UMatrixRule>(rules.Except(exemptedRules));


			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			var ruleManager = new RuleRelationshipManager(workingRules.ToList());
			ruleManager.MergeEvent += (sender, e) =>
			{
				if (logger.IsEnabled(LogLevel.Information))
				{
					var info = "合并" + string.Join("、\r\n    ", e.RulesToDelete.Select(r => r.ToString("\t")));
					info += "\r\n  为" + e.MasterRule.ToString("\t") + "。";
					logger.LogInformation(info);
				}
			};

			ruleManager.DedupEvent += (sender, e) =>
			 {
				 if (logger.IsEnabled(LogLevel.Information))
				 {
					 var info = "删除    " + string.Join("、\r\n        ", e.DuplicateRules.Select(r => r.ToString("\t")));
					 info += $"\r\n因为它被{e.MasterRule.ToString("\t")}包含。";
					 logger.LogInformation(info);
				 }
			 };


			var newRules = ruleManager.Clean(3);
			sw.Stop();
			logger.LogDebug("合并用时{0}毫秒", sw.ElapsedMilliseconds);


			return string.Join(Environment.NewLine, ignoredLines) + string.Join(Environment.NewLine, exemptedRules.Union(newRules));

		}
	}
}
