using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fraud_rule_engine_service.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fraud_rule_engine");

            migrationBuilder.CreateTable(
                name: "fraud_cases",
                schema: "fraud_rule_engine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<string>(type: "text", nullable: false),
                    account_id = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    overall_score = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    correlation_id = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evaluated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fraud_cases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transaction_snapshots",
                schema: "fraud_rule_engine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<string>(type: "text", nullable: false),
                    account_id = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    merchant_name = table.Column<string>(type: "text", nullable: true),
                    merchant_category_code = table.Column<string>(type: "text", nullable: true),
                    counterparty_account_id = table.Column<string>(type: "text", nullable: true),
                    is_new_payee = table.Column<bool>(type: "boolean", nullable: false),
                    country_code = table.Column<string>(type: "text", nullable: false),
                    device_id = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transaction_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fraud_case_triggered_rules",
                schema: "fraud_rule_engine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fraud_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_code = table.Column<string>(type: "text", nullable: false),
                    rule_name = table.Column<string>(type: "text", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fraud_case_triggered_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_fraud_case_triggered_rules_fraud_cases_fraud_case_id",
                        column: x => x.fraud_case_id,
                        principalSchema: "fraud_rule_engine",
                        principalTable: "fraud_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fraud_case_triggered_rules_fraud_case_id",
                schema: "fraud_rule_engine",
                table: "fraud_case_triggered_rules",
                column: "fraud_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_fraud_case_triggered_rules_rule_code",
                schema: "fraud_rule_engine",
                table: "fraud_case_triggered_rules",
                column: "rule_code");

            migrationBuilder.CreateIndex(
                name: "ix_fraud_cases_account_id_evaluated_at",
                schema: "fraud_rule_engine",
                table: "fraud_cases",
                columns: new[] { "account_id", "evaluated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fraud_cases_customer_id",
                schema: "fraud_rule_engine",
                table: "fraud_cases",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_fraud_cases_severity",
                schema: "fraud_rule_engine",
                table: "fraud_cases",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "ix_fraud_cases_transaction_id",
                schema: "fraud_rule_engine",
                table: "fraud_cases",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_snapshots_account_id_occurred_at",
                schema: "fraud_rule_engine",
                table: "transaction_snapshots",
                columns: new[] { "account_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_transaction_snapshots_transaction_id",
                schema: "fraud_rule_engine",
                table: "transaction_snapshots",
                column: "transaction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fraud_case_triggered_rules",
                schema: "fraud_rule_engine");

            migrationBuilder.DropTable(
                name: "transaction_snapshots",
                schema: "fraud_rule_engine");

            migrationBuilder.DropTable(
                name: "fraud_cases",
                schema: "fraud_rule_engine");
        }
    }
}
