namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class miPieczecRequest
  {
    public string Algorithm { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string ApiKeyBase64 { get; set; } = string.Empty;
    public override string ToString()
    {
      return $"Algorithm: {Algorithm}, ApiKey: Hidden, Hash: {Hash}";
    }
  }
}
