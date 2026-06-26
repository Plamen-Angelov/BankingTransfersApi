using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingTransfers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReasonToTransferRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "TransferRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "TransferRequests");
        }
    }
}
