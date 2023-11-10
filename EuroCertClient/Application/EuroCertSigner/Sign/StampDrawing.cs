using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;


namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class StampDrawing
  {
    private readonly string LogoFilePath;
    public static readonly int Width = 520;
    public static readonly int Height = 163;

    public StampDrawing(string logoFilePath) 
    {
      LogoFilePath = logoFilePath;
    }

    public string Stamp()
    {
      Image image = Image.FromFile(LogoFilePath);

      Bitmap bitmap = new(Width, Height);
      Graphics graphics = Graphics.FromImage(bitmap);
      
      graphics.Clear(Color.White);
      graphics.DrawImage(image, new Point(0, 0));

      var krakowBlue = new SolidBrush(Color.FromArgb(0, 108, 183));

      graphics.DrawString("PREZYDENT MIASTA KRAKOWA", new Font("Helvetica", 17, FontStyle.Bold), krakowBlue, new Point(145, 35));
      graphics.DrawString($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", new Font("Helvetica", 12, FontStyle.Regular), Brushes.Black, new Point(220, 80));
      graphics.DrawString("(pieczęć elektroniczna)", new Font("Helvetica", 12, FontStyle.Regular), Brushes.Black, new Point(230, 100));

      var temporaryFileName = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
      bitmap.Save(temporaryFileName, ImageFormat.Png);

      return temporaryFileName;
    }
  }
}
