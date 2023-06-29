using EuroCertClient.Application.EuroCertSigner.Sign;
using Microsoft.AspNetCore.Mvc;

namespace EuroCertClient.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class EuroCertSignerController : ControllerBase
  {

    private readonly ILogger<EuroCertSignerController> _logger;

    public EuroCertSignerController(ILogger<EuroCertSignerController> logger)
    {
      _logger = logger;
    }

    [HttpGet]
    public async Task Sign(SignRequest request)
    {
      await new SignRequestHandler().Handle(request);
    }
  }
}
