using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using uMatrixCleaner.Xml;


namespace uMatrixCleaner
{
	public class Program
	{
		private static readonly ILogger logger;
		private static Options options;

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
				//var mergeEventArgs = new MergeEventArgs();
				//mergeEventArgs.MasterRule = new UMatrixRule("* * css allow");
				//mergeEventArgs.RulesToDelete = new List<UMatrixRule>(new[]
				//{
				//	new UMatrixRule("google.com facebook.com cookie block"),
				//	new UMatrixRule("* 1st-party script allow"),
				//});

				//var dedupEventArgs = new DedupRuleEventArgs();
				//dedupEventArgs.MasterRule = new UMatrixRule("www.amazon.com * cookie allow");
				//dedupEventArgs.DuplicateRules = new List<UMatrixRule>(new[]{
				//	new UMatrixRule("www.amazon.com macao.com cookie allow")
				//});

				//var list = new List<EventArgs>();
				//list.Add(mergeEventArgs);
				//list.Add(dedupEventArgs);


				//var overrides = new XmlAttributeOverrides();
				//var xmlAttributes = new XmlAttributes();
				//xmlAttributes.XmlRoot = new XmlRootAttribute("Events");
				////xmlAttributes.XmlElements.Add(new XmlElementAttribute("MergeEvent", typeof(MergeEventArgs)));
				////xmlAttributes.XmlElements.Add(new XmlElementAttribute("DedupRuleEvent", typeof(DedupRuleEventArgs)));

				//overrides.Add(list.GetType(), xmlAttributes);
				//xmlAttributes = new XmlAttributes();
				//xmlAttributes.XmlRoot = new XmlRootAttribute("MergeEvent");
				//overrides.Add(typeof(MergeEventArgs), xmlAttributes);

				//xmlAttributes = new XmlAttributes();
				//xmlAttributes.XmlRoot = new XmlRootAttribute("DedupRuleEvent");
				//overrides.Add(typeof(DedupRuleEventArgs), xmlAttributes);


				//using (var xmlWriter = XmlWriter.Create("a.xml", new XmlWriterSettings { Indent = true }))
				//{

				//	new XmlSerializer(list.GetType(), overrides, new[] { typeof(MergeEventArgs), typeof(DedupRuleEventArgs) }, null, null).Serialize(xmlWriter, list);
				//}

				//using (var xmlReader = XmlReader.Create("a.xml", new XmlReaderSettings { IgnoreWhitespace = true }))
				//{
				//	var obj = (Xml.EventClass)new XmlSerializer(list.GetType(), new[] { typeof(MergeEventArgs), typeof(DedupRuleEventArgs) }).Deserialize(xmlReader);

				//}

				//return;


				options = new Options();
				if (ParseOptions(args)) return;


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



			List<Predicate<UMatrixRule>> fixedRulePredicates = new List<Predicate<UMatrixRule>>(new Predicate<UMatrixRule>[]
				{
					r=>r.ToString()=="* * * block", //不删除默认规则
					r=>r.ToString()=="* * css allow",
					r=>r.ToString()=="* * frame block",
					r=>r.ToString()=="* * image allow",
					r=>r.ToString()=="* 1st-party * allow",
					r=>r.ToString()=="* 1st-party frame allow"
				});
			if (options.CheckLog != null)
			{
				var deletedRules = ReadHistorialDeletions();
				fixedRulePredicates.Add(rule => deletedRules.Contains(rule));
			}

			var isFixedRules = rules.ToLookup(rule => fixedRulePredicates.Any(p => p(rule)));

			HashSet<UMatrixRule> workingRules = new HashSet<UMatrixRule>();
			var random = new Random();
			foreach (var rule in isFixedRules[false])
			{
				if (options.RandomDelete > 0 && random.Next(0, 100) <= options.RandomDelete)
					logger.LogInformation("{0}被随机删除。", rule);
				else
					workingRules.Add(rule);
			}

			//不可删除的规则也要参与锁定
			RuleRelationshipManager ruleManager = new RuleRelationshipManager(workingRules.Union(isFixedRules[true]).ToList());
			EventsHelper events = options.Log != null ? new EventsHelper() : null;

			Stopwatch sw = null;
			if (logger.IsEnabled(LogLevel.Debug))
			{
				sw = new System.Diagnostics.Stopwatch();
				sw.Start();
			}

			ruleManager.MergeEvent += (sender, e) =>
			{
				if (logger.IsEnabled(LogLevel.Information))
				{
					var info = "合并" + string.Join("、\r\n    ", e.RulesToDelete.Select(r => r.ToString("\t")));
					info += "\r\n  为" + e.MasterRule.ToString("\t") + "。";
					logger.LogInformation(info);
				}

				events?.Events.Add(e);
			};

