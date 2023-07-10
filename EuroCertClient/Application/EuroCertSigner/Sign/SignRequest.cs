using System.Text;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class SignRequest
  {
    public string EuroCertApiKey { get; set; } = string.Empty;
    public string EuroCertTaskId { get; set; } = string.Empty;
    public IFormFile? SourceFile { get; set; } = null;
    public string? SignatureFieldName { get; set; } = null;
    public Appearance? Appearance { get; set; } = null;
  }

  public class Appearance
  {
    public int PageNumber { get; set; } = 0;
    public List<int> Rectangle { get; set; } = new List<int>() { 0, 0, 0, 0 };
    public string Reason { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
  }
}
