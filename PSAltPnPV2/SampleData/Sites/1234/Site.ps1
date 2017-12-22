@{
    siteName     = '1234'
    description  = 'First demo site'
    location     = 'Under the stairs with Harry'
    devices      = @(
        @{
            hostname         = 'ceRouter1234'
            domainName       = 'nm.local'
            deviceType       = 'C2811'
            ipAddress        = '172.20.2.1/30'
            description      = 'CERouter provided by service provider. Not managed'
            dhcpRelay        = $true
            dhcpTftpBootfile = 'config.txt'
        },
        @{
            hostname         = 'r1234001'
            domainName       = 'nm.local'
            deviceType       = 'C1841'
            iosVersion       = 'XE16.2(33)'
            ipAddress        = '10.100.16.1/24'
            description      = 'Edge router'
            template     = @{
                name        = 'r12340001'
                description = 'Current configuration' 
                parameters  = @(
                    @{ name='uplinkAddress';      value='172.20.2.2' }
                    @{ name='uplinkSubnetMask';   value='255.255.255.252' }
                    @{ name='defaultGateway';     value='172.20.2.1' }
                    @{ name='enableSecret';       value='Minions12345' }
                    @{ name='snmpContact';        value='Rocket Raccoon' }
                    @{ name='snmpLocation';       value='12345' }
                    @{ name='deviceDescription';  value='My name is Jonas' }
                    @{ name='deviceRole';         value='Access' }
                )
            }
            dhcpRelay        = $true
            dhcpExclusions   = @(
                @{ start='10.100.16.1';       end='10.100.16.100' }
                @{ start='10.100.16.200';     end='10.100.16.255' }
            )
            dhcpTftpBootfile = 'unprovisioned.config.txt'
        },
        @{
            hostname     = 'l1234011a'
            domainName   = 'nm.local'
            deviceType   = 'C2960-24PS-TT-L'
            ipAddress    = '10.100.16.11/24'
            template     = @{
                name        = 'WS-C2960G-24TC-L-Access'
                description = 'Current configuration' 
                parameters  = @(
                    @{ name='defaultGateway';     value='10.100.16.1' }
                    @{ name='enableSecret';       value='catAteMyHomework' }
                    @{ name='snmpContact';        value='Rocket Raccoon' }
                    @{ name='snmpLocation';       value='12345' }
                    @{ name='deviceDescription';  value='My name is Jonas' }
                    @{ name='deviceRole';         value='Access' }
                    @{ name='circuitIdd';          value='l1234011a.nm.local' }
                    )
            }
        },
        @{
            hostname     = 'l1234011b'
            domainName   = 'nm.local'
            deviceType   = 'C2960-24PS-TT-L'
            ipAddress    = '10.100.16.12/24'
            template     = @{
                name        = 'WS-C2960G-24TC-L-Access'
                description = 'Current configuration blah' 
                parameters  = @(
                    @{ name='defaultGateway';     value='10.100.16.1' }
                    @{ name='enableSecret';       value='catAteMyHomework' }
                    @{ name='snmpContact';        value='Rocket Raccoon' }
                    @{ name='snmpLocation';       value='1234' }
                    @{ name='deviceDescription';  value='My name is Jonas' }
                    @{ name='deviceRole';         value='Access' }
                )
            }
        }
    )
    connections = @(
        @{
            domainName         = 'nm.local'
            networkDevice      = 'l1234011a'
            interface          = 'GigabitEthernet0/2'
            uplinkToDevice     = 'r1234001'
            uplinkToInterface  = 'FastEthernet0/0'
        },
        @{
            domainName         = 'nm.local'
            networkDevice      = 'l1234011b'
            interface          = 'GigabitEthernet0/2'
            uplinkToDevice     = 'l1234011a'
            uplinkToInterface  = 'GigabitEthernet0/1'
        },
        @{
            domainName          = 'nm.local'
            networkDevice       = 'r1234001'
            interface           = 'FastEthernet0/1'
            uplinkToDevice      = 'ceRouter1234'
            uplinkToInterface   = 'FastEthernet0/0'
        }
    )
}