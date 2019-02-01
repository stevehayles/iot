. F:\iot-hackathon\ips.ps1

$env:DOTNET_MULTILEVEL_LOOKUP = 0

function dotnet {
    Param(
        [parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Args
    )
    F:\src\iot\.dotnet\dotnet.exe @Args
}

dotnet publish -r linux-arm
piCopyDir 4 .\bin\Debug\netcoreapp2.1\linux-arm\publish\ /home/pi/rfid
piRun 4 chmod +x /home/pi/rfid/RfidRc522.Samples
piRunSudo 4 /home/pi/rfid/RfidRc522.Samples
