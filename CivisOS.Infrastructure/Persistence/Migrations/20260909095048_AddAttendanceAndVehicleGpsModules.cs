using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivisOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceAndVehicleGpsModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeoFenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckInAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckInLatitude = table.Column<double>(type: "float", nullable: false),
                    CheckInLongitude = table.Column<double>(type: "float", nullable: false),
                    CheckInDistanceMeters = table.Column<double>(type: "float", nullable: false),
                    CheckOutLatitude = table.Column<double>(type: "float", nullable: true),
                    CheckOutLongitude = table.Column<double>(type: "float", nullable: true),
                    CheckOutDistanceMeters = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_GeoFences_GeoFenceId",
                        column: x => x.GeoFenceId,
                        principalTable: "GeoFences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleGeoFenceEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeoFenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleGeoFenceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleGeoFenceEvents_GeoFences_GeoFenceId",
                        column: x => x.GeoFenceId,
                        principalTable: "GeoFences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleGeoFenceEvents_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleLocationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    SpeedKmh = table.Column<double>(type: "float(8)", precision: 8, scale: 2, nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleLocationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleLocationHistories_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    SpeedKmh = table.Column<double>(type: "float(8)", precision: 8, scale: 2, nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleLocations_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_AttendanceDate",
                table: "Attendances",
                column: "AttendanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId",
                table: "Attendances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId_AttendanceDate",
                table: "Attendances",
                columns: new[] { "EmployeeId", "AttendanceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_GeoFenceId",
                table: "Attendances",
                column: "GeoFenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_Status",
                table: "Attendances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleGeoFenceEvents_EventType",
                table: "VehicleGeoFenceEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleGeoFenceEvents_GeoFenceId",
                table: "VehicleGeoFenceEvents",
                column: "GeoFenceId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleGeoFenceEvents_OccurredAtUtc",
                table: "VehicleGeoFenceEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleGeoFenceEvents_VehicleId",
                table: "VehicleGeoFenceEvents",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocationHistories_RecordedAtUtc",
                table: "VehicleLocationHistories",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocationHistories_VehicleId",
                table: "VehicleLocationHistories",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocationHistories_VehicleId_RecordedAtUtc",
                table: "VehicleLocationHistories",
                columns: new[] { "VehicleId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocations_RecordedAtUtc",
                table: "VehicleLocations",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocations_VehicleId",
                table: "VehicleLocations",
                column: "VehicleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "VehicleGeoFenceEvents");

            migrationBuilder.DropTable(
                name: "VehicleLocationHistories");

            migrationBuilder.DropTable(
                name: "VehicleLocations");
        }
    }
}
