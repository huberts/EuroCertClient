using EuroCertClient.Application.EuroCertSigner.Sign;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mime;

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
      try
      {
        var destinationFileName = await _signRequestHandler.Handle(request);
        var sourceFileName = request.SourceFile?.FileName ?? "";
        _logger.LogInformation($"Signed: {sourceFileName}");
        var responseFileName = $"{Path.GetFileNameWithoutExtension(sourceFileName)}_signed{Path.GetExtension(sourceFileName)}";
        return File(new FileStream(destinationFileName, FileMode.Open), "application/octet-stream", responseFileName);
      }
      catch (ArgumentNullException)
      {
        return BadRequest("No source file defined.");
      }
      catch (EuroCertException e)
      {
        _logger.LogError($"EuroCertException: not signed: {request.SourceFile?.FileName} -> {e.Code}: {e.Message}");
        return StatusCode(StatusCodes.Status500InternalServerError, $"EuroCert error: {e.Code}");
      }
    }
  }
}
