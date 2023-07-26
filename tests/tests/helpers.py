from impacket import smbserver
import multiprocessing
import os
import subprocess
from time import sleep
import csv
import shutil
import uuid


def __setupSMB(address, dir, SMB2 = True):
    os.chdir(f"{os.environ['ROOTDIR']}empty_dir")
    server = smbserver.SimpleSMBServer(listenAddress=address, listenPort=445)
    server.addShare("share", dir, "")
    server.addCredential("test", 1200, "9FD78381EC915F1AAAD3B435B51404EE", "25EDEDFF26CB970623DDA4733227A3F7")
    server.setSMB2Support(SMB2)
    server.setLogFile('')
    server.start()

def setupSMB(address, dir):
    process = multiprocessing.Process(target=__setupSMB, args=[address, dir])
    process.start()
    return process

class SMB(object):
    def __init__(self, address = "0.0.0.0", dir_structure = ["fileA", "fileB"]):
        self.address = address
        self.dir_structure = dir_structure
        self.dir = f"{os.environ['ROOTDIR']}{uuid.uuid4().hex}"
    def __enter__(self):
        self.smb = setupSMB(self.address, self.dir)
        os.mkdir(self.dir)
        self.populate_dir(self.dir, self.dir_structure)
    def populate_dir(self, dir, dir_structure):
        for item in dir_structure:
            if type(item) != type( () ) and type(item) != type(""):
                raise ValueError("Directory should be list of strings and tuples")
            if type(item) == type( () ):
                #type tuple, so create folder and then parse that structure
                os.mkdir(f"{dir}{os.sep}{item[0]}")
                self.populate_dir(f"{dir}{os.sep}{item[0]}", item[1])
            else:
                # type string, so make the file
                open(f"{dir}{os.sep}{item}", 'a').close()

    def __exit__(self, *args, **kwargs):
        self.smb.kill()
        sleep(1)
        shutil.rmtree(self.dir)
        #self.smb.close()

def runSMBeagle(*args, print_out=True):
    run = subprocess.run(["smbeagle",*args], stdout = subprocess.PIPE, universal_newlines=True)
    if print_out:
        print(run.stdout)
    return run.stdout

def runSMBeagleToCSV(*args):
    return runSMBeagle("-c","out.csv",*args)

def runSMBeagleQuick(*args):
    return runSMBeagleToCSV("-D",*args)

def runSMBeagleToCSVWithAuth(*args):
    try:
        os.environ["NATIVE_AUTH"]
        return runSMBeagleToCSV(*args)
    except:
        return runSMBeagleToCSV("-u","test", "-p", "goose", *args)

def runSMBeagleToCSVWithAuthAndReturnResults(*args):
    print(runSMBeagleToCSVWithAuth(*args))
    sleep(30) # wait for file to flush
    with open('out.csv', newline='') as csvfile:
        results = list(csv.DictReader(csvfile, delimiter=',', quotechar='"'))
        for result in results:
            print(result)
        return results
