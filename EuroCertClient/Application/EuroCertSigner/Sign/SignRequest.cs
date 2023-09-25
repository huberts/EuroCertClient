using System.Text;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class SignRequest
  {
    public string SignData { get; set; }
    public IFormFile? SourceFile { get; set; } = null;
  }

  public class SignData
  {
    public string EuroCertApiKeyBase64 { get; set; } = string.Empty;
    public string EuroCertApiKey { get; set; } = string.Empty;
    public string ec_ApiKey { 
      get
      {
        return string.IsNullOrEmpty(EuroCertApiKeyBase64)
          ? Encoding.UTF8.GetString(Convert.FromBase64String(EuroCertApiKey))
          : Encoding.UTF8.GetString(Convert.FromBase64String(EuroCertApiKeyBase64));
      }
    }
    public string EuroCertTaskId { get; set; } = string.Empty;
    public string? SignatureFieldName { get; set; } = null;
    public Appearance? Appearance { get; set; } = null;
    public string ServiceApiKey { get; set; } = string.Empty;
  }

  public class Appearance
  {
    public int PageNumber { get; set; } = 0;
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
  }
}
