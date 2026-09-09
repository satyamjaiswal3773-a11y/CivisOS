using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivisOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeoFenceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeoFences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CenterLatitude = table.Column<double>(type: "float", nullable: false),
                    CenterLongitude = table.Column<double>(type: "float", nullable: false),
                    RadiusMeters = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoFences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeoFences_Name",
                table: "GeoFences",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeoFences_Status",
                table: "GeoFences",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeoFences");
        }
    }
}
