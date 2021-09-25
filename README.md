```
    ____              __   _____                      _ __       
   / __ \__  ______  / /__/ ___/___  _______  _______(_) /___  __
  / /_/ / / / / __ \/ //_/\__ \/ _ \/ ___/ / / / ___/ / __/ / / /
 / ____/ /_/ / / / / ,<  ___/ /  __/ /__/ /_/ / /  / / /_/ /_/ / 
/_/    \__,_/_/ /_/_/|_|/____/\___/\___/\__,_/_/  /_/\__/\__, /  
                        PRESENTS                        /____/   
```                                                       
    
# SMBeagle v1.0.0

## Usage
```
USAGE:
Output to a CSV file:
  SMBeagle -c out.csv
Output to elasticsearch (Preffered):
  SMBeagle -e 127.0.0.1
Disable network discovery and provide manual networks:
  SMBeagle -D -e 127.0.0.1 -n 192.168.12.0./23 192.168.15.0/24
Scan local filesystem too (SLOW):
  SMBeagle -e 127.0.0.1 -l
Do not enumerate ACLs (FASTER):
  SMBeagle -A -e 127.0.0.1

OPTIONS:
  -c, --csv-file                     (Group: output) Output results to a CSV file by providing filepath
  -e, --elasticsearch-host           (Group: output) Output results to elasticsearch by providing elasticsearch
                                     hostname (port is set to 9200 automatically)
  -l, --scan-local-drives            Scan local drives on this machine
  -L, --exclude-local-shares         Do not scan local drives on this machine
  -D, --disable-network-discovery    Disable network discovery
  -n, --network                      Manually add network to scan
  -N, --exclude-network              Exclude a network from scanning
  -h, --host                         Manually add host to scan
  -H, --exclude-host                 Exclude a host from scanning
  -q, --quiet                        Disable unneccessary output
  -v, --verbose                      Give more output
  -m, --max-network-cidr-size        (Default: 20) Maximum network size to scan for SMB Hosts
  -A, --dont-enumerate-acls          (Default: false) Skip enumeration of file ACLs
  --help                             Display this help screen.
  --version                          Display version information.

```

## Architecture
![Schematic](Docs/schematic.png)
