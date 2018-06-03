using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace uMatrixCleaner
{
    public class ApplicationLogging
    {
        public static ILoggerFactory LoggerFactory { get; } = new LoggerFactory();
        public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
    }
}
