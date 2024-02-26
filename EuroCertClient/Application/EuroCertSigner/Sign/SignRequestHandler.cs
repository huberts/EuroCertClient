using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;

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
      SignData? signData = JsonConvert.DeserializeObject<SignData>(request.SignData);
      if (string.IsNullOrEmpty(ServiceApiKey)
        || string.IsNullOrEmpty(signData?.ServiceApiKey)
        || !signData.ServiceApiKey.Equals(ServiceApiKey))
      {
        throw new ArgumentException("ServiceApiKey invalid.");
      }
      _logger.LogInformation("Request authorized.");
      _logger.LogInformation($"SignImage: {request.SignImage?.Name ?? "No image."}");

      var temporaryFileName = Path.GetTempFileName();
      using var destinationFileStream = new FileStream(temporaryFileName, FileMode.Create);

      var stamper = PdfStamper.CreateSignature(
        new PdfReader(request.SourceFile?.OpenReadStream()),
        destinationFileStream,
        '\0', null, true);
      PrepareAppearance(stamper.SignatureAppearance, signData, request.SignImage);
      _logger.LogInformation("Create signature stamper.");

      IExternalSignature externalSignature = GetExternalSignature(signData, _logger);

      if (DebugMode_PKSigner)
      {
        SignUsingPrivateKey(stamper.SignatureAppearance);
      }
      else
      {
        MakeSignature.SignDetached(
          stamper.SignatureAppearance,
          externalSignature,
          Chain, null, null, null, 0, CryptoStandard.CADES);
      }
      _logger.LogInformation("MakeSignature.SignDetached");
      
      return Task.FromResult(temporaryFileName);
    }

    private IExternalSignature GetExternalSignature(SignData signData, ILogger logger)
    {
      if (CloudSignerName == "miPieczec")
        return new miPieczecSignature(EuroCertAddress, signData.EuroCertApiKey, logger);
      else
        return new EuroCertSignature(EuroCertAddress, signData.EuroCertApiKey, signData.EuroCertTaskId, logger);
    }

    private void SignUsingPrivateKey(PdfSignatureAppearance signatureAppearance)
    {
      using var cert = new FileStream(DebugMode_PKPath, FileMode.Open, FileAccess.Read);
      Pkcs12Store pfxKeyStore = new(cert, DebugMode_PKPass.ToCharArray());
      string alias = pfxKeyStore.Aliases.Cast<string>().FirstOrDefault(entryAlias => pfxKeyStore.IsKeyEntry(entryAlias));
      if (alias != null)
      {
        ICipherParameters privateKey = pfxKeyStore.GetKey(alias).Key;
        IExternalSignature pks = new PrivateKeySignature(privateKey, DigestAlgorithms.SHA256);
        MakeSignature.SignDetached(signatureAppearance, pks, new X509Certificate[] { pfxKeyStore.GetCertificate(alias).Certificate }, null, null, null, 0, CryptoStandard.CADES);
      }
      else
      {
        Console.WriteLine("Private key not found in the PFX certificate.");
      }
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

    private void PrepareAppearance(PdfSignatureAppearance appearance, SignData signData, IFormFile? signImage)
    {
      if (signData.Appearance is null)
      {
        return;
      }

      if (signImage is null)
      {
        var stamp = new StampGenerator();
        appearance.SetVisibleSignature(stamp.BuildBBOX(signData.Appearance), signData.Appearance.PageNumber, signData.SignatureFieldName);
        appearance.Image = Image.GetInstance(stamp.Stamp(LogoFilePath));
      }
      else
      {
        Rectangle pageRect = new(
          signData.Appearance.X,
          signData.Appearance.Y,
          signData.Appearance.X + signData.Appearance.Width,
          signData.Appearance.Y + signData.Appearance.Height);
        appearance.SetVisibleSignature(pageRect, signData.Appearance.PageNumber, signData.SignatureFieldName);
        appearance.Image = Image.GetInstance(signImage.OpenReadStream());
      }
      appearance.Layer2Text = "";
    }
  }
}
