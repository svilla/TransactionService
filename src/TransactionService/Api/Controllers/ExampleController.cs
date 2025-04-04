using Microsoft.AspNetCore.Mvc;
using TransactionService.Api.Controllers.Requests;
using TransactionService.Api.Controllers.Responses;

namespace TransactionService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    [HttpGet]
    public ActionResult<ExampleResponse> Get()
    {
        return Ok(new ExampleResponse { 
            Message = "TransactionService funcionando correctamente",
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