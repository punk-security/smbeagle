from time import time
from helpers import *

one_file = ["fileA"]
two_files = ["fileA", "fileB"]
no_files = []
empty_dir = [("emptyDir", [])]
dir_with_one_file = [("dirA", one_file)]
dir_with_two_files = [("dirB", two_files)]
empty_dir_with_empty_dir = [("emptyDir", empty_dir)]
empty_dir_with_empty_dir_nested = [("emptyDir", empty_dir_with_empty_dir)]
two_files_in_two_nested_dirs = [("dirA", [("dirB", two_files)])]

detected_share_message = r"Enumerating all subdirectories for '\\{host}\{share}\'"

def test_detect_normal_share():
    with SMB():
        assert detected_share_message.format(host = "127.0.0.2", share = "share") in runSMBeagleToCSVWithAuth("-h","127.0.0.2")

def test_detect_admin_share():
    with SMB():
        assert detected_share_message.format(host = "127.0.0.2", share = "ipc$") in runSMBeagleToCSVWithAuth("-h","127.0.0.2")

def test_do_not_detect_admin_share():
    with SMB():
        assert detected_share_message.format(host = "127.0.0.2", share = "ipc$") not in runSMBeagleToCSVWithAuth("-h","127.0.0.2","-E")

def test_do_not_detect_none_matching_share():
    with SMB():
        assert detected_share_message.format(host = "127.0.0.2", share = "share") not in runSMBeagleToCSVWithAuth("-h","127.0.0.2","-s","goose")

def test_detect_matching_share():
    with SMB():
        assert detected_share_message.format(host = "127.0.0.2", share = "share") in runSMBeagleToCSVWithAuth("-h","127.0.0.2","-s","share")

def test_detect_matching_share_from_multiple():
    with SMB():
        assert detected_share_message.format(host = "127.0.0.2", share = "share") in runSMBeagleToCSVWithAuth("-h","127.0.0.2","-s","goose","share")

def test_one_host_no_files():
    with SMB(dir_structure=no_files):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 0

def test_one_host_empty_dir():
    with SMB(dir_structure=empty_dir):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 0

def test_one_host_empty_dir_nested():
    with SMB(dir_structure=empty_dir_with_empty_dir):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 0

def test_one_host_empty_dir_nested_twice():
    with SMB(dir_structure=empty_dir_with_empty_dir_nested):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 0

def test_one_host_one_file():
    with SMB(dir_structure=one_file):
        assert runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2") == 1

def test_one_host_two_files():
    with SMB(dir_structure=two_files):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 2

def test_one_host_empty_dir_and_two_files():
    with SMB(dir_structure=(empty_dir + two_files)):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 2

def test_one_host_one_dir_and_one_file():
    with SMB(dir_structure=dir_with_one_file):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 1

def test_one_host_one_dir_and_two_files():
    with SMB(dir_structure=dir_with_two_files):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 2

def test_one_host_one_dir_and_one_file_and_another_root_file():
    with SMB(dir_structure=dir_with_one_file + one_file):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 2

def test_one_host_two_dirs_with_three_files_and_another_root_file():
    with SMB(dir_structure=dir_with_one_file + dir_with_two_files + one_file):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 4

def test_one_host_two_dirs_with_three_files_and_another_two_root_files():
    with SMB(dir_structure=dir_with_one_file + dir_with_two_files + two_files):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 5

def test_one_host_two_dirs_with_three_files():
    with SMB(dir_structure=dir_with_one_file + dir_with_two_files):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 3

def test_one_host_two_files_in_nested_dirs():
    with SMB(dir_structure=two_files_in_two_nested_dirs):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == 2

def n_files(n):
    return [f"a{x}" for x in range(0,n)]

def smb_with_n_files(n):
    with SMB(dir_structure=n_files(n)):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")) == n

def test_ten_files_in_the_root():
    smb_with_n_files(10)

def test_fifty_files_in_the_root():
    smb_with_n_files(50)

def test_one_hundred_files_in_the_root():
    smb_with_n_files(100)

def test_five_hundred_files_in_the_root():
    smb_with_n_files(500)

def test_nine_hundred_files_in_the_root():
    smb_with_n_files(900)

# FAILS    
#def test_one_thousand_files_in_the_root():
#    smb_with_n_files(1000)

def n_files_in_n_dirs(files, dirs):
    return [(f"dir{x}", n_files(files)) for x in range(0,dirs)]

def smb_with_n_files_in_n_dirs(files, dirs):
    with SMB(dir_structure=n_files_in_n_dirs(files, dirs)):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2", "-A")) == files * dirs

def test_ten_files_in_two_folders():
    smb_with_n_files_in_n_dirs(10,2)

def test_fifty_files_in_two_folders():
    smb_with_n_files_in_n_dirs(50,2)

def test_one_hundred_files_in_two_folders():
    smb_with_n_files_in_n_dirs(100,2)

def test_five_hundred_files_in_two_folders():
    smb_with_n_files_in_n_dirs(500,2)

def test_ten_files_in_five_folders():
    smb_with_n_files_in_n_dirs(10,5)

def test_fifty_files_in_five_folders():
    smb_with_n_files_in_n_dirs(50,5)

def test_one_hundred_files_in_five_folders():
    smb_with_n_files_in_n_dirs(100,5)

def test_five_hundred_files_in_five_folders():
    smb_with_n_files_in_n_dirs(500,5)

def test_five_hundred_files_in_one_hundred_folders():
    smb_with_n_files_in_n_dirs(500,100)

def test_fast_mode_is_faster():
    start = time()
    with SMB(dir_structure=n_files_in_n_dirs(50, 5)):
        runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2")
    time_to_run_normal = time() - start
    start = time()
    with SMB(dir_structure=n_files_in_n_dirs(50, 5)):
        runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2", "-f")
    time_to_run_fast = time() - start
    assert time_to_run_normal > time_to_run_fast

def test_no_acl_mode_is_faster_than_fast():
    start = time()
    with SMB(dir_structure=n_files_in_n_dirs(50, 5)):
        runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2", "-f")
    time_to_run_fast = time() - start
    start = time()
    with SMB(dir_structure=n_files_in_n_dirs(50, 5)):
        runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2", "-A")
    time_to_run_no_acl = time() - start
    assert time_to_run_fast > time_to_run_no_acl

def n_files_in_dir_n_deep(files, depth):
    dir = [(f"dir", n_files(files))]
    for x in range(0,depth):
        dir = [(f"dir{x}", dir)]
    return dir

def smb_with_n_files_at_depth_n(n, depth):
    with SMB(dir_structure=n_files_in_dir_n_deep(n, depth)):
        assert len(runSMBeagleToCSVWithAuthAndReturnResults("-h", "127.0.0.2", "-A")) == n

def test_ten_files_one_folders_deep():
    smb_with_n_files_at_depth_n(10,1)

def test_ten_files_two_folders_deep():
    smb_with_n_files_at_depth_n(10,2)

def test_ten_files_three_folders_deep():
    smb_with_n_files_at_depth_n(10,3)

def test_ten_files_ten_folders_deep():
    smb_with_n_files_at_depth_n(10,10)
