using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotteryDetection.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketAnalyses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTicketAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    ImageBinaryObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Province = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DrawDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TicketNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DrawType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    IsWinner = table.Column<bool>(type: "bit", nullable: true),
                    MatchedPrize = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PrizeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    RawModelResponse = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTicketAnalyses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTicketAnalyses_TenantId_CreatorUserId_CreationTime",
                table: "AppTicketAnalyses",
                columns: new[] { "TenantId", "CreatorUserId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppTicketAnalyses_TenantId_Status",
                table: "AppTicketAnalyses",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTicketAnalyses");
        }
    }
}
