using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using Newtonsoft.Json;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class SignRequestHandler
  {
    private readonly IConfiguration Configuration;

    public SignRequestHandler(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public Task<string> Handle(SignRequest request, ILogger _logger)
    {
      _logger.LogInformation("SignRequestHandler");
      SignData? signData = JsonConvert.DeserializeObject<SignData>(request.SignData);
      if (string.IsNullOrEmpty(ServiceApiKey))
        throw new ArgumentException("No ServiceApiKey in configuration.");
      if (string.IsNullOrEmpty(signData?.ServiceApiKey))
        throw new ArgumentException("ServiceApiKey is empty.");
      if (!signData.ServiceApiKey.Equals(ServiceApiKey))
        throw new ArgumentException("ServiceApiKey invalid.");

      _logger.LogInformation("Request authorized.");
      _logger.LogInformation($"SignImage: {request.SignImage?.Name ?? "No image."}");

      using var srcFileStream = request.SourceFile?.OpenReadStream();
      using var destFileStream = new FileStream(Path.GetTempFileName(), FileMode.Create);
      _logger.LogInformation($"Destination file created: {destFileStream.Name}");

      var pdfReader = new PdfReader(srcFileStream);
      var stamper = PdfStamper.CreateSignature(
        pdfReader, destFileStream, '\0', null, true
        );
      _logger.LogInformation($"PdfSigner created. PDF Version {pdfReader.PdfVersion}");

      PrepareAppearance(stamper.SignatureAppearance, signData, request.SignImage, pdfReader);
      _logger.LogInformation("Create signature stamper.");

      IExternalSignature externalSignature = GetExternalSignature(signData, _logger);
      _logger.LogInformation($"CreateExternalSignature {externalSignature}");

      var chain = GetChain();
      _logger.LogInformation($"Certificate loaded {chain.First().SubjectDN}");

      MakeSignature.SignDetached(
        stamper.SignatureAppearance,
        externalSignature,
        chain, null, null, null, 0, CryptoStandard.CADES);
      _logger.LogInformation("MakeSignature.SignDetached");

      return Task.FromResult(destFileStream.Name);
    }

    private IExternalSignature GetExternalSignature(SignData signData, ILogger logger)
    {
      if (DebugMode_PKSigner)
      {
        using var cert = new FileStream(DebugMode_PKPath, FileMode.Open, FileAccess.Read);
        Pkcs12Store pfxKeyStore = new Pkcs12StoreBuilder().Build();
        pfxKeyStore.Load(cert, DebugMode_PKPass.ToCharArray());
        string alias = pfxKeyStore.Aliases.Cast<string>().FirstOrDefault(pfxKeyStore.IsKeyEntry);
        if (alias == null)
          throw new ArgumentException("Private key not found in the PFX certificate.");

        ICipherParameters privateKey = pfxKeyStore.GetKey(alias).Key;
        return new PrivateKeySignature(privateKey, DigestAlgorithms.SHA256);
      }

      if (CloudSignerName == "miPieczec")
        return new miPieczecSignature(EuroCertAddress, signData.EuroCertApiKey, logger);
      else
        return new EuroCertSignature(EuroCertAddress, signData.EuroCertApiKey, signData.EuroCertTaskId, logger);
    }

    private Org.BouncyCastle.X509.X509Certificate[] GetChain()
    {
      X509Certificate2 cert = DebugMode_PKSigner
        ? new X509Certificate2(DebugMode_PKPath, DebugMode_PKPass)
        : new X509Certificate2(CertificateFilePath);
      
      return [ new Org.BouncyCastle.X509.X509Certificate(cert.RawData) ];
    }

    private void PrepareAppearance(PdfSignatureAppearance appearance, SignData signData, IFormFile? signImage, PdfReader pdfReader)
    {
      if (signData.Appearance is null)
      {
        throw new ArgumentException("SignData.Appearance is required.");
      }

      if (signImage is null)
      {
        throw new ArgumentException("SignImage is required.");
      }

      using var ms = new MemoryStream();
      signImage.CopyTo(ms);
      ms.Position = 0;
      appearance.SignatureGraphic = iTextSharp.text.Image.GetInstance(ms);
      appearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.GRAPHIC;
      appearance.Layer2Text = "";
      appearance.Reason = signData.Appearance.Reason;
      appearance.Location = signData.Appearance.Location;

      var rect = new RectangleF(
        signData.Appearance.X,
        signData.Appearance.Y,
        signData.Appearance.Width,
        signData.Appearance.Height
        );

      //var pageRotation = pdfReader.GetPageRotation(signData.Appearance.PageNumber);
      //var pageSize = pdfReader.GetPageSize(signData.Appearance.PageNumber);
      //if (pageRotation == 270)
      //{
      //  rect = new RectangleF(
      //    rect.Y,
      //    pageSize.Height - rect.X - rect.Width,
      //    rect.Height,
      //    rect.Width
      //    );
      //}
      //else if (pageRotation == 90)
      //{
      //  rect = new RectangleF(
      //    pageSize.Width - rect.Y - rect.Height,
      //    rect.X,
      //    rect.Height,
      //    rect.Width
      //    );
      //}
      
      appearance.SetVisibleSignature(
        RectangleFToRectangle(rect),
        signData.Appearance.PageNumber,
        signData.SignatureFieldName
        );
    }

    private iTextSharp.text.Rectangle RectangleFToRectangle(RectangleF rect)
    {
      return new iTextSharp.text.Rectangle(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
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
    private string CloudSignerName
    {
      get => Configuration["Eurocert:CloudSignerName"]?.ToString() ?? "";
    }
    private bool DebugMode_PKSigner
    {
      get => Configuration.GetValue<bool>("DebugMode:PKSigner");
    }
    private string DebugMode_PKPath
    {
      get => Configuration["DebugMode:PKPath"]?.ToString() ?? "";
    }
    private string DebugMode_PKPass
    {
      get => Configuration["DebugMode:PKPass"]?.ToString() ?? "";
    }
  }
}
