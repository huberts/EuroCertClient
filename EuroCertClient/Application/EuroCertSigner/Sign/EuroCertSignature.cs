using iTextSharp.text.pdf.security;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class EuroCertSignature : IExternalSignature
  {
    private readonly string _address;
    private readonly string _apiKey;
    private readonly string _taskId;
    private readonly ILogger _logger;

    public EuroCertSignature(string address, string apiKey, string taskId, ILogger logger)
    {
      _address = address;
      _apiKey = apiKey;
      _taskId = taskId;
      _logger = logger;
      _logger.LogInformation("EuroCertSignature");
    }

    public string GetEncryptionAlgorithm() => "RSA";

    public string GetHashAlgorithm() => DigestAlgorithms.SHA256;

    public byte[] Sign(byte[] message)
    {
      string requestContent = JsonConvert.SerializeObject(new
      {
        Algorithm = "SHA256",
        Hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(message)),
      });
      _logger.LogInformation("Before Sign: " + requestContent);

      var content = new StringContent(requestContent, Encoding.UTF8, "application/json");
      content.Headers.Add("API-KEY", _apiKey);

      EuroCertResponse result = new EuroCertResponse();
      string address = $"{_address}/{_taskId}";
      try
      {
        var response = new HttpClient().PostAsync(address, content).Result;
        result = JsonConvert.DeserializeObject<EuroCertResponse>(response.Content.ReadAsStringAsync().Result)!;
      } 
      catch (Exception ex)
      {
        _logger.LogInformation(address);
        _logger.LogCritical(ex, "EuroCertResponse");
        throw new ArgumentException($"EuroCertResponse: {ex.Message}", ex);
      }

      if (result.Error != 0)
      {
        throw new ArgumentException($"EuroCertSignature After Post: Code<{result.Error}> {result.ErrorDescription}");
      }
      _logger.LogInformation($"After Sign: {result.Error}");

      return Convert.FromBase64String(result.Signature);
    }
  }
}
