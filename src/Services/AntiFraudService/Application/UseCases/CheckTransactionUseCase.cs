using AntiFraudService.Domain.Models;
using AntiFraudService.Domain.Ports.Input;
using AntiFraudService.Domain.Ports.Output;
using Microsoft.Extensions.Logging;

namespace AntiFraudService.Application.UseCases;

public class CheckTransactionUseCase : ICheckTransactionUseCase
{
    public CheckTransactionUseCase()
    {
       
    }

    public async Task ExecuteAsync(Transaction transaction)
    {
      
    }

} 