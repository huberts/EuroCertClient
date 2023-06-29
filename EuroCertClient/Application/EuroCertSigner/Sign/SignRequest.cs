using System.Text;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class SignRequest
  {
    public SignRequest(string base64EncodedSourceFilePath, string base64EncodedDestinationFilePath)
    {
      Base64EncodedSourceFilePath = base64EncodedSourceFilePath;
      Base64EncodedDestinationFilePath = base64EncodedDestinationFilePath;
    }

    public string Base64EncodedSourceFilePath { get; set; }
    public string Base64EncodedDestinationFilePath { get; set; }

    public string SourceFilePath { get
      {
        return Encoding.UTF8.GetString(Convert.FromBase64String(Base64EncodedSourceFilePath));
      }
    }
    public string DestinationFilePath { get
      {
        return Encoding.UTF8.GetString(Convert.FromBase64String(Base64EncodedDestinationFilePath));
      }
    }
  }
}
