using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
			var result = new Parser(with =>
			{
				with.EnableDashDash = true;
				with.HelpWriter = Console.Error;
			}).ParseArguments<Options>(args);

			Clean(System.IO.File.ReadAllText(@"D:\Documents\Visual Studio 2017\Projects\uMatrixCleaner\test.txt"));

		}


		static void Clean(string input)
		{
			var rules = from line in input.Split("\r\n")
						where line.Length > 0 && line.StartsWith("matrix-off") == false && line.StartsWith("noscript-spoof") == false && line.Contains("#") == false
						select new UMatrixRule(line);



			Predicate<UMatrixRule>[] examptedFromRemoving =
			{
                //r=>r.ToString().Contains("#") || r.ToString()=="* * script block" || r.ToString()=="* * frame block",
                //r=>r.Source.Value=="*" && r.Destination.Value!="*" && (r.Type.HasFlag(TypePredicate.Script) || r.Type.HasFlag(TypePredicate.Frame)),
                r=>r.Selector.Destination.Value=="simg.sinajs.cn"
			};
			var workingRules = new HashSet<UMatrixRule>(from rule in rules
														where examptedFromRemoving.All(p => p(rule) == false)
														select rule);


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


			ruleManager.Clean(3);
			sw.Stop();
			logger.LogDebug("合并用时{0}毫秒", sw.ElapsedMilliseconds);


			//throw new NotImplementedException();
		}
	}


}
