$url = ('https://www.python.org/ftp/python/{0}/python-{0}-amd64.exe' -f $env:PYTHON_VERSION)
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri $url -OutFile 'python.exe';
# https://docs.python.org/3.7/using/windows.html#installing-without-ui
Start-Process python.exe -Wait -ArgumentList @(
    '/quiet',
    'InstallAllUsers=1',
    'TargetDir=C:\Python',
    'PrependPath=1',
    'Shortcuts=0',
    'Include_doc=0',
    'Include_pip=1',
    'Include_test=0'
	);
#the installer updated PATH, so we should refresh our local value
Remove-Item python.exe -Force