			ruleManager.DedupEvent += (sender, e) =>
			{
				if (logger.IsEnabled(LogLevel.Information))
				{
					var info = "删除    " + string.Join("、\r\n        ", e.DuplicateRules.Select(r => r.ToString("\t")));
					info += $"\r\n因为它被{e.MasterRule.ToString("\t")}包含。";
					logger.LogInformation(info);
				}
				events?.Events.Add(e);
			};


			var newRules = ruleManager.Clean(options.MergeThreshold);
			if (sw != null)
			{
				sw.Stop();
				logger.LogDebug("合并用时{0}毫秒", sw.ElapsedMilliseconds);
			}


			if (events != null)
				SaveEvents(events);
			var newWorkingRules = newRules;

			return string.Join(Environment.NewLine, ignoredLines)+ Environment.NewLine + string.Join(Environment.NewLine, isFixedRules[true].Union(newWorkingRules));

		}

		private static List<UMatrixRule> ReadHistorialDeletions()
		{
			var deletedRules = new List<UMatrixRule>();
			var serializer = new XmlSerializer(typeof(UMatrixRule));
			foreach (var fileName in Directory.EnumerateFiles(options.CheckLog == string.Empty ? AppContext.BaseDirectory : options.CheckLog, "*.xml"))
			{
				try
				{
					XDocument doc = XDocument.Load(fileName);
					var deletedRuleElements = doc.XPathSelectElements("(//DedupRuleEvent/DuplicateRules|//MergeEvent/RulesToDelete)/UMatrixRule");

					foreach (var deletedRuleElement in deletedRuleElements)
					{
						using (var reader = deletedRuleElement.CreateReader())
						{
							var rule = (UMatrixRule)serializer.Deserialize(reader);
							deletedRules.Add(rule);
						}
					}
				}
				catch (Exception ex)
				{
					logger.LogInformation(ex, "读入日志失败。日志路径为{0}", fileName);
				}
			}

			return deletedRules;
		}

		private static void SaveEvents(EventsHelper events)
		{
			var xmlPath = options.Log == "d" ? System.IO.Path.Combine(AppContext.BaseDirectory, "uMatrix-" + DateTimeOffset.Now.ToString("yyyy-MM-dd") + ".xml") : options.Log;
			using (XmlWriter xmlWriter = XmlWriter.Create(xmlPath, new XmlWriterSettings { Indent = true }))
			{
				xmlWriter.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"styles.xsl\"");
				new XmlSerializer(events.GetType(), new[] { typeof(MergeEventArgs), typeof(DedupRuleEventArgs) }).Serialize(xmlWriter, events);
			}

			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			using (var resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".styles.xsl"))
			{
				using (var file = new FileStream(Path.Combine(Path.GetDirectoryName(xmlPath), "styles.xsl"), FileMode.Create, FileAccess.Write))
				{
					resource.CopyTo(file);
				}
			}
		}


		private static bool ParseOptions(string[] args)
		{

			var argList = new List<string>(args);

			if (args.Length == 0 || Options.GetBooleanOption(argList, "--Help"))
			{
				Console.Write(File.ReadAllText("README.md"));
				return true;
			}

			options.Log = Options.GetOptionalNamedOptionArgument(argList, "--Log", "d");
			options.MergeThreshold = Options.GetOptionalNamedOptionArgument(argList, "--MergeThreshold", 3);
			options.IsVerbose = Options.GetBooleanOption(argList, "--Verbose");
			options.CheckLog = Options.GetOptionalNamedOptionArgument(argList, "--CheckLog", string.Empty);
			options.RandomDelete = Options.GetOptionalNamedOptionArgument(argList, "--RandomDelete", 5);
			var p = argList.IndexOf("--");
			if (p > -1)
				argList.RemoveAt(p);
			var unknownOptions = argList.Where(item => item.StartsWith("-"));
			if (unknownOptions.Any())
			{
				logger.LogError("未能识别命名参数：{0}", string.Join(" ", unknownOptions));
				return true;
			}

			if (argList.Count == 0)
			{
				logger.LogError("至少存在1个位置参数，而实际发现0个。");
				return true;
			}

			if (argList.Count > 2)
			{
				logger.LogError("最多支持2个位置参数，而实际发现{0}个：{1}", argList.Count, string.Join(" ", argList));
				return true;
			}

			options.InputFilePath = argList[0];
			if (argList.Count == 2)
				options.OutputFilePath = argList[1];
			return false;
		}

	}
}
