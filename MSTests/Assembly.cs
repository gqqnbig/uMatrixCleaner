using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog.Extensions.Logging;
using uMatrixCleaner;

namespace MSTests
{
	[TestClass]
	public class Assembly
	{
		[AssemblyInitialize]
		public static void AssemblyInitialize(TestContext context)
		{
			var config = new NLog.Config.LoggingConfiguration();

			var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "log.txt" };
			config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logfile);

			NLog.LogManager.Configuration = config;

			ApplicationLogging.LoggerFactory.AddNLog();
		}
	}
}
