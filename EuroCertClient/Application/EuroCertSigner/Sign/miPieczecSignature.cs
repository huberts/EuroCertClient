using iTextSharp.text.pdf.security;
using Newtonsoft.Json;
using System.Text;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class miPieczecSignature : IExternalSignature
  {
    private readonly string _address;
    private readonly string _apiKey;
    private readonly ILogger _logger;

    public miPieczecSignature(string address, string apiKey, ILogger logger) 
    {
      _address = address;
      _apiKey = apiKey;
      _logger = logger;
      _logger.LogInformation("miPieczecSignature");
    }

    public string GetEncryptionAlgorithm() => "RSA";

    public string GetHashAlgorithm() => DigestAlgorithms.SHA256;

    public byte[] Sign(byte[] message)
    {
      var request = new miPieczecRequest
      {
        Algorithm = "SHA-256",
        Hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(message)),
        ApiKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(_apiKey))
      };
      _logger.LogInformation("Before Sign: " + request.ToString());      

      var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
      string result = string.Empty;

      try
      {
        using var client = new HttpClient();
        var response = client.PostAsync(_address, content).Result;
        if (response.IsSuccessStatusCode)
        {
          result = response.Content.ReadAsStringAsync().Result!;
        }
        else
        {
          throw new ArgumentException($"StatusCode: {response.StatusCode} ReasonPhrase: {response.ReasonPhrase}");
        }
      }
      catch (Exception ex)
      {
        _logger.LogInformation(_address);
        _logger.LogCritical(ex, "miPieczecResponse");
        throw new ArgumentException($"miPieczecResponse: {ex.Message}", ex);
      }

      _logger.LogInformation("After Sign");
      return Convert.FromBase64String(result);
    }
  }
}
