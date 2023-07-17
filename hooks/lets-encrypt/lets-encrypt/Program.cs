using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ACMESharp.Authorizations;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.Security.KeyVault.Certificates;
using Newtonsoft.Json;

var deployEdgeSecurity = Environment.GetEnvironmentVariable("DEPLOY_EDGE_SECURITY");

//Don't bother getting a LetsEncrypt cert if we aren't deploying the edge security pieces.
if (!deployEdgeSecurity) { return false;}

var location = Environment.GetEnvironmentVariable("AZURE_LOCATION");
var azdEnvironment = Environment.GetEnvironmentVariable("AZURE_ENV_NAME");
var certificateName = Environment.GetEnvironmentVariable("CHAT_API_CUSTOM_HOST");
var dnsSuffix = Environment.GetEnvironmentVariable("DNS_RESOURCE_NAME")!;
var dnsResourceGroup = Environment.GetEnvironmentVariable("DNS_RESOURCE_RG")!;
var certContact = Environment.GetEnvironmentVariable("ACME_CONTACT")!;
var secretsOfficersGroup = Environment.GetEnvironmentVariable("SECRETS_OFFICER_AAD_GROUP_OBJECTID");
var resourceGroup = $"rg-{azdEnvironment}";

var cred = new AzureCliCredential();

//check for the Resource group we expect azd to create
var azureLocation = new AzureLocation(location);
var armClient = new ArmClient(new AzureCliCredential());
var subscriptionResource = armClient
    .GetDefaultSubscription();

//check for KeyVault
var expectedKvName =$"kv-{azdEnvironment.Replace("-", "").Replace("_", "").ToLowerInvariant()}";

Console.WriteLine($"Checking for existing of Resource Group: {resourceGroup}");
var rg = subscriptionResource
    .GetResourceGroups()
    .CreateOrUpdate(WaitUntil.Completed, resourceGroup, 
        new ResourceGroupData(azureLocation)
        {
            Tags =
            {
                { "env-kv-name", expectedKvName}, 
                { "azd-env-name", azdEnvironment},
            }
        })
    .Value;

Console.WriteLine($"Looking for Dns Zone: {dnsResourceGroup}/{dnsSuffix}");
var dnsResource = subscriptionResource
    .GetResourceGroup(dnsResourceGroup)
    .Value.GetDnsZone(dnsSuffix).Value;

var tenantId = subscriptionResource.Get().Value.Data.TenantId!.Value;

Console.WriteLine($"Checking for KeyVault: {expectedKvName}");
var kv = rg.GetKeyVaults()
    .CreateOrUpdate(
        WaitUntil.Completed,
        expectedKvName,
        new KeyVaultCreateOrUpdateContent(
            azureLocation,
            new KeyVaultProperties(tenantId, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))
            {
                AccessPolicies = { new KeyVaultAccessPolicy(tenantId, secretsOfficersGroup, new IdentityAccessPermissions()
                {
                    Certificates = { IdentityAccessCertificatePermission.All },
                    Keys = { IdentityAccessKeyPermission.All },
                    Secrets = { IdentityAccessSecretPermission.All },
                    Storage = { IdentityAccessStoragePermission.All }
                }) }
            }
        )
    ).Value;

var client = new HttpClient();
var token = await cred.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com/" }));
var kvClient = new CertificateClient(kv.Data.Properties.VaultUri, cred);

var password = "p@ssw0rd";

try
{
    var cert = await kvClient.GetCertificateAsync(certificateName);
    Console.WriteLine("Certificate already exists");
    return;
}
catch (RequestFailedException re) when (re.ErrorCode == "CertificateNotFound")
{
    //OK - as expected
}


var productionLetsEncrypt = "https://acme-v02.api.letsencrypt.org/";

var http = new HttpClient() { BaseAddress = new Uri(productionLetsEncrypt) };

var acmeDir = default(ServiceDirectory);
var account = default(AccountDetails);
var accountSigner = default(IJwsTool);

var acme = new AcmeProtocolClient(http, acmeDir, account, accountSigner, usePostAsGet: true);

if (acmeDir == null)
{
    acmeDir = await acme.GetDirectoryAsync();
    acme.Directory = acmeDir;
}

await acme.GetNonceAsync();

if (account == null || accountSigner == null)
{
    account = await acme.CreateAccountAsync(new[] { certContact }.Select(x => "mailto:" + x), true);
    account.Payload.TermsOfServiceAgreed = true;
    accountSigner = acme.Signer;
    acme.Account = account;
    await acme.UpdateAccountAsync();
}
else
{
    account.Payload.TermsOfServiceAgreed = true;
    await acme.UpdateAccountAsync();
}

var dnsNames = new[] { $"{certificateName}.{dnsSuffix}", $"{certificateName}.scm.{dnsSuffix}" };
var order = await acme.CreateOrderAsync(dnsNames);

