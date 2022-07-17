from helpers import *

username_or_password_missing_error = "ERROR: Username and Password required on none Windows platforms" 
def test_username_and_password_required_on_linux():
    assert username_or_password_missing_error in runSMBeagleToCSV()
def test_password_required_on_linux():
    assert username_or_password_missing_error in runSMBeagleToCSV("-p","goose")
def test_username_required_on_linux():
    assert username_or_password_missing_error in runSMBeagleToCSV("-u","goose")
def test_username_and_password_accepted():
    assert username_or_password_missing_error not in runSMBeagleToCSV("-u","goose", "-p", "goose")
def test_long_username_accepted():
    assert username_or_password_missing_error not in runSMBeagleToCSV("--username","goose", "-p", "goose")
def test_long_password_accepted():
    assert username_or_password_missing_error not in runSMBeagleToCSV("-u","goose", "--password", "goose")

output_required_error = "At least one option from group 'output' (c, csv-file, e, elasticsearch-host)"
def test_csv_or_elasticsearch_required():
    assert output_required_error in runSMBeagle()
def test_short_csv_accepted():
    assert output_required_error not in runSMBeagle("-c","out.csv")
def test_long_csv_accepted():
    assert output_required_error not in runSMBeagle("--csv-file","out.csv")
def test_short_elasticsearch_accepted():
    assert output_required_error not in runSMBeagle("-e","elasticsearch")
def test_long_elasticsearch_accepted():
    assert output_required_error not in runSMBeagle("--elasticsearch-host","elasticsearch")


def test_manual_host_accepted():
    assert "127.0.0.2" in runSMBeagleToCSVWithAuth("-h", "127.0.0.2")
def test_multiple_manual_host_accepted():
    output = runSMBeagleToCSVWithAuth("-h", "127.0.0.2", "127.0.0.3")
    assert "127.0.0.2" in output and "127.0.0.3" in output

def test_manual_network_accepted():
    output = runSMBeagleToCSVWithAuth("-n", "127.0.0.0/24")
    assert "127.0.0.0/24" in output
def test_multiple_manual_network_accepted():
    output = runSMBeagleToCSVWithAuth("-n", "127.0.0.0/24", "127.0.1.0/24")
    assert "127.0.0.0/24" in output and "127.0.1.0/24" in output
