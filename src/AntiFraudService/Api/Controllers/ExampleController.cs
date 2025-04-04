using Microsoft.AspNetCore.Mvc;
using AntiFraudService.Api.Controllers.Requests;
using AntiFraudService.Api.Controllers.Responses;

namespace AntiFraudService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    [HttpGet]
    public ActionResult<ExampleResponse> Get()
    {
        return Ok(new ExampleResponse { 
            Message = "AntiFraudService funcionando correctamente",
            Success = true
        });
    }

    [HttpPost]
    public ActionResult<ExampleResponse> Post([FromBody] ExampleRequest request)
    {
        return Ok(new ExampleResponse { 
            Message = $"Datos recibidos: {request.Data}",
            Success = true
        });
    }
} 