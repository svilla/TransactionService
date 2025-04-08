using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntiFraudService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateAccumulatedTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyAccumulatedTransactions",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    AccumulatedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyAccumulatedTransactions", x => new { x.AccountId, x.Date });
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyAccumulatedTransactions_AccountId",
                table: "DailyAccumulatedTransactions",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyAccumulatedTransactions");
        }
    }
}
