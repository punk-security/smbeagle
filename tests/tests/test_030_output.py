from helpers import *

def test_no_acl_mode_returns_false_perms():
    with SMB(dir_structure=["fileA","fileB","fileC"]):
        for result in runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2", "-A"):
            print(result)
            # assert perms are all false
            assert result["Readable"] == 'False'
            assert result["Writeable"] == 'False'
            assert result["Deletable"] == 'False'

### test fast mode gives matching perms

def test_csv_fields_exist():
    with SMB(dir_structure=["fileA"]):
        fields = ['Name','Host', 'Extension', 'Username', 'Hostname', 'UNCDirectory', 'CreationTime', 'LastWriteTime', 'Readable', 'Writeable', 'Deletable', 'DirectoryType', 'Base']
        for result in runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2", "-A"):
            for field in fields:
                assert field in result.keys()

def test_csv_fields_are_valid():
    with SMB(dir_structure=[("dirA",["fileA.txt"])]):
        fields = ['Name','Host', 'Extension', 'Username', 'Hostname', 'UNCDirectory', 'CreationTime', 'LastWriteTime', 'Readable', 'Writeable', 'Deletable', 'DirectoryType', 'Base']
        for result in runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2"):
            print(result)
            assert result["Name"].lower() == "filea.txt"
            assert result["Extension"].lower() == "txt"
            assert result["Host"] == "127.0.0.2"
            assert result["DirectoryType"] == "SMB"
            assert result["UNCDirectory"].lower() ==  "\\\\127.0.0.2\\share\\dira"
            assert result["Base"].lower() ==  "\\\\127.0.0.2\\share\\"
            assert result["Readable"] == 'True'
            assert result["Writeable"] == 'True'
            assert result["Deletable"] == 'True'
