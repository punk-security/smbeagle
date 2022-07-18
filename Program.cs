using SMBeagle.FileDiscovery;
using SMBeagle.HostDiscovery;
using SMBeagle.NetworkDiscovery;
using SMBeagle.Output;
using SMBeagle.ShareDiscovery;
using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace SMBeagle
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult
                .WithParsed(Run)
                .WithNotParsed(errs => OutputHelp(parserResult, errs));
        }

        static void Run(Options opts)
        {

            if (!opts.Quiet)
                OutputHelper.ConsoleWriteLogo();
            else
                Console.WriteLine("SMBeagle by PunkSecurity [punksecurity.co.uk]");

            if (! RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: should we have an enum for exit codes?
                if (opts.Username == null|| opts.Password == null)
                {
                    OutputHelper.WriteLine("ERROR: Username and Password required on none Windows platforms");
                    Environment.Exit(1);
                }
            }

            if (opts.Username == null ^ opts.Password == null)
            {
                OutputHelper.WriteLine("ERROR: We need a username and password, not just one");
                Environment.Exit(1);
            }
            bool crossPlatform = false;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || opts.Username != null )
            {
                crossPlatform = true;
                // The library we use hangs when scanning ourselves
                if (opts.ScanLocalShares)
                {
                    OutputHelper.WriteLine("ERROR: We cannot scan local shares when running on Linux or with commandline credentials");
                    Environment.Exit(1);
                }
            }

            String username = "";
            if (opts.Username != null)
                username = opts.Username;
            if (opts.Domain != "")
                username = $"{opts.Domain}\\{username}";

            if (opts.ElasticsearchHost != null && opts.ElasticsearchPort != null)
                OutputHelper.EnableElasticsearchLogging($"http://{opts.ElasticsearchHost}:{opts.ElasticsearchPort}/", username);

            if (opts.CsvFile != null)
                OutputHelper.EnableCSVLogging(opts.CsvFile, username);

            NetworkFinder
                nf = new();

            // Discover networks automagically
            if (!opts.DisableNetworkDiscovery)
            {
                OutputHelper.WriteLine("1. Performing network discovery...");
                nf.DiscoverNetworks();

                OutputHelper.WriteLine($"discovered {nf.PrivateNetworks.Count} private networks and {nf.PrivateAddresses.Count} private addresses", 1);

                if (!opts.Quiet)
                {
                    OutputHelper.WriteLine("private networks:", 2);
                    foreach (Network pn in nf.PrivateNetworks)
                        OutputHelper.WriteLine(pn.ToString(), 3);
                    OutputHelper.WriteLine("private addresses:", 2);
                    foreach (string pa in nf.PrivateAddresses)
                        OutputHelper.WriteLine(pa.ToString(), 3);
                }

                if (opts.Verbose)
                {
                    OutputHelper.WriteLine($"discovered but will ignore the following {nf.PublicAddresses.Count} public addresses:", 1);
                    foreach (string pa in nf.PublicAddresses)
                        OutputHelper.WriteLine(pa, 2);
                    OutputHelper.WriteLine($"discovered but will ignore the following {nf.PublicNetworks.Count} public networks:", 1);
                    foreach (Network pn in nf.PublicNetworks)
                        OutputHelper.WriteLine(pn.ToString(), 2);
                }
            }
                
            else
            {
                OutputHelper.WriteLine("1. Skipping network discovery due to -D switch...");
            }

            // build list of provided exclusions
            List<string> filteredAddresses = new();
            List<Network> networks = new();

            if (!opts.DisableNetworkDiscovery)
            {
                OutputHelper.WriteLine("2. Filtering discovered networks and addresses...");

                // build list of discovered and provided networks
                Int16
                    maxNetworkSizeForScanning = Int16.Parse(opts.MaxNetworkSizeForScanning);

                networks = nf.PrivateNetworks
                    .Where(item => item.IPVersion == 4) // We cannot scan ipv6 networks, they are HUGE, but we do scan the ipv6 hosts
                    .Where(item => Int16.Parse(item.Cidr) >= maxNetworkSizeForScanning)
                    .Where(item => !opts.ExcludedNetworks.Contains(item.ToString()))
                    .ToList();

                OutputHelper.WriteLine($"filtered and have {networks.Count} private networks to scan and {filteredAddresses.Count} private addresses to exclude", 1);

                if (!opts.Quiet)
                {
                    if (networks.Count > 0)
                    {
                        OutputHelper.WriteLine("private networks to scan:", 2);
                        foreach (Network pn in networks)
                            OutputHelper.WriteLine(pn.ToString(), 3);
                    }


                    if (filteredAddresses.Count > 0)
                    {
                        OutputHelper.WriteLine("private addresses to exclude:", 2);
                        foreach (string pa in filteredAddresses)
                            OutputHelper.WriteLine(pa, 3);
                    }
                }
            }
            else
            {
                OutputHelper.WriteLine("2. Skipping filtering as network discovery disabled...");
            }

            if (! opts.ScanLocalShares)
            {
                filteredAddresses.AddRange(nf.DiscoverNetworksViaClientConfiguration(store:false));
            }
            filteredAddresses.AddRange(opts.ExcludedHosts.ToList());

            List<string> addresses = new();

            if (opts.Networks.Any() || opts.Hosts.Any())
            {
                OutputHelper.WriteLine("3. Processing manual networks and addresses...");
                foreach (string network in opts.Networks)
                {
                    networks.Add(
                        new Network(network, Enums.NetworkDiscoverySourceEnum.ARGS)
                        );
                    OutputHelper.WriteLine($"added network '{network}'", 1);

                }

                foreach (string address in opts.Hosts)
                {
                    addresses.Add(address);
                    OutputHelper.WriteLine($"added host '{address}'", 1);

                }

            }
            else
            {
                OutputHelper.WriteLine("3. No manual networks or addresses provided, skipping...");
            }

            if (addresses.Count == 0 && networks.Count == 0)
            {
                OutputHelper.WriteLine("After filtering - there are no networks or hosts to scan...");
                Environment.Exit(0);
            }

            OutputHelper.WriteLine("4. Probing hosts and scanning networks for SMB port 445...");

            //TODO: add none quiet output to show what we are scanning at this point - nets, hosts and exclusiosn

            // Begin the scan for up hosts
            HostFinder
                hf = new(addresses, networks, filteredAddresses);

            OutputHelper.WriteLine($"scanning is complete and we have {hf.ReachableHosts.Count} hosts with reachable SMB services", 1);

            if (hf.ReachableHosts.Count == 0)
            {
                OutputHelper.WriteLine("There are no hosts with accessible SMB services...");
                Environment.Exit(0);
            }

            if (opts.Verbose)
            {
                OutputHelper.WriteLine($"reachable hosts:", 2);
                foreach (Host h in hf.ReachableHosts)
                    OutputHelper.WriteLine(h.Address, 3);
            }

            OutputHelper.WriteLine("5. Probing SMB services for accessible shares...");

            if (crossPlatform)
            {
                foreach (Host host in hf.ReachableHosts)
                {
                    Thread t = new(() => CrossPlatformShareFinder.DiscoverDeviceShares(host, opts.Domain, opts.Username, opts.Password));
                    t.Start();
                }
                // Wait for max scan time
                Thread.Sleep(Host.PORT_MAX_WAIT_MS * 4);
            }
            else
            {
                // Enumerate shares
                foreach (Host host in hf.ReachableHosts)
                {
                    Thread t = new(() => WindowsShareFinder.DiscoverDeviceShares(host));
                    t.Start();
                }
                // Wait for max scan time
                Thread.Sleep(Host.PORT_MAX_WAIT_MS * 4);
            }

            OutputHelper.WriteLine($"probing is complete and we have {hf.HostsWithShares.Count} hosts with accessible shares", 1);

            if (hf.HostsWithShares.Count == 0)
            {
                OutputHelper.WriteLine("There are no hosts with accessible SMB shares.  Exiting...");
                Environment.Exit(0);
            }

            if (!opts.Quiet)
            {
                OutputHelper.WriteLine("reachabled hosts with accessible SMB shares:",2);
                foreach (Host host in hf.HostsWithShares)
                    OutputHelper.WriteLine(host.Address,3);
            }
            
            // Build list of uncPaths from up hosts
            List<Share> shares = new();
                foreach (Host h in hf.HostsWithShares)
                    shares.AddRange(h.Shares);

            if (opts.Verbose)
            {
                OutputHelper.WriteLine("accessible SMB shares:", 2);
                foreach (Share share in shares)
                    OutputHelper.WriteLine(share.uncPath, 3);
            }

            if(opts.ExcludeHiddenShares || opts.Shares.Any() || opts.ExcludedShares.Any())
                OutputHelper.WriteLine("6a. Filtering share list");

            if (opts.Shares.Any())
            {
                OutputHelper.WriteLine("Keeping only named shares", 1);
                shares = shares
                    .Where(item => opts.Shares.ToList().ConvertAll(i => i.ToLower()).Contains(item.Name.ToLower()))
                    .ToList();
            }

            if (opts.ExcludeHiddenShares)
            {
                OutputHelper.WriteLine("Filtering out hidden shares",1);
                shares = shares
                    .Where(item => !item.Name.EndsWith('$'))
                    .ToList();
            }

            if (opts.ExcludedShares.Any())
            {
                OutputHelper.WriteLine("Filtering out named excluded shares", 1);
                shares = shares
                    .Where(item => ! opts.ExcludedShares.ToList().ConvertAll(i => i.ToLower()).Contains(item.Name.ToLower()))
                    .ToList();
            }

            if (! shares.Any())
            {
                OutputHelper.WriteLine("There are no accessible SMB shares to scan.  Exiting...");
                Environment.Exit(0);
            }

            if (opts.Verbose)
            {
                OutputHelper.WriteLine($"Shares found:", 1);
                foreach (Share s in shares)
                    OutputHelper.WriteLine(s.uncPath, 2);
            }

            OutputHelper.WriteLine("6. Enumerating accessible shares, this can be slow...");

            // Find files on all the shares
            FileFinder
                ff = new(
                    shares: shares, 
                    getPermissionsForSingleFileInDir: opts.EnumerateOnlyASingleFilesAcl, 
                    enumerateAcls: !opts.DontEnumerateAcls,
                    verbose: opts.Verbose,
                    crossPlatform:crossPlatform
                    );

            OutputHelper.WriteLine("7. Completing the writes to CSV or elasticsearch (or both)");

            OutputHelper.CloseAndFlush();

            OutputHelper.WriteLine(" -- AUDIT COMPLETE --");


            // TODO: know when elasticsearch sink has finished outputting
        }

        static void OutputHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            OutputHelper.ConsoleWriteLogo();
            HelpText helpText = HelpText.AutoBuild(result, h =>
            {
                //configure help
                h.AdditionalNewLineAfterOption = false;
                h.Heading = "";
                h.Copyright = "Apache License 2.0";
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
        Console.WriteLine(helpText);
        }

        static void OutputHelp(Exception err)
        {
            string pad = new('-', err.Message.Length / 2);
            OutputHelper.ConsoleWriteLogo();
            Console.WriteLine($"!{pad} ERROR {pad}!");
            Console.WriteLine("");
            Console.WriteLine("    " + err.Message);
            Console.WriteLine("");
            Console.WriteLine("    For help use --help");
            Console.WriteLine("");
            Console.WriteLine($"!{pad} ERROR {pad}!");
            System.Environment.Exit(1);
        }


        #region Classes

        public class Options
        {

            [Option('c', "csv-file", Group = "output", Required = false, HelpText = "Output results to a CSV file by providing filepath")]
            public string CsvFile { get; set; }

            [Option('e', "elasticsearch-host", Group = "output", Required = false, HelpText = "Output results to elasticsearch by providing elasticsearch hostname (default port is 9200 , but can be overridden)")]
            public string ElasticsearchHost { get; set; }

            [Option("elasticsearch-port", Required = false, Default = "9200", HelpText = "Define the elasticsearch custom port if required")]
            public string ElasticsearchPort { get; set; }

            [Option('f', "fast", Required = false, HelpText = "Enumerate only one files permissions per directory")]
            public bool EnumerateOnlyASingleFilesAcl { get; set; }

            [Option('l', "scan-local-shares", Required = false, HelpText = "Scan the local shares on this machine")]
            public bool ScanLocalShares { get; set; }

            [Option('D', "disable-network-discovery", Required = false, HelpText = "Disable network discovery")]
            public bool DisableNetworkDiscovery { get; set; }

            [Option('n', "network", Required = false, HelpText = "Manually add network to scan (multiple accepted)")]
            public IEnumerable<String> Networks { get; set; }
            [Option('N', "exclude-network", Required = false, HelpText = "Exclude a network from scanning (multiple accepted)")]
            public IEnumerable<string> ExcludedNetworks { get; set; }

            [Option('h', "host", Required = false, HelpText = "Manually add host to scan")]
            public IEnumerable<string> Hosts { get; set; }

            [Option('H', "exclude-host", Required = false, HelpText = "Exclude a host from scanning")]
            public IEnumerable<string> ExcludedHosts { get; set; }

            [Option('q', "quiet", Required = false, HelpText = "Disable unneccessary output")]
            public bool Quiet { get; set; }

            [Option('S', "exclude-share", Required = false, HelpText = "Do not scan shares with this name (multiple accepted)")]
            public IEnumerable<string> ExcludedShares { get; set; }

            [Option('s', "share", Required = false, HelpText = "Only scan shares with this name (multiple accepted)")]
            public IEnumerable<string> Shares { get; set; }

            [Option('E', "exclude-hidden-shares", Required = false, HelpText = "Exclude shares ending in $")]
            public bool ExcludeHiddenShares { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Give more output")]
            public bool Verbose { get; set; }

            [Option('m', "max-network-cidr-size", Required = false, Default = "20", HelpText = "Maximum network size to scan for SMB Hosts")]
            public string MaxNetworkSizeForScanning { get; set; }

            [Option('A', "dont-enumerate-acls", Required = false, Default = false, HelpText = "Skip enumeration of file ACLs")]
            public bool DontEnumerateAcls { get; set; }
            [Option('d', "domain", Required = false, Default = "", HelpText = "Domain for connecting to SMB")]
            public string Domain { get; set; }

            [Option('u', "username", Required = false, HelpText = "Username for connecting to SMB - mandatory on linux")]
            public string Username { get; set; }

            [Option('p', "password", Required = false, HelpText = "Password for connecting to SMB - mandatory on linux")]
            public string Password { get; set; }

            [Usage(ApplicationAlias = "SMBeagle")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    UnParserSettings unParserSettings = new();
                    unParserSettings.PreferShortName = true;
                    yield return new Example("Output to a CSV file", unParserSettings,new Options { CsvFile = "out.csv" });
                    yield return new Example("Output to elasticsearch (Preffered)", unParserSettings, new Options { ElasticsearchHost = "127.0.0.1" });
                    yield return new Example("Output to elasticsearch and CSV", unParserSettings, new Options { ElasticsearchHost = "127.0.0.1", CsvFile = "out.csv" });
                    yield return new Example("Disable network discovery and provide manual networks", unParserSettings, new Options { ElasticsearchHost = "127.0.0.1", DisableNetworkDiscovery = true,  Networks = new List<String>() { "192.168.12.0./23", "192.168.15.0/24" } });
                    yield return new Example("Do not enumerate ACLs (FASTER)", unParserSettings, new Options { ElasticsearchHost = "127.0.0.1", DontEnumerateAcls = true });
                }
            }
        }

        #endregion
    }
}
