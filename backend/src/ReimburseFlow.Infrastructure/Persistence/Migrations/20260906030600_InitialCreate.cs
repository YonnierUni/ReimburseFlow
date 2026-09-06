using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReimburseFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reimbursements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reimbursements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRequests",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReimbursementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRequests", x => x.Key);
                    table.ForeignKey(
                        name: "FK_IdempotencyRequests_Reimbursements_ReimbursementId",
                        column: x => x.ReimbursementId,
                        principalTable: "Reimbursements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRequests_ReimbursementId",
                table: "IdempotencyRequests",
                column: "ReimbursementId");

            migrationBuilder.CreateIndex(
                name: "UX_Reimbursements_EmployeeId_ReceiptNumber",
                table: "Reimbursements",
                columns: new[] { "EmployeeId", "ReceiptNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyRequests");

            migrationBuilder.DropTable(
                name: "Reimbursements");
        }
    }
}
