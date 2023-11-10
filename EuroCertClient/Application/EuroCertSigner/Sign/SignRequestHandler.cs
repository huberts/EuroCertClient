using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
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

      var temporaryFileName = Path.GetTempFileName();
      using var destinationFileStream = new FileStream(temporaryFileName, FileMode.Create);
      var stamper = PdfStamper.CreateSignature(
        new PdfReader(request.SourceFile.OpenReadStream()),
        destinationFileStream,
        '\0', null, true);
      PrepareAppearance(stamper.SignatureAppearance, signData);
      if (DebugMode_PKSigner)
      {
        SignUsingPrivateKey(stamper.SignatureAppearance);
      }
      else
      {
        MakeSignature.SignDetached(
          stamper.SignatureAppearance,
          new EuroCertSignature(EuroCertAddress, signData.EuroCertApiKey, signData.EuroCertTaskId),
          Chain, null, null, null, 0, CryptoStandard.CADES);
      }
      return Task.FromResult(temporaryFileName);
    }

    private void SignUsingPrivateKey(PdfSignatureAppearance signatureAppearance)
    {
      Pkcs12Store pfxKeyStore = new(new FileStream(DebugMode_PKPath, FileMode.Open, FileAccess.Read), DebugMode_PKPass.ToCharArray());
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

    private void PrepareAppearance(PdfSignatureAppearance appearance, SignData signData)
    {
      if (signData.Appearance is null)
      {
        return;
      }
      appearance.SetVisibleSignature(BuildBBOX(signData.Appearance), signData.Appearance.PageNumber, signData.SignatureFieldName);
      appearance.Layer2Text = "";
      appearance.Image = Image.GetInstance(new StampGenerator(LogoFilePath).Stamp());
    }

    private Rectangle BuildBBOX(Appearance appearance)
    {
      float ratio = (float)StampGenerator.Height / (float)StampGenerator.Width;
      if (appearance.Width * ratio < appearance.Height)
      {

        var width = appearance.Width;
        var height = appearance.Width * ratio;
        var x = appearance.X;
        var y = appearance.Y + (appearance.Height - height) / 2;
        return new(x + 1, y + 1, x + width - 1, y + height - 1);
      }
      else
      {
        var width = appearance.Height / ratio;
        var height = appearance.Height;
        var x = appearance.X + (appearance.Width - width) / 2;
        var y = appearance.Y;
        return new(x + 1, y + 1, x + width - 1, y + height - 1);
      }
    }
  }
}
