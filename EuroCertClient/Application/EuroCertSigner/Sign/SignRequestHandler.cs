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
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using Newtonsoft.Json;
using iText.IO.Image;

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

      SignData? signData = JsonConvert.DeserializeObject<SignData>(request.SignData);
      if (string.IsNullOrEmpty(ServiceApiKey) 
        || string.IsNullOrEmpty(signData?.ServiceApiKey) 
        || !signData.ServiceApiKey.Equals(ServiceApiKey))
      {
        throw new ArgumentException("ServiceApiKey invalid.");
      }
      var temporaryFileName = System.IO.Path.GetTempFileName();
      using var destinationFileStream = new FileStream(temporaryFileName, FileMode.Create);
      var signer = new PdfSigner(
        new PdfReader(request.SourceFile.OpenReadStream()),
        destinationFileStream,
        new StampingProperties());
      PrepareAppearance(signer.GetDocument(), signer.GetSignatureAppearance(), signData.Appearance, Chain[0]);
      signer.SetFieldName(signData.SignatureFieldName);
      signer.SignDetached(
        new EuroCertSignature(EuroCertAddress, signData.EuroCertApiKey, signData.EuroCertTaskId),
        Chain, null, null, null, 0, PdfSigner.CryptoStandard.CADES);
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
    private string EuroCertAddress
    {
      get => Configuration["EuroCert:Address"]?.ToString() ?? "";
    }
    private string LogoFilePath
    {
      get => Configuration["EuroCert:Logo"]?.ToString() ?? "";
    }
    private string ServiceApiKey
    {
      get => Configuration["Eurocert:WebServiceApiKey"]?.ToString() ?? "";
    }

    private void PrepareAppearance(PdfDocument document, PdfSignatureAppearance appearance,
      Appearance? request, IX509Certificate iX509Certificate)
    {
      if (request is null)
        return;

      //ImageData logoImg = ImageDataFactory.Create(LogoFilePath);
      Rectangle BBox = new(
          request.X,
          request.Y,
          request.Width,
          request.Height);

      appearance
        .SetPageNumber(request.PageNumber)
        .SetPageRect(BBox);

      //string certCN = CertificateInfo.GetSubjectFields(iX509Certificate).GetField("CN");
      string certCN = "PREZYDENT MIASTA KRAKOWA";
      string date = $"Data: {DateTime.Now:yyyy-MM-dd HH:mm}";
      string footer = "(pieczęć elektroniczna)";

      PaintAppearanceOnCanvas(new PdfCanvas(appearance.GetLayer2(), document), BBox, certCN, date, footer);

    }

    private void PaintAppearanceOnCanvas(PdfCanvas canvas, Rectangle rect, string name, string date, string footer)
    {
      var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA, iText.IO.Font.PdfEncodings.CP1250);
      var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD, iText.IO.Font.PdfEncodings.CP1250);

      var size = CalculateFontSize();

      var nameText = new Text(name)
        .SetFont(bold)
        .SetFontColor(ColorConstants.BLUE)
        .SetFontSize(size);

      var dateText = new Text(date)
        .SetFont(regular)
        .SetFontSize(size);

      var footerText = new Text(footer)
        .SetFont(regular)
        .SetFontSize(size);

      new Canvas(canvas, new Rectangle(1, 1, rect.GetWidth() / 3 - 2, rect.GetHeight() - 2))
        .Add(new Image(ImageDataFactory.Create(LogoFilePath)).SetAutoScale(true))
        .Close();

      PaintTextCanvas(
        canvas,
        new Rectangle(rect.GetWidth() / 3, rect.GetHeight() / 2, rect.GetWidth() * 2 / 3, rect.GetHeight() / 2),
        name,
        bold,
        ColorConstants.BLUE,
        size);

      PaintTextCanvas(
        canvas,
        new Rectangle(rect.GetWidth() / 3, rect.GetHeight() * 3 / 8, rect.GetWidth() * 2 / 3, rect.GetHeight() / 4),
        date,
        regular,
        ColorConstants.BLACK,
        size);

      PaintTextCanvas(
        canvas,
        new Rectangle(rect.GetWidth() / 3, rect.GetHeight() / 8, rect.GetWidth() * 2 / 3, rect.GetHeight() / 4),
        footer,
        regular,
        ColorConstants.BLACK,
        size);
    }

    private void PaintTextCanvas(PdfCanvas canvas, Rectangle rect, string text, PdfFont font, Color color, float size)
    {
      new Canvas(canvas, rect).Add(
        new Paragraph(new Text(text)
        .SetFont(font)
        .SetFontColor(color)
        .SetFontSize(size)
        )
        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
        .SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER)
        .SetVerticalAlignment(iText.Layout.Properties.VerticalAlignment.MIDDLE)
        ).Close();
    }

    private float CalculateFontSize()
    {
      return 4;
    }
  }
}
