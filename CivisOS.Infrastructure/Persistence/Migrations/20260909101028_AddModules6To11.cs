using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivisOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModules6To11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CleaningAreas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GeoFenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    AssignedEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastCleanedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextCleanDueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CleaningAreas_Employees_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CleaningAreas_GeoFences_GeoFenceId",
                        column: x => x.GeoFenceId,
                        principalTable: "GeoFences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DeviceToken = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CleaningLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CleaningAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    InspectionScore = table.Column<int>(type: "int", nullable: true),
                    InspectionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CleaningLogs_CleaningAreas_CleaningAreaId",
                        column: x => x.CleaningAreaId,
                        principalTable: "CleaningAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CleaningLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CleaningSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CleaningAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    PreferredTimeLocal = table.Column<TimeOnly>(type: "time", nullable: false),
                    AssignedEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CleaningSchedules_CleaningAreas_CleaningAreaId",
                        column: x => x.CleaningAreaId,
                        principalTable: "CleaningAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CleaningSchedules_Employees_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DeadlineUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssigneeEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GeoFenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CleaningAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkTasks_CleaningAreas_CleaningAreaId",
                        column: x => x.CleaningAreaId,
                        principalTable: "CleaningAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTasks_Employees_AssignedByEmployeeId",
                        column: x => x.AssignedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTasks_Employees_AssigneeEmployeeId",
                        column: x => x.AssigneeEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTasks_GeoFences_GeoFenceId",
                        column: x => x.GeoFenceId,
                        principalTable: "GeoFences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversationMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationMembers_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CleaningPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CleaningLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhotoType = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AiConfidenceScore = table.Column<double>(type: "float(5)", precision: 5, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CleaningPhotos_CleaningLogs_CleaningLogId",
                        column: x => x.CleaningLogId,
                        principalTable: "CleaningLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAlerts_AlertType",
                table: "AiAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_AiAlerts_DetectedAtUtc",
                table: "AiAlerts",
                column: "DetectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AiAlerts_Severity",
                table: "AiAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AiAlerts_Status",
                table: "AiAlerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_CreatedAtUtc",
                table: "AppNotifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_IsRead",
                table: "AppNotifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_Type",
                table: "AppNotifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_UserId",
                table: "AppNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningAreas_AssignedEmployeeId",
                table: "CleaningAreas",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningAreas_GeoFenceId",
                table: "CleaningAreas",
                column: "GeoFenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningAreas_Name",
                table: "CleaningAreas",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CleaningAreas_Status",
                table: "CleaningAreas",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningLogs_CleaningAreaId",
                table: "CleaningLogs",
                column: "CleaningAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningLogs_EmployeeId",
                table: "CleaningLogs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningLogs_StartedAtUtc",
                table: "CleaningLogs",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningLogs_Status",
                table: "CleaningLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningPhotos_CleaningLogId",
                table: "CleaningPhotos",
                column: "CleaningLogId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningPhotos_PhotoType",
                table: "CleaningPhotos",
                column: "PhotoType");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningSchedules_AssignedEmployeeId",
                table: "CleaningSchedules",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningSchedules_CleaningAreaId",
                table: "CleaningSchedules",
                column: "CleaningAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningSchedules_IsActive",
                table: "CleaningSchedules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_ConversationId",
                table: "ConversationMembers",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_ConversationId_UserId",
                table: "ConversationMembers",
                columns: new[] { "ConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_UserId",
                table: "ConversationMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Type",
                table: "Conversations",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_SentAtUtc",
                table: "Messages",
                columns: new[] { "ConversationId", "SentAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SentAtUtc",
                table: "Messages",
                column: "SentAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_DeviceToken",
                table: "UserDeviceTokens",
                column: "DeviceToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_IsActive",
                table: "UserDeviceTokens",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_UserId",
                table: "UserDeviceTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_AssignedByEmployeeId",
                table: "WorkTasks",
                column: "AssignedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_AssigneeEmployeeId",
                table: "WorkTasks",
                column: "AssigneeEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_CleaningAreaId",
                table: "WorkTasks",
                column: "CleaningAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_DeadlineUtc",
                table: "WorkTasks",
                column: "DeadlineUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_GeoFenceId",
                table: "WorkTasks",
                column: "GeoFenceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_Priority",
                table: "WorkTasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_Status",
                table: "WorkTasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAlerts");

            migrationBuilder.DropTable(
                name: "AppNotifications");

            migrationBuilder.DropTable(
                name: "CleaningPhotos");

            migrationBuilder.DropTable(
                name: "CleaningSchedules");

            migrationBuilder.DropTable(
                name: "ConversationMembers");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "UserDeviceTokens");

            migrationBuilder.DropTable(
                name: "WorkTasks");

            migrationBuilder.DropTable(
                name: "CleaningLogs");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "CleaningAreas");
        }
    }
}
