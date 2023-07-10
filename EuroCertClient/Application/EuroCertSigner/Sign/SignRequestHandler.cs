using iText.Commons.Bouncycastle.Cert;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.X509;
using iText.Bouncycastle.X509;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Colors;
using iText.Layout.Element;
using iText.Layout;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class SignRequestHandler
  {
    private readonly IConfiguration Configuration;

    public SignRequestHandler(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public Task<string> Handle(SignRequest request)
    {
      if (request.SourceFile is null)
      {
        throw new ArgumentNullException("SourceFile not found!");
      }
      var temporaryFileName = System.IO.Path.GetTempFileName();
      using var destinationFileStream = new FileStream(temporaryFileName, FileMode.Create);
      var signer = new PdfSigner(
        new PdfReader(request.SourceFile.OpenReadStream()),
        destinationFileStream,
        new StampingProperties());
      PrepareAppearance(signer.GetDocument(), signer.GetSignatureAppearance(), request.Appearance);
      signer.SetFieldName(request.SignatureFieldName);
      signer.SignDetached(new EuroCertSignature(EuroCertAddress, request.EuroCertApiKey, request.EuroCertTaskId), Chain, null, null, null, 0, PdfSigner.CryptoStandard.CADES);
      return Task.FromResult(temporaryFileName);
    }

    private IX509Certificate[] Chain
    {
      get
      {
        using var certificate = File.Open(CertificateFilePath, FileMode.Open);
        return new IX509Certificate[1]
          {
            new X509CertificateBC(new X509CertificateParser().ReadCertificate(certificate))
          };
      }
    }

    private string CertificateFilePath
    {
      get => Configuration["EuroCert:CertificateFilePath"]?.ToString() ?? "";
    }

    private void PrepareAppearance(PdfDocument document, PdfSignatureAppearance appearance, Appearance? request)
    {
      if (request is not null)
      {
        appearance
          .SetPageNumber(request.PageNumber)
          .SetPageRect(new Rectangle(
            request.Rectangle.ElementAt(0),
            request.Rectangle.ElementAt(1),
            request.Rectangle.ElementAt(2),
            request.Rectangle.ElementAt(3)))
          .SetReason(request.Reason)
          .SetLocation(request.Location);
      }
      var background = appearance.GetLayer0();
      float x = background.GetBBox().ToRectangle().GetLeft();
      float y = background.GetBBox().ToRectangle().GetBottom();
      float width = background.GetBBox().ToRectangle().GetWidth();
      float height = background.GetBBox().ToRectangle().GetHeight();
      var canvas = new PdfCanvas(background, document);
      canvas.SetFillColor(ColorConstants.YELLOW);
      canvas.Rectangle(x, y, width, height);
      canvas.Fill();

      var content = appearance.GetLayer2();
      var p = new Paragraph("=== Podpisano poprawnie ===");
      p.SetFontColor(ColorConstants.BLUE);
      new Canvas(content, document).Add(p);
    }

    private string EuroCertAddress
    {
      get => Configuration["EuroCert:Address"]?.ToString() ?? "";
    }
  }
}
