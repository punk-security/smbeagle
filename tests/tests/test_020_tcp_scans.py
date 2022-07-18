from helpers import *

smb_reachable_message = "we have {} hosts with reachable SMB services"

no_smb_service_discovered_message = smb_reachable_message.format(0)
one_smb_service_discovered_message = smb_reachable_message.format(1)
two_smb_service_discovered_message = smb_reachable_message.format(2)
three_smb_service_discovered_message = smb_reachable_message.format(3)
four_smb_service_discovered_message = smb_reachable_message.format(4)

def test_one_manual_host_tcp_success():
    with SMB():
        assert one_smb_service_discovered_message in runSMBeagleToCSVWithAuth("-D", "-h", "127.0.0.2")

def test_one_manual_host_tcp_fail_if_not_listening():
    with SMB("127.0.0.2"):
        assert no_smb_service_discovered_message in runSMBeagleToCSVWithAuth("-D", "-h", "127.0.0.3")

def test_two_manual_host_tcp_success():
    with SMB("127.0.0.2"):
        with SMB("127.0.0.3"):
            assert two_smb_service_discovered_message in runSMBeagleToCSVWithAuth("-D", "-h", "127.0.0.2", "127.0.0.3")

def test_one_manual_host_tcp_success_and_not_two_if_second_not_listening():
    with SMB("127.0.0.2"):
            assert one_smb_service_discovered_message in runSMBeagleToCSVWithAuth("-D", "-h", "127.0.0.2", "127.0.0.3")

def test_one_discovered_host_tcp_success():
    with SMB("127.0.0.2"):
        assert one_smb_service_discovered_message in runSMBeagleToCSVWithAuth("-D", "-n", "127.0.0.0/24")

def test_no_discovered_host_when_filtered():
    with SMB("127.0.0.2"):
        assert no_smb_service_discovered_message in runSMBeagleToCSVWithAuth("-D", "-n", "127.0.0.0/24","-H","127.0.0.2" )

def test_one_discovered_host_when_one_filtered():
    with SMB("127.0.0.2"):
        with SMB("127.0.0.3"):
            assert one_smb_service_discovered_message in runSMBeagleToCSVWithAuth("-D", "-n", "127.0.0.0/24","-H","127.0.0.2" )

def test_two_discovered_host_tcp_success():
    with SMB("127.0.0.2"):
        with SMB("127.0.0.3"):
            assert two_smb_service_discovered_message in runSMBeagleToCSVWithAuth("-D", "-n", "127.0.0.0/24")

def test_three_discovered_host_tcp_success():
    with SMB("127.0.0.2"):
        with SMB("127.0.0.3"):
            with SMB("127.0.0.4"):
                assert three_smb_service_discovered_message in runSMBeagleToCSVWithAuth("-D", "-n", "127.0.0.0/24")

def test_four_discovered_host_tcp_success():
    with SMB("127.0.0.2"):
        with SMB("127.0.0.3"):
            with SMB("127.0.0.4"):
                with SMB("127.0.0.5"):
                    assert four_smb_service_discovered_message in runSMBeagleToCSVWithAuth("-D", "-n", "127.0.0.0/24")

def test_disable_network_discovery():
    no_networks_to_scan_message = "there are no networks or hosts to scan"
    assert no_networks_to_scan_message in runSMBeagleToCSVWithAuth("-D")
