using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261007090000_AddBackgroundTaskOutbox")]
public sealed class AddBackgroundTaskOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE culinary."BackgroundTasks" (
            "Id" uuid PRIMARY KEY,
            "Kind" text NOT NULL CHECK ("Kind" IN ('welcome', 'resize')),
            "TargetId" text NOT NULL,
            "CreatedAt" timestamptz NOT NULL DEFAULT now(),
            "DispatchedAt" timestamptz NULL,
            "CompletedAt" timestamptz NULL,
            UNIQUE ("Kind", "TargetId")
        );
        CREATE INDEX "IX_BackgroundTasks_Pending" ON culinary."BackgroundTasks" ("CreatedAt")
            WHERE "DispatchedAt" IS NULL;
        """);
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""DROP TABLE culinary."BackgroundTasks";""");
}
