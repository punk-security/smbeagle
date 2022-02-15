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

            if (opts.ElasticsearchHost != null)
                OutputHelper.EnableElasticsearchLogging($"http://{opts.ElasticsearchHost}:9200/");

            if (opts.CsvFile != null)
                OutputHelper.EnableCSVLogging(opts.CsvFile);

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
                    OutputHelper.WriteLine($"discovered but will ignore the following {nf.DiscoveredPublicNetworks.Count} public networks:", 1);
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

                if (opts.DisableLocalShares)
                    filteredAddresses.AddRange(nf.LocalAddresses);

                filteredAddresses.AddRange(opts.ExcludedHosts.ToList());

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


            List<string> addresses = new();

            if (opts.Networks.Count() > 0 | opts.Hosts.Count() > 0)
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

            if (addresses.Count == 0 & networks.Count == 0)
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
                OutputHelper.WriteLine("There are no hosts with accessible SMB shares...");
                Environment.Exit(0);
            }

            if (opts.Verbose)
            {
                OutputHelper.WriteLine($"reachable hosts:", 2);
                foreach (Host h in hf.ReachableHosts)
                    OutputHelper.WriteLine(h.Address, 3);
            }

            OutputHelper.WriteLine("5. Probing SMB services for accessible shares...");

            // Enumerate shares
            foreach (Host h in hf.ReachableHosts)
                ShareFinder.DiscoverDeviceShares(h);

            OutputHelper.WriteLine($"probing is complete and we have {hf.HostsWithShares.Count} hosts with accessible shares", 1);

            if (hf.HostsWithShares.Count == 0)
            {
                OutputHelper.WriteLine("There are no hosts with accessible SMB shares...");
                Environment.Exit(0);
            }

            if (!opts.Quiet)
            {
                OutputHelper.WriteLine("reachabled hosts with accessible SMB shares:",2);
                foreach (Host host in hf.HostsWithShares)
                    OutputHelper.WriteLine(host.Address,3);
            }
            
            // Build list of uncPaths from up hosts
            List<string> uncPaths = new();
                foreach (Host h in hf.ReachableHosts.Where(item => item.ShareCount > 0))
                    uncPaths.AddRange(h.UNCPaths);

            if (opts.Verbose)
            {
                OutputHelper.WriteLine("accessible SMB shares:", 2);
                foreach (string uncPath in uncPaths)
                    OutputHelper.WriteLine(uncPath, 3);
            }

            OutputHelper.WriteLine("6. Enumerating accessible shares, this can be slow...");

            // Find files on all the shares
            FileFinder
                ff = new(
                    paths: uncPaths, 
                    getPermissionsForSingleFileInDir: opts.EnumerateOnlyASingleFilesAcl, 
                    enumerateLocalDrives: opts.EnumerateLocalDrives, 
                    username: "", 
                    enumerateAcls: !opts.DontEnumerateAcls,
                    verbose: opts.Verbose
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

            [Option('e', "elasticsearch-host", Group = "output", Required = false, HelpText = "Output results to elasticsearch by providing elasticsearch hostname (port is set to 9200 automatically)")]
            public string ElasticsearchHost { get; set; }

            [Option('f', "fast", Required = false, HelpText = "Enumerate only one files permissions per directory")]
            public bool EnumerateOnlyASingleFilesAcl { get; set; }

            [Option('l', "scan-local-drives", Required = false, HelpText = "Scan local drives on this machine")]
            public bool EnumerateLocalDrives { get; set; }
            [Option('L', "exclude-local-shares", Required = false, HelpText = "Do not scan local drives on this machine")]
            public bool DisableLocalShares { get; set; }

            [Option('D', "disable-network-discovery", Required = false, HelpText = "Disable network discovery")]
            public bool DisableNetworkDiscovery { get; set; }

            [Option('n', "network", Required = false, HelpText = "Manually add network to scan")]
            public IEnumerable<String> Networks { get; set; }
            [Option('N', "exclude-network", Required = false, HelpText = "Exclude a network from scanning")]
            public IEnumerable<string> ExcludedNetworks { get; set; }

            [Option('h', "host", Required = false, HelpText = "Manually add host to scan")]
            public IEnumerable<string> Hosts { get; set; }

            [Option('H', "exclude-host", Required = false, HelpText = "Exclude a host from scanning")]
            public IEnumerable<string> ExcludedHosts { get; set; }

            [Option('q', "quiet", Required = false, HelpText = "Disable unneccessary output")]
            public bool Quiet { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Give more output")]
            public bool Verbose { get; set; }

            [Option('m', "max-network-cidr-size", Required = false, Default = "20", HelpText = "Maximum network size to scan for SMB Hosts")]
            public string MaxNetworkSizeForScanning { get; set; }

            [Option('A', "dont-enumerate-acls", Required = false, Default = false, HelpText = "Skip enumeration of file ACLs")]
            public bool DontEnumerateAcls { get; set; }

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
                    yield return new Example("Scan local filesystem too (SLOW)", unParserSettings, new Options { ElasticsearchHost = "127.0.0.1", EnumerateLocalDrives = true });
                    yield return new Example("Do not enumerate ACLs (FASTER)", unParserSettings, new Options { ElasticsearchHost = "127.0.0.1", DontEnumerateAcls = true });
                }
            }
        }

        #endregion
    }
}
