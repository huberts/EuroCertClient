using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using iText.Commons.Bouncycastle.Asn1.Esf;
using iText.Commons.Bouncycastle.Cert;
using iText.Forms.Form.Element;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Signatures;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Esf;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System.Drawing;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
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

      var signer = new PdfSigner(
          new PdfReader(srcFileStream).SetStrictnessLevel(PdfReader.StrictnessLevel.CONSERVATIVE),
          destFileStream,
          new StampingProperties().UseAppendMode());
      _logger.LogInformation($"PdfSigner created. PDF Version {signer.GetDocument().GetPdfVersion()}");
      
      PrepareAppearance(signer, signData, request.SignImage);
      _logger.LogInformation("Create signature stamper.");

      IExternalSignature externalSignature = GetExternalSignature(signData, _logger);
      _logger.LogInformation($"CreateExternalSignature {externalSignature}");

      IX509Certificate[] chain = GetChain();
      _logger.LogInformation($"Certificate loaded {chain.First().GetSubjectDN()}");

      signer.SignDetached(
          externalSignature, chain, null, null, null, 0, PdfSigner.CryptoStandard.CADES);
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
        return new PrivateKeySignature(new PrivateKeyBC(privateKey), DigestAlgorithms.SHA256);
      }

      if (CloudSignerName == "miPieczec")
        return new miPieczecSignature(EuroCertAddress, signData.EuroCertApiKey, logger);
      else
        return new EuroCertSignature(EuroCertAddress, signData.EuroCertApiKey, signData.EuroCertTaskId, logger);
    }

    private IX509Certificate[] GetChain()
    {
      X509Certificate2 cert = DebugMode_PKSigner
        ? new X509Certificate2(DebugMode_PKPath, DebugMode_PKPass)
        : new X509Certificate2(CertificateFilePath);
      
      return new IX509Certificate[]
      {
        new X509CertificateBC(new Org.BouncyCastle.X509.X509Certificate(cert.RawData))
      };
    }

    private void PrepareAppearance(PdfSigner signer, SignData signData, IFormFile? signImage)
    {
      if (signData.Appearance is null)
      {
        throw new ArgumentException("SignData.Appearance is required.");
      }

      if (signImage is null)
      {
        throw new ArgumentException("SignImage is required.");
      }

      var appearance = new SignatureFieldAppearance(signData.SignatureFieldName);
      appearance.SetPageNumber(signData.Appearance.PageNumber);

      using var ms = new MemoryStream();
      signImage.CopyTo(ms);
      appearance.SetContent(ImageDataFactory.Create(ms.ToArray()));

      signer.SetSignatureAppearance(appearance);
      signer.SetFieldName(signData.SignatureFieldName);
      signer.SetPageNumber(signData.Appearance.PageNumber);
      signer.SetReason(signData.Appearance.Reason);
      signer.SetLocation(signData.Appearance.Location);

      var rect = new iText.Kernel.Geom.Rectangle(
        signData.Appearance.X,
        signData.Appearance.Y,
        signData.Appearance.Width,
        signData.Appearance.Height);

      var page = signer.GetDocument().GetPage(signData.Appearance.PageNumber);
      if (page.GetRotation() == 270)
        rect = new iText.Kernel.Geom.Rectangle(
          rect.GetY(), 
          page.GetPageSize().GetHeight() - rect.GetX() - rect.GetWidth(), 
          rect.GetHeight(), 
          rect.GetWidth()
          );
      if (page.GetRotation() == 90)
        rect = new iText.Kernel.Geom.Rectangle(
          page.GetPageSize().GetWidth() - rect.GetY() - rect.GetHeight(),
          rect.GetX(),
          rect.GetHeight(),
          rect.GetWidth()
          );

      signer.SetPageRect(rect);
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
