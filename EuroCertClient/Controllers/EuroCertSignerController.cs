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
    public async Task Sign(SignRequest request)
    {
      await _signRequestHandler.Handle(request);
    }
  }
}
