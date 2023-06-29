using iText.Commons.Bouncycastle.Cert;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.X509;
using iText.Bouncycastle.X509;
using iText.Kernel.Geom;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class SignRequestHandler
  {
    private readonly IConfiguration Configuration;
    private readonly EuroCertSignature EuroCertSignature;

    public SignRequestHandler(IConfiguration configuration, EuroCertSignature euroCertSignature)
    {
      Configuration = configuration;
      EuroCertSignature = euroCertSignature;
    }

    public Task Handle(SignRequest request)
    {
      var signer = new PdfSigner(
        new PdfReader(request.SourceFilePath),
        new FileStream(request.DestinationFilePath, FileMode.Create),
        new StampingProperties());
      if (request.Apperance is not null)
      {
        signer.GetSignatureAppearance()
          .SetPageNumber(request.Apperance.PageNumber)
          .SetPageRect(new Rectangle(
            request.Apperance.Rectangle.ElementAt(0),
            request.Apperance.Rectangle.ElementAt(1),
            request.Apperance.Rectangle.ElementAt(2),
            request.Apperance.Rectangle.ElementAt(3)))
          .SetReason(request.Apperance.Reason)
          .SetLocation(request.Apperance.Location);
      }
      signer.SetFieldName(request.SignatureFieldName);
      signer.SignDetached(EuroCertSignature, Chain, null, null, null, 0, PdfSigner.CryptoStandard.CADES);
      return Task.CompletedTask;
    }

    private IX509Certificate[] Chain
    {
      get => new IX509Certificate[1]
        {
          new X509CertificateBC(new X509CertificateParser().ReadCertificate(File.Open(CertificateFilePath, FileMode.Open)))
        };
    }

    private string CertificateFilePath
    {
      get => Configuration["EuroCert:CertificateFilePath"]?.ToString() ?? "";
    }
  }
}
