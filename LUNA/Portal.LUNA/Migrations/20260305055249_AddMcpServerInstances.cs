using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portal.LUNA.Migrations
{
    /// <inheritdoc />
    public partial class AddMcpServerInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "McpServerInstances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AvailableMcpServerId = table.Column<string>(type: "TEXT", nullable: false),
                    ContainerId = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StoppedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpServerInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McpServerInstances_AvailableMcpServers_AvailableMcpServerId",
                        column: x => x.AvailableMcpServerId,
                        principalTable: "AvailableMcpServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_McpServerInstances_AvailableMcpServerId",
                table: "McpServerInstances",
                column: "AvailableMcpServerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "McpServerInstances");
        }
    }
}
