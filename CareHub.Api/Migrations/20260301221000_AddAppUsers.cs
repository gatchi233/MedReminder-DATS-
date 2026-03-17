using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace CareHub.Api.Migrations
{
    [DbContext(typeof(Data.CareHubDbContext))]
    [Migration("20260301221000_AddAppUsers")]
    public partial class AddAppUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: AppUsers table is created by the later AddAppUser migration.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
