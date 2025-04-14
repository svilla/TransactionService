-- Active: 1744145422339@@127.0.0.1@5432
using AntiFraudService.Application.UseCases;
using AntiFraudService.Domain.Models;
using AntiFraudService.Domain.Ports.Input;
using AntiFraudService.Domain.Ports.Output;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AntiFraudService.Application.Tests;

public class CheckTransactionUseCaseTests
{
    private readonly Mock<IDailyAccumulatedTransactionRepository> _mockRepository;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<CheckTransactionUseCase>> _mockLogger;
    private readonly CheckTransactionUseCase _useCase;

    public CheckTransactionUseCaseTests()
    {
        _mockRepository = new Mock<IDailyAccumulatedTransactionRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<CheckTransactionUseCase>>(); 

        _useCase = new CheckTransactionUseCase(
            _mockRepository.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectTransaction_WhenAmountExceedsIndividualLimit()
    {
        // Arrange
        var transactionAmount = TransactionAmount.Create(1001).Value; 
        var transaction = Transaction.CreateNew("account-1", "account-2", transactionAmount);

        // Act
        await _useCase.ExecuteAsync(transaction);

        // Assert
        transaction.IsRejected.Should().BeTrue();
        // Asumiendo que tienes una propiedad RejectionReason o similar
        // transaction.RejectionReason.Should().Be(TransactionRejectionReason.IndividualLimitExceeded); 
        
        // Verificar que el evento de rechazo fue publicado
        _mockEventPublisher.Verify(p => p.PublishAsync(It.Is<IEnumerable<DomainEvent>>(events => 
            events.OfType<TransactionValidationResultEvent>().Any(e => e.Status == TransactionStatus.Rejected))), 
            Times.Once);

        // Verificar que no se intentó guardar nada en el repositorio
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<DailyAccumulatedTransaction>()), Times.Never);
        // Verificar que no se consultó el acumulado, ya que se rechaza antes
        _mockRepository.Verify(r => r.GetForAccountTodayAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldApproveTransaction_WhenValidAndNoExistingAccumulated()
    {
        // Arrange
        var transactionAmount = TransactionAmount.Create(500).Value; // Below individual limit
        var transaction = Transaction.CreateNew("account-new", "account-dest", transactionAmount);

        // Mock repository to return null (no existing accumulated)
        _mockRepository.Setup(r => r.GetForAccountTodayAsync(transaction.SourceAccountId))
                       .ReturnsAsync((DailyAccumulatedTransaction?)null);

        DailyAccumulatedTransaction? savedAccumulated = null;
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<DailyAccumulatedTransaction>()))
                       .Callback<DailyAccumulatedTransaction>(acc => savedAccumulated = acc) // Capture the saved object
                       .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(transaction);

        // Assert
        transaction.IsRejected.Should().BeFalse();

        // Verify repository interactions
        _mockRepository.Verify(r => r.GetForAccountTodayAsync(transaction.SourceAccountId), Times.Once);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<DailyAccumulatedTransaction>()), Times.Once);

        // Verify the saved accumulated transaction is new and has the correct amount
        savedAccumulated.Should().NotBeNull();
        savedAccumulated?.AccountId.Should().Be(transaction.SourceAccountId);
        savedAccumulated?.AccumulatedAmount.Should().Be(transaction.Amount); // Initial amount

        // Verify approval event published
        _mockEventPublisher.Verify(p => p.PublishAsync(It.Is<IEnumerable<DomainEvent>>(events =>
            events.OfType<TransactionValidationResultEvent>().Any(e => e.Status == TransactionStatus.Approved))),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldApproveTransaction_WhenValidAndUpdateExistingAccumulated()
    {
        // Arrange
        var transactionAmount = TransactionAmount.Create(300).Value; // Below individual limit
        var transaction = Transaction.CreateNew("account-existing", "account-dest", transactionAmount);
        var existingAmount = TransactionAmount.Create(500).Value;
        var existingAccumulated = DailyAccumulatedTransaction.CreateNew("account-existing", existingAmount); // Existing accumulated below daily limit

        // Mock repository to return the existing accumulated transaction
        _mockRepository.Setup(r => r.GetForAccountTodayAsync(transaction.SourceAccountId))
                       .ReturnsAsync(existingAccumulated);

        DailyAccumulatedTransaction? savedAccumulated = null;
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<DailyAccumulatedTransaction>()))
                       .Callback<DailyAccumulatedTransaction>(acc => savedAccumulated = acc)
                       .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(transaction);

        // Assert
        transaction.IsRejected.Should().BeFalse();

        // Verify repository interactions
        _mockRepository.Verify(r => r.GetForAccountTodayAsync(transaction.SourceAccountId), Times.Once);
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<DailyAccumulatedTransaction>()), Times.Once);

        // Verify the saved accumulated transaction is the updated one
        savedAccumulated.Should().NotBeNull();
        savedAccumulated?.AccountId.Should().Be(transaction.SourceAccountId);
        // Assuming daily limit > 800 (500 + 300)
        savedAccumulated?.AccumulatedAmount.Should().Be(existingAmount + transaction.Amount); 

        // Verify approval event published
        _mockEventPublisher.Verify(p => p.PublishAsync(It.Is<IEnumerable<DomainEvent>>(events =>
            events.OfType<TransactionValidationResultEvent>().Any(e => e.Status == TransactionStatus.Approved))),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectTransaction_WhenAmountExceedsDailyAccountLimit()
    {
        // Arrange
        var transactionAmount = TransactionAmount.Create(500).Value; // Below individual limit (1000)
        var transaction = Transaction.CreateNew("account-daily-limit", "account-dest", transactionAmount);
        // Existing amount close to daily limit (assuming 2000)
        var existingAmount = TransactionAmount.Create(1600).Value; 
        var existingAccumulated = DailyAccumulatedTransaction.CreateNew("account-daily-limit", existingAmount);

        // Mock repository to return the existing accumulated transaction
        _mockRepository.Setup(r => r.GetForAccountTodayAsync(transaction.SourceAccountId))
                       .ReturnsAsync(existingAccumulated);
        
        DailyAccumulatedTransaction? savedAccumulated = null;
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<DailyAccumulatedTransaction>()))
                       .Callback<DailyAccumulatedTransaction>(acc => savedAccumulated = acc) // Capture the saved object
                       .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(transaction);

        // Assert
        transaction.IsRejected.Should().BeTrue();
        // transaction.RejectionReason.Should().Be(TransactionRejectionReason.DailyAccountLimitExceeded); // Assuming this reason exists

        // Verify repository interactions
        _mockRepository.Verify(r => r.GetForAccountTodayAsync(transaction.SourceAccountId), Times.Once);
        // Save is called even if rejected by daily limit, because accumulation happens before the check in the current code
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<DailyAccumulatedTransaction>()), Times.Once); 

        // Verify the saved accumulated reflects the added amount before rejection
        savedAccumulated.Should().NotBeNull();
        savedAccumulated?.AccumulatedAmount.Should().Be(existingAmount + transaction.Amount); // Amount is added before check

        // Verify rejection event published
         _mockEventPublisher.Verify(p => p.PublishAsync(It.Is<IEnumerable<DomainEvent>>(events => 
            events.OfType<TransactionValidationResultEvent>().Any(e => e.Status == TransactionStatus.Rejected))), 
            Times.Once);
    }

    // TODO: Consider adding a test for unexpected exceptions during repository or publisher operations
} 