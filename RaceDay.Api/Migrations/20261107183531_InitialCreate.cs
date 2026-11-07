using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Club",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    registration_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    contact_email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    contact_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Club", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Discipline",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discipline", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    id_number_or_passport = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    asa_permanent_license_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Organiser",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    provisioned_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    provisioned_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organiser", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_Organiser_User_user_id",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Participant",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participant", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_Participant_User_user_id",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organiser_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    distance_km = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    event_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    venue_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.id);
                    table.ForeignKey(
                        name: "FK_Event_Organiser_organiser_id",
                        column: x => x.organiser_id,
                        principalTable: "Organiser",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClubMembership",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    participant_id = table.Column<int>(type: "int", nullable: false),
                    club_id = table.Column<int>(type: "int", nullable: false),
                    discipline_id = table.Column<int>(type: "int", nullable: false),
                    membership_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    joined_at = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubMembership", x => x.id);
                    table.ForeignKey(
                        name: "FK_ClubMembership_Club_club_id",
                        column: x => x.club_id,
                        principalTable: "Club",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubMembership_Discipline_discipline_id",
                        column: x => x.discipline_id,
                        principalTable: "Discipline",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubMembership_Participant_participant_id",
                        column: x => x.participant_id,
                        principalTable: "Participant",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    event_id = table.Column<int>(type: "int", nullable: false),
                    discipline_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    category_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    distance_km = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    min_age = table.Column<int>(type: "int", nullable: true),
                    max_age = table.Column<int>(type: "int", nullable: true),
                    entry_fee_zar = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    max_slots = table.Column<int>(type: "int", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.id);
                    table.ForeignKey(
                        name: "FK_Category_Discipline_discipline_id",
                        column: x => x.discipline_id,
                        principalTable: "Discipline",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Category_Event_event_id",
                        column: x => x.event_id,
                        principalTable: "Event",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Registration",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    participant_id = table.Column<int>(type: "int", nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    registration_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    payment_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    amount_paid = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    assigned_bib_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    requires_temp_asa_license = table.Column<bool>(type: "bit", nullable: false),
                    medical_aid_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    medical_aid_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registration", x => x.id);
                    table.ForeignKey(
                        name: "FK_Registration_Category_category_id",
                        column: x => x.category_id,
                        principalTable: "Category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Registration_Participant_participant_id",
                        column: x => x.participant_id,
                        principalTable: "Participant",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Result",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    registration_id = table.Column<int>(type: "int", nullable: false),
                    gun_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    net_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    overall_position = table.Column<int>(type: "int", nullable: true),
                    category_position = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Result", x => x.id);
                    table.ForeignKey(
                        name: "FK_Result_Registration_registration_id",
                        column: x => x.registration_id,
                        principalTable: "Registration",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Category_discipline_id",
                table: "Category",
                column: "discipline_id");

            migrationBuilder.CreateIndex(
                name: "IX_Category_event_id",
                table: "Category",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_Club_name",
                table: "Club",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Club_registration_number",
                table: "Club",
                column: "registration_number",
                unique: true,
                filter: "[registration_number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembership_club_id",
                table: "ClubMembership",
                column: "club_id");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembership_discipline_id",
                table: "ClubMembership",
                column: "discipline_id");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembership_participant_id_discipline_id",
                table: "ClubMembership",
                columns: new[] { "participant_id", "discipline_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Discipline_name",
                table: "Discipline",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Event_organiser_id",
                table: "Event",
                column: "organiser_id");

            migrationBuilder.CreateIndex(
                name: "IX_Registration_category_id",
                table: "Registration",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Registration_participant_id",
                table: "Registration",
                column: "participant_id");

            migrationBuilder.CreateIndex(
                name: "IX_Registration_participant_id_category_id",
                table: "Registration",
                columns: new[] { "participant_id", "category_id" },
                unique: true,
                filter: "[status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_Result_registration_id",
                table: "Result",
                column: "registration_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_email",
                table: "User",
                column: "email",
                unique: true);

            // Subtype role triggers from docs/RaceDay.sql (EF does not model these).
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_Participant_EnforceRole ON Participant
                AFTER INSERT AS
                BEGIN
                  IF EXISTS (SELECT 1 FROM inserted i JOIN [User] u ON u.id=i.user_id WHERE u.role <> 'Participant')
                    THROW 51001, 'Participant subtype requires Participant role.', 1;
                  IF EXISTS (SELECT 1 FROM inserted i JOIN Organiser o ON o.user_id=i.user_id)
                    THROW 51002, 'A user cannot be both Participant and Organiser.', 1;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER TR_Organiser_EnforceRole ON Organiser
                AFTER INSERT AS
                BEGIN
                  IF EXISTS (SELECT 1 FROM inserted i JOIN [User] u ON u.id=i.user_id WHERE u.role <> 'Organiser')
                    THROW 51003, 'Organiser subtype requires Organiser role.', 1;
                  IF EXISTS (SELECT 1 FROM inserted i JOIN Participant p ON p.user_id=i.user_id)
                    THROW 51004, 'A user cannot be both Participant and Organiser.', 1;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER TR_User_RoleMatchesSubtype ON [User]
                AFTER UPDATE AS
                BEGIN
                  IF EXISTS (
                    SELECT 1 FROM inserted i
                    WHERE (EXISTS (SELECT 1 FROM Participant p WHERE p.user_id = i.id) AND i.role <> 'Participant')
                       OR (EXISTS (SELECT 1 FROM Organiser o WHERE o.user_id = i.id) AND i.role <> 'Organiser')
                  )
                    THROW 51005, 'User role must match its account subtype.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Participant_EnforceRole;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Organiser_EnforceRole;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_User_RoleMatchesSubtype;");

            migrationBuilder.DropTable(
                name: "ClubMembership");

            migrationBuilder.DropTable(
                name: "Result");

            migrationBuilder.DropTable(
                name: "Club");

            migrationBuilder.DropTable(
                name: "Registration");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Participant");

            migrationBuilder.DropTable(
                name: "Discipline");

            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "Organiser");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
