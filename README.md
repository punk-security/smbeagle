[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://GitHub.com/punk-security/pwnspoof/graphs/commit-activity)
[![Maintaner](https://img.shields.io/badge/maintainer-PunkSecurity-blue)](https://www.punksecurity.co.uk)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=punk-security_SMBeagle&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=punk-security_SMBeagle)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=punk-security_SMBeagle&metric=bugs)](https://sonarcloud.io/summary/new_code?id=punk-security_SMBeagle)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=punk-security_SMBeagle&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=punk-security_SMBeagle)

```
    ____              __   _____                      _ __       
   / __ \__  ______  / /__/ ___/___  _______  _______(_) /___  __
  / /_/ / / / / __ \/ //_/\__ \/ _ \/ ___/ / / / ___/ / __/ / / /
 / ____/ /_/ / / / / ,<  ___/ /  __/ /__/ /_/ / /  / / /_/ /_/ / 
/_/    \__,_/_/ /_/_/|_|/____/\___/\___/\__,_/_/  /_/\__/\__, /  
                        PRESENTS                        /____/   
```                                                       
    
=======
# SMBeagle

*** We now run on Linux!!! ***

## Intro

SMBeagle is an (SMB) fileshare auditing tool that hunts out all files it can see in the network 
and reports if the file can be read and/or written.  All these findings are streamed out to either
a CSV file or an elasticsearch host, or both!?  🚀

SMBeagle tries to make use of the win32 APIs for maximum speed, but fails back to a slower ACL check.

It has 2 awesome use cases:

### Cast a spotlight on weak share permissions.
Businesses of all sizes often have file shares with awful file permissions.  

Large businesses have sprawling shares on file servers and its not uncommon to find sensitive data with misconfigured permissions. 

Small businesses often have a small NAS in the corner of the office with no restrictions at all!

SMBeagle crawls these shares and lists out all the files it can read and write.  If it can read them, so can ransomware. 
    
### Lateral movement and privilege escalation
SMBeagle can provide penetration testers with the less obvious routes to escalate privileges and move laterally.

By outputting directly into elasticsearch, testers can quickly find readable scripts and writeable executables.

Finding watering hole attacks and unprotected passwords never felt so easy! 🐱‍👤

## Kibana Dashboard
Please see [Kibana readme](Kibana/README.md) for detailed instructions on installing and using the Kibana dashboards which
provide management visuals and makes data pivoting all the easier.

## Installation

* Go to the latest release on the right-hand side of the repo >>>>  
* Download the win_x64.zip (only 64bit is supported at the moment)
* Unzip the download and run SMBeagle.exe from a command prompt or powershell terminal

## Usage

The only mandatory parameter is to set an output, which should be either an elasticsearch hosts IP address or a csv file.

A good starting point is to enable fast mode and output to csv, but this CSV could get huge depending on how many files it finds.

```
./SMBeagle.exe -c out.csv -f
```

### Public IP scanning

The scanning of discovered public hosts and networks is disabled by default as SMBeagle discovers networks from netstat which 
includes all current connections such as web browser sessions etc.

To scan a public network, declare it manually with something like `-n 1.0.0.1/32` or `-n 1.0.0.0/24`

### Docker usage
Punk security provides a linux docker image of SMBeagle.

Run SMBeagle: `docker run -v "./output:/tmp/output" punksecurity/smbeagle -c /tmp/output/results.csv -n 10.10.10.0/24`
Note that network discovery is disabled when running in docker, so make sure you pass the ranges that
you wish to scan with the `-n` command line switch.

### Full Usage

```
USAGE:
Output to a CSV file:
  SMBeagle -c out.csv
Output to elasticsearch (Preferred):
  SMBeagle -e 127.0.0.1
Output to elasticsearch and CSV:
  SMBeagle -c out.csv -e 127.0.0.1
Disable network discovery and provide manual networks:
  SMBeagle -D -e 127.0.0.1 -n 192.168.12.0./23 192.168.15.0/24
Scan local filesystem too (SLOW):
  SMBeagle -e 127.0.0.1 -l
Do not enumerate ACLs (FASTER):
  SMBeagle -A -e 127.0.0.1

  -c, --csv-file                     (Group: output) Output results to a CSV
                                     file by providing filepath
  -e, --elasticsearch-host           (Group: output) Output results to
                                     elasticsearch by providing elasticsearch
                                     hostname (port is set to 9200
                                     automatically)
  -f, --fast                         Enumerate only one files permissions per
                                     directory
  -l, --scan-local-drives            Scan local drives on this machine
  -L, --exclude-local-shares         Do not scan local drives on this machine
  -D, --disable-network-discovery    Disable network discovery
  -n, --network                      Manually add network to scan
  -N, --exclude-network              Exclude a network from scanning
  -h, --host                         Manually add host to scan
  -H, --exclude-host                 Exclude a host from scanning
  -q, --quiet                        Disable unneccessary output
  -v, --verbose                      Give more output
  -m, --max-network-cidr-size        (Default: 20) Maximum network size to scan
                                     for SMB Hosts
  -A, --dont-enumerate-acls          (Default: false) Skip enumeration of file
                                     ACLs
  -d, --domain                       (Default: ) Domain for connecting to SMB
  -u, --username                     Username for connecting to SMB - mandatory
                                     on linux
  -p, --password                     Password for connecting to SMB - mandatory
                                     on linux
  --help                             Display this help screen.
  --version                          Display version information.

```

## Architecture

SMBeagle does a lot of work, which is broken down into loosely coupled modules which hand off to each other.
This keeps the design simple and allows us to extend each module easily.

![Schematic](Docs/schematic.png)
