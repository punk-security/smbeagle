using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using Serilog.Sinks.Elasticsearch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;

namespace SMBeagle.Output
{
    public static class OutputHelper
    {
        #region Constants

        const string LOGO = @"
        ____              __   _____                      _ __       
       / __ \__  ______  / /__/ ___/___  _______  _______(_) /___  __
      / /_/ / / / / __ \/ //_/\__ \/ _ \/ ___/ / / / ___/ / __/ / / /
     / ____/ /_/ / / / / ,<  ___/ /  __/ /__/ /_/ / /  / / /_/ /_/ / 
    /_/    \__,_/_/ /_/_/|_|/____/\___/\___/\__,_/_/  /_/\__/\__, /  
                                           PRESENTS         /____/  

                         -- SMBeagle v1.0.0 --
";

        #endregion

        #region Private properties

        static ILogger ElasticsearchLogger { get; set; } = null;

        static ILogger CsvLogger { get; set; } = null;

        static readonly CompactJsonFormatter _jsonFormatter = new(new JsonValueFormatter(null));

        static string Hostname { get; set; } = GetHostname();

        static string Username { get; set; }

        #endregion

        #region Public methods

        public static void EnableElasticsearchLogging(string nodeUris, string username = "")
        {
            Username = string.IsNullOrEmpty(username) ? WindowsIdentity.GetCurrent().Name : username;

            // Need to do Index template to match the engine
            ElasticsearchLogger = new LoggerConfiguration()
                .WriteTo.Elasticsearch(
                    customFormatter: _jsonFormatter,
                    nodeUris: nodeUris,
                    autoRegisterTemplate: true,
                    autoRegisterTemplateVersion: AutoRegisterTemplateVersion.ESv7,
                    indexFormat: "SMBeagle-{0:yyyy.MM.dd}"
                )
                .CreateLogger();
        }

        public static void EnableCSVLogging(string path, string username="")
        {
            Username = string.IsNullOrEmpty(username) ? WindowsIdentity.GetCurrent().Name : username;

            CsvLogger = new LoggerConfiguration()
                .WriteTo.File(new CSVFormatter(), path)
                .CreateLogger();
        }

        public static void CloseAndFlush()
        {
            if (ElasticsearchLogger != null)
            {
                Log.Logger = ElasticsearchLogger;
                Log.CloseAndFlush();
            }
            
            if (CsvLogger != null)
            {
                Log.Logger = CsvLogger;
                Log.CloseAndFlush();
            }
        }

        public static void AddPayload(IOutputPayload payload, Enums.OutputtersEnum author)
        {
            LogOut("{hostname}:{username}:{@" + author + "}", payload);
        }

        public static void ConsoleWriteLogo()
        {
            Console.Write(LOGO);
        }

        #endregion

        #region Private methods

        static string GetHostname()
        {
            string
                domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName,
                hostname = Dns.GetHostName();

            if (domainName == "")
                domainName = "WORKGROUP";

            return $"{hostname}.{domainName}";
        }

        static void LogOut(string msg, IOutputPayload payload)
        {
            if (ElasticsearchLogger != null)
                ElasticsearchLogger.Information(msg, Hostname, Username, payload);

            if (CsvLogger != null)
                CsvLogger.Information(msg, Hostname, Username, payload);
        }

        #endregion
    }

    public class CSVFormatter : ITextFormatter
    {
        #region Constants

        const char CSV_SEPERATOR = ',';

        #endregion
        
        #region Static

        private static bool _headersWritten = false;

        #endregion

        #region ITextFormatter

        public void Format(LogEvent logEvent, TextWriter output)
        {
            try // We have issues if the file has a comma in it!  -to revisit
            {
                Dictionary<string,string> 
                    dict = logEvent.Properties["File"].ToString()
                            .Substring("FileOutput {".Length)
                            .Trim('}')
                            .Split(",")
                            .Select(s => s.Split(":", 2))
                            .ToDictionary(
                                p => p[0].Trim().Trim('"'),
                                p => p[1].Trim().Trim('"')
                            );

                string[]
                    keys = dict.Keys.ToArray(),
                    values = dict.Values.ToArray();

                if (!_headersWritten)
                {
                    for(int i=0; i<keys.Length; i++)
                    {
                        output.Write(keys[i]);

                        if (i < keys.Length - 1)
                            output.Write(CSV_SEPERATOR);
                    }

                    output.WriteLine();
                    _headersWritten = true;
                }

                for (int i = 0; i < values.Length; i++)
                {
                    output.Write(values[i]);

                    if (i < values.Length - 1)
                        output.Write(CSV_SEPERATOR);
                }

                output.WriteLine();
            }
            catch
            {
                // Intentionally empty
            }
        }

        #endregion
    }
}
