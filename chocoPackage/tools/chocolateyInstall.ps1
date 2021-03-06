$brokerName = 'K2Field.K2NE.ServiceBroker'
$k2Server = "localhost";
$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition

Function StartK2Service([string]$server) {
    Write-Host -ForegroundColor DarkMagenta "Starting K2 blackpearl service on $server"
    Get-Service -DisplayName 'K2 blackpearl Server' -ComputerName $server | where-object {$_.Status -eq "Stopped"} | Start-Service
}

Function StopK2Service([string]$server) {
    Write-Host -ForegroundColor DarkMagenta "Stopping K2 blackpearl service on $server"
    Get-Service -DisplayName 'K2 blackpearl Server' -ComputerName $server | where-object {$_.Status -eq "Running"} | Stop-Service
}

Function GetK2InstallPath([string]$server = $env:computername) {
    $registryKeyLocation = "SOFTWARE\SourceCode\BlackPearl\blackpearl Core\"
    $registryKeyName = "InstallDir"

	Write-Debug "Getting K2 install path from $machine "
    
    $reg = [Microsoft.Win32.RegistryKey]::OpenRemoteBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, $machine)
    $regKey= $reg.OpenSubKey($registryKeyLocation)
    $installDir = $regKey.GetValue($registryKeyName)
    return $installDir
}


Function GetK2ConnectionString([string]$server = $env:computername, [int]$port = 5555) {
    Write-Debug "Creating connectionstring for machine '$k2Farm' and port '$port'";
	
	Add-Type -AssemblyName ('SourceCode.HostClientAPI, Version=4.0.0.0, Culture=neutral, PublicKeyToken=16a2c5aaaa1b130d')
    $connString = New-Object -TypeName "SourceCode.Hosting.Client.BaseAPI.SCConnectionStringBuilder";
    $connString.Integrated = $true;
    $connString.IsPrimaryLogin = $true;
    $connString.Host = $server;
    $connString.Port = $port;

    #Return the connectionstring
    return $connString.ConnectionString; 
}



Function RegisterServiceType([string]$k2ConnectionString) {
  # Get Paths for local environment and for the remote machine, we might run this installer from a simple windows 7 host, while we deploy to a server that has a different drive...
    $k2Path = GetK2InstallPath
    $smoManServiceAssembly = Join-Path $k2Path "bin\SourceCode.SmartObjects.Services.Management.dll"
    $serviceBrokerAssembly = Join-Path $k2Path "ServiceBroker\K2Field.K2NE.ServiceBroker.dll"
    $sbGuid = [guid]"FF92264F-3EE4-4DED-946F-F2550021A274"
    Write-Debug "Adding/Updating ServiceType $serviceBrokerAssembly with guid $sbGuid using $k2ConnectionString"
    
    Add-Type -Path $smoManServiceAssembly # we load this assembly locally, but we connect to the remote host.
    $smoManService = New-Object SourceCode.SmartObjects.Services.Management.ServiceManagementServer

    #Create connection and capture output (methods return a bool)
    $tmpOut = $smoManService.CreateConnection()
    $tmpOut = $smoManService.Connection.Open($k2ConnectionString);
    Write-Debug "Connected to K2 host server"

    # Check if we need to update or register a new one...
    if ([string]::IsNullOrEmpty($smoManService.GetServiceType($sbGuid)) ) {
        Write-Debug "Registering new service type..."
        $tmpOut = $smoManService.RegisterServiceType($sbGuid, "K2Field.CMSF.ServiceObjects.CMSFServiceBroker", "CMSF", "CMSF Service Broker", $serviceBrokerAssembly, "K2Field.CMSF.ServiceObjects.CMSFServiceBroker");
        write-debug "Registered new service type..."
    } else {
        Write-Debug "Updating service type..."
        $tmpOut = $smoManService.UpdateServiceType($sbGuid, "K2Field.CMSF.ServiceObjects.CMSFServiceBroker", "CMSF", "CMSF Service Broker", $serviceBrokerAssembly, "K2Field.CMSF.ServiceObjects.CMSFServiceBroker");
        Write-Debug "Updated service type..."
    }

    $smoManService.Connection.Close();
    write-host "Deployed service-type"
}


Write-Host Stopping Service
StopK2Service -server $k2Server

$targetPath = GetK2InstallPath -server $k2Server

Write-Host Installing files
Copy-Item $scriptPath\* -Include *.dll,*.md -Destination "$targetPath"


# $conString = GetK2ConnectionString -server $k2Server
# RegisterServiceType -k2ConnectionString $conString


Write-Host Starting Service
StartK2Service -server localhost
