using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "AppUsers" (
                    "Id" uuid NOT NULL,
                    "Username" text NOT NULL,
                    "PasswordHash" text NOT NULL,
                    "DisplayName" text NOT NULL,
                    "Role" text NOT NULL,
                    "ResidentId" uuid NULL,
                    CONSTRAINT "PK_AppUsers" PRIMARY KEY ("Id")
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AppUsers_Username" ON "AppUsers" ("Username");
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");
        }
    }
}
