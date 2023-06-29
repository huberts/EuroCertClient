namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class EuroCertRequest
  {
    public string Algorithm { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
  }
}
