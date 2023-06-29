using iText.Kernel.Exceptions;
using iText.Signatures;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class EuroCertSignature : IExternalSignature
  {
    private readonly IConfiguration Configuration;

    public EuroCertSignature(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public string GetDigestAlgorithmName() => DigestAlgorithms.SHA256;

    public string GetSignatureAlgorithmName() => "RSA";

    public ISignatureMechanismParams? GetSignatureMechanismParameters() => null;

    public byte[] Sign(byte[] message)
    {
      var content = new StringContent(JsonConvert.SerializeObject(new
      {
        Algorithm = "SHA256",
        Hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(message)),
      }), Encoding.UTF8, "application/json");
      content.Headers.Add("API-KEY", ApiKey);

      var response = new HttpClient().PostAsync(Address, content).Result;

      var result = JsonConvert.DeserializeObject<EuroCertResponse>(response.Content.ReadAsStringAsync().Result)!;

      return Convert.FromBase64String(result.Signature);
    }

    private string Address
    {
      get => $"https://ecqss.eurocert.pl/api/rsa/sign/{TaskId}";
    }

    private string TaskId {
      get => Configuration["EuroCert:TaskId"]?.ToString() ?? "";
    }

    private string ApiKey {
      get => Configuration["EuroCert:ApiKey"]?.ToString() ?? "";
    }
  }

  class EuroCertResponse
  {
    public string Signature { get; set; } = string.Empty;
  }
}
