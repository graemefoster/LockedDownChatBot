param dnsResourceName string
param applicationName string
param gatewayPublicIp string

resource dnsANameToFirewall 'Microsoft.Network/dnsZones/A@2018-05-01' = {
  name: '${dnsResourceName}/${applicationName}'
  properties: {
    TTL: 60
    ARecords: [
      {
        ipv4Address: gatewayPublicIp
      }
    ]
  }
}

resource dnsANameToFirewallScm 'Microsoft.Network/dnsZones/A@2018-05-01' = {
  name: '${dnsResourceName}/${applicationName}.scm'
  properties: {
    TTL: 60
    ARecords: [
      {
        ipv4Address: gatewayPublicIp
      }
    ]
  }
}
