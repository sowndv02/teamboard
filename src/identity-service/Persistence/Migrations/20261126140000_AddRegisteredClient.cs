using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegisteredClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    client_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    secret_hash = table.Column<string>(type: "text", nullable: true),
                    secret_salt = table.Column<string>(type: "text", nullable: true),
                    secret_iterations = table.Column<int>(type: "integer", nullable: true),
                    redirect_uris = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    allowed_scopes = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client", x => x.client_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client");
        }
    }
}