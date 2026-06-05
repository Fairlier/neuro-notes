using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroNotes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAIOperationIsAutomaticAndClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Classification_CustomPrompt",
                table: "UserAIProfiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Classification_IsAutomatic",
                table: "UserAIProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Classification_TargetLanguage",
                table: "UserAIProfiles",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Classification_UseCustomPrompt",
                table: "UserAIProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GlobalChat_IsAutomatic",
                table: "UserAIProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NoteChat_IsAutomatic",
                table: "UserAIProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Structuring_IsAutomatic",
                table: "UserAIProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Summarization_IsAutomatic",
                table: "UserAIProfiles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Transcription_IsAutomatic",
                table: "UserAIProfiles",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Classification_CustomPrompt",
                table: "UserAIProfiles");

            migrationBuilder.DropColumn(
                name: "Classification_IsAutomatic",
                table: "UserAIProfiles");

            migrationBuilder.DropColumn(
                name: "Classification_TargetLanguage",
                table: "UserAIProfiles");

            migrationBuilder.DropColumn(
                name: "Classification_UseCustomPrompt",
                table: "UserAIProfiles");

            migrationBuilder.DropColumn(
                name: "GlobalChat_IsAutomatic",
                table: "UserAIProfiles");

            migrationBuilder.DropColumn(
                name: "NoteChat_IsAutomatic",
                table: "UserAIProfiles");

            migrationBuilder.DropColumn(
                name: "Structuring_IsAutomatic",
                table: "UserAIProfiles");

            migrationBuilder.DropColumn(
                name: "Summarization_IsAutomatic",
                table: "UserAIProfiles");

            migrationBuilder.DropColumn(
                name: "Transcription_IsAutomatic",
                table: "UserAIProfiles");
        }
    }
}
