using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlowPro.Migrations
{
    /// <inheritdoc />
    public partial class MakeEmployeeUserIdNullableAndPartialUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Employees",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.Sql(@"UPDATE Employees SET UserId = NULL WHERE UserId = '';");
            migrationBuilder.Sql(@"CREATE UNIQUE INDEX IF NOT EXISTS IX_Employees_UserId_NotNull ON Employees(UserId) WHERE UserId IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Employees_UserId_NotNull;");
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Employees",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
