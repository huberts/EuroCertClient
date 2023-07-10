namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class EuroCertException : Exception
  {
    public EuroCertException(int code, string message) : base(message)
    {
      Code = code;
    }

    public int Code { get; }
  }
}