var attempts = 0;
foreach (var authorisation in order.Payload.Authorizations)
{
    var authDetails = await acme.GetAuthorizationDetailsAsync(authorisation);
    var dnsChallenge = authDetails.Challenges.Single(x => x.Type == "dns-01");
    var challenge = ResolveChallengeForDns01(authDetails, dnsChallenge, accountSigner);
    Console.WriteLine($"{challenge.DnsRecordName}  ::  {challenge.DnsRecordValue}");
    var req = new HttpRequestMessage(
        HttpMethod.Put,
        $"https://management.azure.com{dnsResource.Id}/{challenge.DnsRecordType}/{challenge.DnsRecordName[..^(dnsSuffix.Length + 1)]}?api-version=2018-05-01"
    );
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    var txtContent = JsonConvert.SerializeObject(new
    {
        properties = new
        {
            TTL = 60,
            TXTRecords =
                new[]
                {
                    new
                    {
                        value = new[]
                        {
                            challenge.DnsRecordValue
                        }
                    }
                }
        }
    });

    req.Content = new StringContent(txtContent, Encoding.UTF8, "application/json");
    var res = await client.SendAsync(req);
    res.EnsureSuccessStatusCode();

    //wait for the DNS to be there
    var url = $"https://dns.google.com/resolve?name={Uri.EscapeDataString(challenge.DnsRecordName)}&type=TXT";
    attempts = 0;
    do
    {
        attempts++;
        var googleDnsResponse = await client.GetFromJsonAsync<GoogleDnsResponse>(url);
        if (googleDnsResponse.Status == 0 &&
            googleDnsResponse.Answer.SingleOrDefault(x => x.Name == $"{challenge.DnsRecordName}.").Data ==
            challenge.DnsRecordValue)
        {
            Console.WriteLine("Google has reported correct DNS value");
            break;
        }

        if (attempts == 20) throw new InvalidOperationException("Failed to see DNS propagate");
        await Task.Delay(TimeSpan.FromSeconds(5));
    } while (true);

    await acme.AnswerChallengeAsync(dnsChallenge.Url);

    while (dnsChallenge.Status == "pending")
    {
        dnsChallenge = await acme.GetChallengeDetailsAsync(dnsChallenge.Url);
        if (dnsChallenge.Status == "valid") break;
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}

attempts = 0;
while (order.Payload.Status != "ready")
{
    attempts++;
    order = await acme.GetOrderDetailsAsync(order.OrderUrl, order);
    if (order.Payload.Status == "ready") break;
    await Task.Delay(TimeSpan.FromSeconds(5));
    if (attempts == 5) throw new InvalidOperationException("Failed to see DNS propagate");
    await Task.Delay(TimeSpan.FromSeconds(5));
}

var keyPair = RSA.Create();
if (order.Payload.Status == "ready") // && order.Payload.Certificate == null)
{
    var firstDns = dnsNames.First();
    var csr = new CertificateRequest($"CN={firstDns}", keyPair, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    var builder = new SubjectAlternativeNameBuilder();
    foreach (var dns in dnsNames) builder.AddDnsName(dns);
    csr.CertificateExtensions.Add(builder.Build());

    var csrDer = csr.CreateSigningRequest();

    order = await acme.FinalizeOrderAsync(order.Payload.Finalize, csrDer);
}

attempts = 0;
while (order.Payload.Status != "valid")
{
    attempts++;
    order = await acme.GetOrderDetailsAsync(order.OrderUrl, order);
    if (order.Payload.Status == "valid") break;
    if (attempts == 5) throw new InvalidOperationException("Failed to see DNS propagate");
    await Task.Delay(TimeSpan.FromSeconds(5));
}

if (order.Payload.Status == "valid")
{
    var cert = await acme.GetOrderCertificateAsync(order);
    var x509Cert = X509Certificate2.CreateFromPem(Encoding.UTF8.GetString(cert), keyPair.ExportRSAPrivateKeyPem());

    var certificate = x509Cert.Export(X509ContentType.Pfx, password);
    Console.WriteLine(password);

    var response = kvClient.ImportCertificate(
        new ImportCertificateOptions(
            certificateName,
            certificate)
        {
            Password = password
        });

    Console.WriteLine(response.Value.SecretId);
}
else
{
    throw new InvalidOperationException("Failed to authorise challenges");
}


Dns01ChallengeValidationDetails ResolveChallengeForDns01(
    ACMESharp.Protocol.Resources.Authorization authz, Challenge challenge, IJwsTool signer)
{
    var keyAuthzDigested = JwsHelper.ComputeKeyAuthorizationDigest(
        signer, challenge.Token);

    return new Dns01ChallengeValidationDetails
    {
        DnsRecordName = $@"{Dns01ChallengeValidationDetails.DnsRecordNamePrefix}.{
            authz.Identifier.Value}",
        DnsRecordType = Dns01ChallengeValidationDetails.DnsRecordTypeDefault,
        DnsRecordValue = keyAuthzDigested,
    };
}

public class GoogleDnsResponse
{
    public int Status { get; set; }
    public Answer[] Answer { get; set; }
}

public class Answer
{
    public string Name { get; set; }
    public string Data { get; set; }
}