using fraud_rule_engine_service.Domain.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fraud_rule_engine_service.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/rules")]
[Produces("application/json")]
public sealed class RulesController(IEnumerable<IFraudRule> rules) : ControllerBase
{
    /// <summary>Lists every fraud rule currently registered in the engine, for transparency/auditing.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RuleDescriptor>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<RuleDescriptor>> GetAll()
    {
        var descriptors = rules
            .Select(r => new RuleDescriptor(r.Code, r.Name))
            .OrderBy(r => r.Code)
            .ToList();

        return Ok(descriptors);
    }
}

public sealed record RuleDescriptor(string Code, string Name);
