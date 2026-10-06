using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261005120000_RemoveFuelCardNumberWhitespace")]
public sealed class RemoveFuelCardNumberWhitespace : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Match the Unicode whitespace removed by FuelCardRules.RemoveCardNumberWhitespace.
        int[] whitespaceCodePoints =
        [
            9, 10, 11, 12, 13, 32, 133, 160, 5760,
            8192, 8193, 8194, 8195, 8196, 8197, 8198, 8199, 8200, 8201, 8202,
            8232, 8233, 8239, 8287, 12288
        ];
        var cleanedNumber = whitespaceCodePoints.Aggregate("card.[CardNumber]",
            (value, codePoint) => $"REPLACE({value}, NCHAR({codePoint}), N'')");

        migrationBuilder.Sql($"""
            IF EXISTS (
                SELECT 1
                FROM [app].[FuelCards] AS card
                CROSS APPLY (SELECT {cleanedNumber} AS [Number]) AS cleaned
                WHERE DATALENGTH(cleaned.[Number]) = 0
            )
                THROW 51000, 'A fuel card number contains only whitespace; correct it before applying this migration.', 1;

            UPDATE card
            SET [CardNumber] = cleaned.[Number]
            FROM [app].[FuelCards] AS card
            CROSS APPLY (SELECT {cleanedNumber} AS [Number]) AS cleaned
            WHERE DATALENGTH(card.[CardNumber]) <> DATALENGTH(cleaned.[Number]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Removed whitespace cannot be reconstructed.
    }
}
