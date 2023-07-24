﻿using System.Text;

namespace EuroCertClient.Application.EuroCertSigner.Sign
{
  public class SignRequest
  {
    public string SignData { get; set; }
    public IFormFile? SourceFile { get; set; } = null;
  }

  public class SignData
  {
    public string EuroCertApiKey { get; set; } = string.Empty;
    public string EuroCertTaskId { get; set; } = string.Empty;
    public string? SignatureFieldName { get; set; } = null;
    public Appearance? Appearance { get; set; } = null;
  }

  public class Appearance
  {
    public int PageNumber { get; set; } = 0;
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
  }
}
