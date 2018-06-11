using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
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


			EventsHelper events = null;

			if (options.Log != null)
				events = new EventsHelper();
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
			sw.Stop();
			logger.LogDebug("合并用时{0}毫秒", sw.ElapsedMilliseconds);

			if (events != null)
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



				//反序列化的代码如下
				//using (var xmlReader = XmlReader.Create("a.xml", new XmlReaderSettings { IgnoreWhitespace = true }))
				//{
				//	var obj = (Xml.EventClass)new XmlSerializer(list.GetType(), new[] { typeof(MergeEventArgs), typeof(DedupRuleEventArgs) }).Deserialize(xmlReader);

				//}
			}

			return string.Join(Environment.NewLine, ignoredLines) + string.Join(Environment.NewLine, exemptedRules.Union(newRules));

		}


		private static bool ParseOptions(string[] args)
		{
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
