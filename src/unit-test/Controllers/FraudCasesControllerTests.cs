using fraud_rule_engine_service.Common.Exceptions;
using fraud_rule_engine_service.Controllers;
using fraud_rule_engine_service.Domain.Models;
using fraud_rule_engine_service.Service;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace fraud_rule_engine_service.UnitTests.Controllers;

public class FraudCasesControllerTests
{
    private readonly Mock<IFraudCaseQueryService> _queryService = new();
    private readonly FraudCasesController _controller;

    public FraudCasesControllerTests()
    {
        _controller = new FraudCasesController(_queryService.Object);
    }

    [Fact]
    public async Task GetById_ExistingCase_ReturnsOkWithCase()
    {
        var fraudCase = BuildFraudCase();
        _queryService.Setup(s => s.GetByIdAsync(fraudCase.Id, It.IsAny<CancellationToken>())).ReturnsAsync(fraudCase);

        var result = await _controller.GetById(fraudCase.Id, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(fraudCase);
    }

    [Fact]
    public async Task GetById_MissingCase_ThrowsNotFoundException()
    {
        _queryService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((FraudCaseResult?)null);

        await Should.ThrowAsync<NotFoundException>(() => _controller.GetById(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Search_FromLaterThanTo_ReturnsBadRequest()
    {
        var result = await _controller.Search(
            accountId: null, customerId: null, minSeverity: null, status: null, ruleCode: null,
            from: DateTimeOffset.UtcNow, to: DateTimeOffset.UtcNow.AddDays(-1), page: 1, pageSize: 20, CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_ValidFilters_ReturnsPagedResultFromService()
    {
        var paged = new PagedResult<FraudCaseResult> { Items = [BuildFraudCase()], Page = 1, PageSize = 20, TotalCount = 1 };
        _queryService
            .Setup(s => s.QueryAsync(It.IsAny<FraudCaseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _controller.Search(
            accountId: "acc-1", customerId: null, minSeverity: null, status: null, ruleCode: null,
            from: null, to: null, page: 1, pageSize: 20, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(paged);
    }

    [Fact]
    public async Task UpdateStatus_ExistingCase_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _queryService.Setup(s => s.UpdateStatusAsync(id, FraudCaseStatus.Confirmed, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.UpdateStatus(id, new UpdateFraudCaseStatusRequest { Status = FraudCaseStatus.Confirmed }, CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateStatus_MissingCase_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _queryService.Setup(s => s.UpdateStatusAsync(id, It.IsAny<FraudCaseStatus>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.UpdateStatus(id, new UpdateFraudCaseStatusRequest { Status = FraudCaseStatus.FalsePositive }, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    private static FraudCaseResult BuildFraudCase() => new()
    {
        Id = Guid.NewGuid(),
        TransactionId = "txn-1",
        AccountId = "acc-1",
        CustomerId = "cust-1",
        Amount = 1000m,
        Currency = "ZAR",
        Category = "groceries",
        Severity = FraudSeverity.Medium,
        OverallScore = 50,
        Status = FraudCaseStatus.Open,
        OccurredAt = DateTimeOffset.UtcNow,
        EvaluatedAt = DateTimeOffset.UtcNow,
        TriggeredRules = []
    };
}
