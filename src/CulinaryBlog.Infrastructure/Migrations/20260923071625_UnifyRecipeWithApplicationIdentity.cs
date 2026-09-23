using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Migrations;

public partial class UnifyRecipeWithApplicationIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO public."AspNetUsers"
                ("Id", "DisplayName", "AvatarUrl", "Bio", "IsActive", "CreatedAt",
                 "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                 "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
                 "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd",
                 "LockoutEnabled", "AccessFailedCount")
            SELECT
                "Id", "DisplayName", "AvatarUrl", "Bio", "IsActive", "CreatedAt",
                "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
                "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd",
                "LockoutEnabled", "AccessFailedCount"
            FROM culinary."AspNetUsers"
            ON CONFLICT ("Id") DO NOTHING;
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_Recipes_AspNetUsers_AuthorId",
            schema: "culinary",
            table: "Recipes");

        migrationBuilder.AddForeignKey(
            name: "FK_Recipes_AspNetUsers_AuthorId",
            schema: "culinary",
            table: "Recipes",
            column: "AuthorId",
            principalSchema: "public",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Recipes_AspNetUsers_AuthorId",
            schema: "culinary",
            table: "Recipes");

        migrationBuilder.AddForeignKey(
            name: "FK_Recipes_AspNetUsers_AuthorId",
            schema: "culinary",
            table: "Recipes",
            column: "AuthorId",
            principalSchema: "culinary",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }
}