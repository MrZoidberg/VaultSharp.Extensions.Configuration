namespace SampleWebApp.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Options;

[Route("hello")]
[ApiController]
public class HelloController
{
    [HttpGet("")]
    public IActionResult GetValue([FromServices] IOptionsSnapshot<TestOptions> testOptions) => new OkObjectResult(testOptions.Value);
}
