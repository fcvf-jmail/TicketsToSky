using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketsToSky.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddedChatId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    MaxPrice = table.Column<int>(type: "integer", nullable: false),
                    DepartureAirport = table.Column<string>(type: "varchar(10)", nullable: false),
                    ArrivalAirport = table.Column<string>(type: "varchar(10)", nullable: false),
                    DepartureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MaxTransfersCount = table.Column<int>(type: "integer", nullable: false),
                    MinBaggageAmount = table.Column<int>(type: "integer", nullable: false),
                    MinHandbagsAmount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastChecked = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscriptions");
        }
    }
}
