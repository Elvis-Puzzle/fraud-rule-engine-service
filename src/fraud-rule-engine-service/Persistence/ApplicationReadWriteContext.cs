using Microsoft.EntityFrameworkCore;

namespace fraud_rule_engine_service.Persistence;

/// <summary>Backed by the write connection string / role. Used for ingesting evaluated transactions and fraud cases.</summary>
public sealed class ApplicationReadWriteContext(DbContextOptions<ApplicationReadWriteContext> options)
    : ApplicationDbContextBase(options);
