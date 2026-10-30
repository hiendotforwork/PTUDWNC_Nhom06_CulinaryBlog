using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261027090000_AddPasswordResetEmailTask")]
public sealed class AddPasswordResetEmailTask : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE culinary."BackgroundTasks" DROP CONSTRAINT IF EXISTS "BackgroundTasks_Kind_check";
        ALTER TABLE culinary."BackgroundTasks" ADD CONSTRAINT "BackgroundTasks_Kind_check"
            CHECK ("Kind" IN ('welcome', 'resize', 'email-verification', 'password-reset-email'));
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DELETE FROM culinary."BackgroundTasks" WHERE "Kind" = 'password-reset-email';
        ALTER TABLE culinary."BackgroundTasks" DROP CONSTRAINT IF EXISTS "BackgroundTasks_Kind_check";
        ALTER TABLE culinary."BackgroundTasks" ADD CONSTRAINT "BackgroundTasks_Kind_check"
            CHECK ("Kind" IN ('welcome', 'resize', 'email-verification'));
        """);
}
