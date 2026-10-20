using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261020080000_AllowRepeatedEmailVerificationTasks")]
public sealed class AllowRepeatedEmailVerificationTasks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE culinary."BackgroundTasks" DROP CONSTRAINT IF EXISTS "BackgroundTasks_Kind_check";
        ALTER TABLE culinary."BackgroundTasks" ADD CONSTRAINT "BackgroundTasks_Kind_check"
            CHECK ("Kind" IN ('welcome', 'resize', 'email-verification'));

        ALTER TABLE culinary."BackgroundTasks" DROP CONSTRAINT IF EXISTS "BackgroundTasks_Kind_TargetId_key";
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_BackgroundTasks_DeduplicatedKinds"
            ON culinary."BackgroundTasks" ("Kind", "TargetId")
            WHERE "Kind" IN ('welcome', 'resize');
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP INDEX IF EXISTS culinary."IX_BackgroundTasks_DeduplicatedKinds";
        DELETE FROM culinary."BackgroundTasks" WHERE "Kind" = 'email-verification';
        ALTER TABLE culinary."BackgroundTasks" ADD CONSTRAINT "BackgroundTasks_Kind_TargetId_key"
            UNIQUE ("Kind", "TargetId");

        ALTER TABLE culinary."BackgroundTasks" DROP CONSTRAINT IF EXISTS "BackgroundTasks_Kind_check";
        ALTER TABLE culinary."BackgroundTasks" ADD CONSTRAINT "BackgroundTasks_Kind_check"
            CHECK ("Kind" IN ('welcome', 'resize'));
        """);
}
