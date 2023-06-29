using System.Text;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class SignRequest
  {
    public string Base64EncodedSourceFilePath { get; set; } = string.Empty;
    public string Base64EncodedDestinationFilePath { get; set; } = string.Empty;
    public string SignatureFieldName { get; set; } = "Signed by EuroCert";
    public Appearance? Appearance { get; set; } = null;


    public string SourceFilePath
    {
      get => Encoding.UTF8.GetString(Convert.FromBase64String(Base64EncodedSourceFilePath));
    }
    public string DestinationFilePath
    {
      get => Encoding.UTF8.GetString(Convert.FromBase64String(Base64EncodedDestinationFilePath));
    }
  }

  public class Appearance
  {
    public int PageNumber { get; set; } = 0;
    public List<int> Rectangle { get; set; } = new List<int>() { 0, 0, 0, 0 };
    public string Reason { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
  }
}
