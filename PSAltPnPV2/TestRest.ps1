
$body = '{
    "devicetypes":  {
                        "toAdd":  [
                                      {
                                          "productId":  "C2960-24PS-TT-L",
                                          "interfaces":  [
                                                             {
                                                                 "firstIndex":  1,
                                                                 "count":  24,
                                                                 "start":  "FastEthernet0/1"
                                                             },
                                                             {
                                                                 "firstIndex":  25,
                                                                 "count":  2,
                                                                 "start":  "GigabitEthernet0/1"
                                                             }
                                                         ],
                                          "manufacturer":  "Cisco",
                                          "name":  "C2960-24PS-TT-L"
                                      }
                                  ],
                        "toRemove":  [
                                         "6434a9ea-9a42-415e-ff5f-08d546689415",
                                         "5a404d3f-02ae-43a5-ff61-08d546689415"
                                     ],
                        "toChange":  [
                                         {
                                             "id":  "30985fed-68bc-4cb8-ff60-08d546689415",
                                             "interfaces":  {
                                                                "toAdd":  [
                                                                              {
                                                                                  "name":  "FastEthernet0/2",
                                                                                  "interfaceIndex":  1
                                                                              }
                                                                          ],
                                                                "toRemove":  [
                                                                                 "0da2c40f-4582-423f-0023-08d546689d48"
                                                                             ],
                                                                "toChange":  [
                                                                                 {
                                                                                     "name":  "FastEthernet0/1",
                                                                                     "id":  "637df770-7fc0-4b32-0024-08d546689d48",
                                                                                     "interfaceIndex":  0
                                                                                 }
                                                                             ]
                                                            }
                                         }
                                     ]
                    },
    "templates":  {
                      "toAdd":  [
                                    {
                                        "name":  "Pizza",
                                        "content":  "! Example pizza file\r\ninterface Gigabit0/0\r\n ip address dhcp\r\n ip nat outside\r\n!\r\ninterface Gigabit0/1\r\n ip address 10.1.1.1 255.255.255.0\r\n ip nat inside\r\n!\r\nip access-list standard ACL_NAT\r\n permit 10.0.0.0 0.255.255.255\r\n!\r\nip nat inside source list ACL_NAT interface Gigabit0/0"
                                    }
                                ],
                      "toChange":  [
                                       {
                                           "id":  "2362cf64-fd27-43cd-e007-08d54668a9e4",
                                           "name":  "r12340001",
                                           "content":  "service timestamps log datetime msec show-timezone\r\nservice password-encryption\r\n!\r\nfile prompt quiet\r\n!\r\nhostname $hostname\r\nip domain-name $domainName\r\n!\r\naaa new-model\r\naaa authentication login default local\r\naaa authentication enable default enable\r\naaa authorization console\r\naaa authorization exec default local\r\n!\r\nenable secret $enableSecret\r\n!\r\ninterface FastEthernet0/0\r\n ip address $ipAddress $subnetMask\r\n ip helper-address $dhcpServer\r\n no shut\r\n!\r\ninterface FastEthernet0/1\r\n ip address $uplinkAddress $uplinkSubnetMask\r\n no shut\r\n!\r\nip route 0.0.0.0 0.0.0.0 $defaultGateway\r\n!\r\nusername admin priv 15 secret Minions12345\r\n!\r\nip access-list standard ACL_CDP_SNOOPER\r\n permit host $automationServer\r\n!\r\nline vty 0 4\r\n login authentication default\r\n transport input telnet\r\n access-class ACL_CDP_SNOOPER in\r\n!\r\nlogging host $syslogServer\r\nlogging trap 7\r\n!\r\nntp server 10.100.1.1\r\n!\r\nend"
                                       }
                                   ]
                  },
    "tftpFiles":  {
                      "toAdd":  [
                                    {
                                        "content":  "You went to school to learn girl\r\nThings you never, never knew before\r\nLike \"I\" before \"E\" except after \"C\"\r\nAnd why 2 plus 2 makes 4\r\nNow, now, now\r\nI\u0027m gonna teach you\r\n(Teach you, teach you)\r\nAll about love girl\r\n(All about love)\r\nSit yourself down, take a seat\r\nAll you gotta do is repeat after me\r\n\r\nA B C\r\nIt\u0027s easy as, 1 2 3\r\nAs simple as, do re mi\r\nA B C, 1 2 3\r\nBaby, you and me girl\r\nA B C\r\nIt\u0027s easy as, 1 2 3\r\nAs simple as, do re mi\r\nA B C, 1 2 3\r\nBaby, you and me girl\r\n\r\nCome on and love me just a little bit\r\nCome on and love me just a little bit\r\nI\u0027m gonna teach you how to sing it out\r\nCome on, come on, come on\r\nLet me show you what it\u0027s all about\r\nReading, writing, arithmetic\r\nAre the branches of the learning tree\r\nBut listen without the roots of love everyday girl\r\nYour education ain\u0027t complete\r\nTea-Tea-Teacher\u0027s gonna show you\r\n(She\u0027s gonna show you)\r\nHow to get an \"A\" (na-na-na-naaaaaa)\r\nHow to spell \"me\", \"you\", add the two\r\nListen to me, baby\r\nThat\u0027s all you got to do\r\n\r\nOh, A B C\r\nIt\u0027s easy as, 1 2 3\r\nAs simple as, do re mi\r\nA B C, 1 2 3\r\nBaby, you and me girl\r\nA B C it\u0027s easy,\r\nIt\u0027s like counting up to 3\r\nSing a simple melody\r\nThat\u0027s how easy love can be\r\nThat\u0027s how easy love can be\r\nSing a simple melody\r\n1 2 3\r\nYou and me\r\n\r\nSit down girl,\r\nI think I love ya\u0027\r\nNo, get up girl\r\nShow me what you can do\r\nShake it, shake it baby, come on now\r\nShake it, shake it baby, oooh, oooh\r\nShake it, shake it baby, yeah\r\n1 2 3 baby, oooh oooh\r\nA B C baby, ah, ah\r\nDo re mi baby, wow\r\nThat\u0027s how easy love can be\r\nA B C it\u0027s easy\r\nIt\u0027s like counting up to 3\r\nSing a simple melody\r\nThat\u0027s how easy love can be\r\nI\u0027m gonna teach you\r\nHow to sing it out\r\nCome-a, come-a, come-a\r\nLet me show you what\u0027s it\u0027s all about\r\nA B C it\u0027s easy\r\nIt\u0027s like counting up to 3\r\nSing a simple melody\r\nThat\u0027s how easy love can be\r\n\r\nI\u0027m gonna teach you\r\nHow to sing it out\r\nSing it out, sing it out\r\nSing it, sing it\r\nA B C it\u0027s easy\r\nIt\u0027s like counting up to 3\r\nSing a simple melody\r\nThat\u0027s how easy love can be",
                                        "name":  "whatever.config.txt"
                                    }
                                ],
                      "toChange":  [
                                       {
                                           "content":  "service timestamps log datetime msec show-timezone\r\nservice password-encryption\r\n!\r\nfile prompt quiet\r\n!\r\nhostname $hostname\r\nip domain-name $domainName\r\n!\r\naaa new-model\r\naaa authentication login default local\r\naaa authentication enable default enable\r\naaa authorization console\r\naaa authorization exec default local\r\n!\r\nvtp mode transparent\r\n!\r\nint vlan 1\r\n ip address dhcp\r\n!\r\nusername $telnetUsername privilege 15 secret $telnetPassword\r\n!\r\nip access-list standard ACL_CDP_SNOOPER\r\n permit $tftpServer 0.0.0.0\r\n!\r\nline vty 0 15\r\n login authentication default\r\n transport input telnet\r\n access-class ACL_CDP_SNOOPER in\r\n!\r\nlogging host $syslogServer\r\nlogging trap 7\r\n!\r\nntp server 10.100.1.1\r\n!\r\nkron policy-list loadConfig\r\n cli send log $deviceReadyMessage\r\n!\r\nkron occurrence loadConfig in 1 recurring\r\n policy-list loadConfig\r\n!\r\nend",
                                           "name":  "unprovisioned.config.txt",
                                           "id":  "9b5d228b-4ff3-4862-ab2f-08d54668d827"
                                       }
                                   ]
                  },
    "networkDevices":  {
                           "toAdd":  [
                                         {
                                             "dhcpTftpBootfile":  "unprovisioned.config.txt",
                                             "dhcpExclusions":  [
                                                                    {
                                                                        "start":  "10.100.16.1",
                                                                        "end":  "10.100.16.100"
                                                                    },
                                                                    {
                                                                        "start":  "10.100.16.200",
                                                                        "end":  "10.100.16.255"
                                                                    }
                                                                ],
                                             "description":  "Edge router",
                                             "hostname":  "r12340001",
                                             "iosVersion":  "XE16.2(33)",
                                             "dhcpRelay":  false,
                                             "deviceType":  "C1841",
                                             "domainName":  "nm.local",
                                             "template":  {
                                                              "description":  "Current configuration",
                                                              "name":  "r12340001",
                                                              "parameters":  [
                                                                                 {
                                                                                     "value":  "172.20.2.2",
                                                                                     "name":  "uplinkAddress"
                                                                                 },
                                                                                 {
                                                                                     "value":  "255.255.255.252",
                                                                                     "name":  "uplinkSubnetMask"
                                                                                 },
                                                                                 {
                                                                                     "value":  "172.20.2.1",
                                                                                     "name":  "defaultGateway"
                                                                                 },
                                                                                 {
                                                                                     "value":  "Minions12345",
                                                                                     "name":  "enableSecret"
                                                                                 },
                                                                                 {
                                                                                     "value":  "Rocket Raccoon",
                                                                                     "name":  "snmpContact"
                                                                                 },
                                                                                 {
                                                                                     "value":  "12345",
                                                                                     "name":  "snmpLocation"
                                                                                 },
                                                                                 {
                                                                                     "value":  "My name is Jonas",
                                                                                     "name":  "deviceDescription"
                                                                                 },
                                                                                 {
                                                                                     "value":  "Access",
                                                                                     "name":  "deviceRole"
                                                                                 },
                                                                                 {
                                                                                     "value":  "r12340001",
                                                                                     "name":  "hostname"
                                                                                 },
                                                                                 {
                                                                                     "value":  "nm.local",
                                                                                     "name":  "domainName"
                                                                                 },
                                                                                 {
                                                                                     "value":  "C1841",
                                                                                     "name":  "deviceType"
                                                                                 },
                                                                                 {
                                                                                     "value":  "255.255.255.0",
                                                                                     "name":  "subnetMask"
                                                                                 }
                                                                             ]
                                                          },
                                             "ipAddress":  "10.100.16.1/24"
                                         }
                                     ],
                           "toRemove":  [
                                            "2720e5d8-7225-4795-58e4-08d54668ad9b"
                                        ],
                           "toChange":  [
                                            {
                                                "id":  "5757ab83-cddd-4530-58e3-08d54668ad9b",
                                                "description":  "CERouter provided by service provider. Not managed"
                                            },
                                            {
                                                "id":  "688d0d51-e3bd-493e-58e5-08d54668ad9b",
                                                "deviceType":  "C2960-24PS-TT-L",
                                                "template":  {
                                                                 "parameters":  {
                                                                                    "toAdd":  [
                                                                                                  {
                                                                                                      "value":  "l1234011a.nm.local",
                                                                                                      "name":  "circuitIdd"
                                                                                                  }
                                                                                              ],
                                                                                    "toRemove":  [
                                                                                                     "4c474587-6b2d-433e-397e-08d54668b406"
                                                                                                 ],
                                                                                    "toChange":  [
                                                                                                     {
                                                                                                         "id":  "41cdb084-ff7e-4233-3982-08d54668b406",
                                                                                                         "name":  "deviceType",
                                                                                                         "value":  "C2960-24PS-TT-L"
                                                                                                     }
                                                                                                 ]
                                                                                }
                                                             }
                                            },
                                            {
                                                "id":  "b6202bb6-9cfb-4d1b-58e6-08d54668ad9b",
                                                "deviceType":  "C2960-24PS-TT-L",
                                                "template":  {
                                                                 "description":  "Current configuration blah",
                                                                 "parameters":  {
                                                                                    "toChange":  [
                                                                                                     {
                                                                                                         "id":  "62b49390-3d99-4ebe-398d-08d54668b406",
                                                                                                         "name":  "deviceType",
                                                                                                         "value":  "C2960-24PS-TT-L"
                                                                                                     }
                                                                                                 ]
                                                                                }
                                                             }
                                            }
                                        ]
                       },
    "connections":  {
                        "toRemove":  [
                                         "f53948cb-7612-4c76-a794-08d54668c8ab",
                                         "65321238-89e3-4bf9-a795-08d54668c8ab",
                                         "2caab54e-d724-45da-a796-08d54668c8ab"
                                     ]
                    }
}'

$apiBaseUri = [Uri]::new("http://localhost:27600")
$uri = ($apiBaseUri.ToString() + 'api/v0/batch')

$requestSplat = @{
    UseBasicParsing = $true
    Uri = $uri
    Method = 'Post'
    ContentType = 'application/json'
    Body = $body
}

$result = Invoke-RestMethod @requestSplat 
