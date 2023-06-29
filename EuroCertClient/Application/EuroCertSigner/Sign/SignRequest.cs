using System.Text;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class SignRequest
  {
    public string Base64EncodedSourceFilePath { get; set; } = string.Empty;
    public string Base64EncodedDestinationFilePath { get; set; } = string.Empty;
    public string SignatureFieldName = "Signed by EuroCert";
    public Apperance? Apperance { get; set; } = null;


    public string SourceFilePath
    {
      get => Encoding.UTF8.GetString(Convert.FromBase64String(Base64EncodedSourceFilePath));
    }
    public string DestinationFilePath
    {
      get => Encoding.UTF8.GetString(Convert.FromBase64String(Base64EncodedDestinationFilePath));
    }
  }

  public class Apperance
  {
    public int PageNumber { get; set; } = 0;
    public List<int> Rectangle { get; set; } = new List<int>() { 0, 0, 0, 0 };
    public string Reason { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
  }
}
