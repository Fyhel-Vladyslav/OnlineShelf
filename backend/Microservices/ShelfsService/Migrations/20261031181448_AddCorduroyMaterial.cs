using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelfsService.Migrations
{
    /// <inheritdoc />
    public partial class AddCorduroyMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MaterialDetector у ParseImageService розпізнає вельвет, але значення не було в довіднику, і атрибут лишався 0.
            // На новій БД довідник ще порожній: SELECT не дає рядків, значення додасть DatabaseSeeder.
            // Лічильник Id спершу підтягуємо до наявних рядків: у вже заповненій БД він відставав, і вставка падала на PK.
            migrationBuilder.Sql("""
                SELECT setval(pg_get_serial_sequence('shelf_service."AttributesValues"', 'Id'),
                              COALESCE(MAX("Id"), 1), MAX("Id") IS NOT NULL)
                FROM shelf_service."AttributesValues";

                INSERT INTO shelf_service."AttributesValues" ("AttributeId", "AttributeKey", "AttributeValue")
                SELECT a."Id", 9, 'Corduroy'
                FROM shelf_service."Attributes" a
                WHERE a."AttributeKey" = 6
                  AND NOT EXISTS (
                      SELECT 1 FROM shelf_service."AttributesValues" v
                      WHERE v."AttributeId" = a."Id" AND lower(v."AttributeValue") = 'corduroy');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM shelf_service."AttributesValues" v
                USING shelf_service."Attributes" a
                WHERE v."AttributeId" = a."Id" AND a."AttributeKey" = 6 AND v."AttributeValue" = 'Corduroy';
                """);
        }
    }
}
