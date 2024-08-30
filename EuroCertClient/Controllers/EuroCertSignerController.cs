using EuroCertClient.Application.EuroCertSigner.Sign;
using Microsoft.AspNetCore.Mvc;

namespace EuroCertClient.Controllers
{
  [ApiController]
  [Route("EuroCertSigner")]
  public class EuroCertSignerController : ControllerBase
  {
    private readonly ILogger<EuroCertSignerController> _logger;
    private readonly SignRequestHandler _signRequestHandler;

    public EuroCertSignerController(ILogger<EuroCertSignerController> logger, SignRequestHandler signRequestHandler)
    {
      _logger = logger;
      _signRequestHandler = signRequestHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Sign([FromForm] SignRequest request)
    {
      if (request == null || request.SourceFile == null)
      {
        return BadRequest();
      }
      
      try
      {
        var destinationFileName = await _signRequestHandler.Handle(request, _logger);
        var sourceFileName = request.SourceFile?.FileName ?? "";
        _logger.LogInformation($"Signed: {sourceFileName}");
        var responseFileName = $"{Path.GetFileNameWithoutExtension(sourceFileName)}_signed{Path.GetExtension(sourceFileName)}";
        var destFile = new FileStream(destinationFileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
        return File(destFile, "application/octet-stream", responseFileName);
      }
      catch (Exception e)
      {
        _logger.LogError($"EuroCertException: not signed: {request.SourceFile?.FileName} -> {e.Message}");
        return StatusCode(StatusCodes.Status500InternalServerError, $"EuroCert error: {e.Message}");
      }
    }
  }
}
