using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDay.Api.Data;

#nullable disable

namespace RaceDay.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261112120000_AddSessionCache")]
public partial class AddSessionCache : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SessionCache",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(449)", maxLength: 449, nullable: false),
                Value = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                ExpiresAtTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                SlidingExpirationInSeconds = table.Column<long>(type: "bigint", nullable: true),
                AbsoluteExpiration = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SessionCache", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SessionCache_ExpiresAtTime",
            table: "SessionCache",
            column: "ExpiresAtTime");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SessionCache");
    }
}
