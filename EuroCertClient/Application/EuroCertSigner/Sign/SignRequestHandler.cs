using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using Newtonsoft.Json;
using Org.BouncyCastle.X509;
using static iTextSharp.text.Font;

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
      var stamper = PdfStamper.CreateSignature(
        new PdfReader(request.SourceFile.OpenReadStream()),
        destinationFileStream,
        '\0', null, true);
      PrepareAppearance(/*stamper.GetDocument(),*/ stamper.SignatureAppearance, signData.Appearance, signData.SignatureFieldName, Chain[0]);
      MakeSignature.SignDetached(
        stamper.SignatureAppearance,
        new EuroCertSignature(EuroCertAddress, signData.EuroCertApiKey, signData.EuroCertTaskId),
        Chain, null, null, null, 0, CryptoStandard.CADES);
      return Task.FromResult(temporaryFileName);
    }

    private X509Certificate[] Chain
    {
      get
      {
        using var certificate = File.Open(CertificateFilePath, FileMode.Open);
        return new X509Certificate[]
          {
            new X509CertificateParser().ReadCertificate(certificate)
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

    private void PrepareAppearance(/*PdfDocument document,*/ PdfSignatureAppearance appearance,
      Appearance? request, string fieldName, X509Certificate iX509Certificate)
    {
      if (request is null)
        return;

      Rectangle BBox = new(
          request.X,
          request.Y,
          request.Width,
          request.Height);

      appearance.SetVisibleSignature(BBox, request.PageNumber, fieldName);

      string certCN = "PREZYDENT MIASTA KRAKOWA";
      string date = $"Data: {DateTime.Now:yyyy-MM-dd HH:mm}";
      string footer = "(pieczęć elektroniczna)";

      PaintAppearanceOnTemplate(appearance.GetLayer(2), BBox, certCN, date, footer);

    }

    private void PaintAppearanceOnTemplate(PdfTemplate template, Rectangle rect, string name, string date, string footer)
    {
      var regular = BaseFont.CreateFont(BaseFont.HELVETICA, "Cp1250", BaseFont.EMBEDDED);
      var bold = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, "Cp1250", BaseFont.EMBEDDED);

      var size = CalculateFontSize();

      var nameText = new Text(name)
        .SetFont(bold)
        .SetFontColor(BaseColor.BLUE)
        .SetFontSize(size);

      var dateText = new Text(date)
        .SetFont(regular)
        .SetFontSize(size);

      var footerText = new Text(footer)
        .SetFont(regular)
        .SetFontSize(size);



      new Canvas(canvas, new Rectangle(1, 1, rect.Width / 3 - 2, rect.Height - 2))
        .Add(new Image(ImageDataFactory.Create(LogoFilePath)).SetAutoScale(true))
        .Close();

      PaintTextCanvas(
        canvas,
        new Rectangle(rect.Width / 3, rect.Height / 2, rect.Width * 2 / 3, rect.Height / 2),
        name,
        bold,
        BaseColor.BLUE,
        size);

      PaintTextCanvas(
        canvas,
        new Rectangle(rect.Width / 3, rect.Height * 3 / 8, rect.Width * 2 / 3, rect.Height / 4),
        date,
        regular,
        BaseColor.BLACK,
        size);

      PaintTextCanvas(
        canvas,
        new Rectangle(rect.Width / 3, rect.Height / 8, rect.Width * 2 / 3, rect.Height / 4),
        footer,
        regular,
        BaseColor.BLACK,
        size);
    }

    private void PaintTextCanvas(PdfCanvas canvas, Rectangle rect, string text, PdfFont font, BaseColor color, float size)
    {
      new Canvas(canvas, rect).Add(
        new Paragraph(new Text(text)
        .SetFont(font)
        .SetFontColor(color)
        .SetFontSize(size)
        )
        .SetTextAlignment(Element.ALIGN_CENTER)
        .SetHorizontalAlignment(Element.ALIGN_CENTER)
        .SetVerticalAlignment(Element.ALIGN_MIDDLE)
        ).Close();
    }

    private float CalculateFontSize()
    {
      return 4;
    }
  }
}
