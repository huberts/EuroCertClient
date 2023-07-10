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
    private readonly string _address;
    private readonly string _apiKey;
    private readonly string _taskId;

    public EuroCertSignature(string address, string apiKey, string taskId)
    {
      _address = address;
      _apiKey = apiKey;
      _taskId = taskId;
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
      content.Headers.Add("API-KEY", _apiKey);

      var response = new HttpClient().PostAsync($"{_address}/{_taskId}", content).Result;

      var result = JsonConvert.DeserializeObject<EuroCertResponse>(response.Content.ReadAsStringAsync().Result)!;
      if (result.Error != 0)
      {
        throw new EuroCertException(result.Error, result.ErrorDescription);
      }
      return Convert.FromBase64String(result.Signature);
    }
  }
}
