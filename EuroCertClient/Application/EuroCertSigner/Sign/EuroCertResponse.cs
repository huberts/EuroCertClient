using Newtonsoft.Json;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class EuroCertResponse
  {
    public int Error { get; set; } = 0;
    [JsonProperty("error_description")]
    public string ErrorDescription { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
  }
}
