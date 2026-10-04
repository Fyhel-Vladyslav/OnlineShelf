-- Тестовий гардероб для перевірки підбору образів (OutfitOfferService).
-- Речі без фото: їх скоринг іде за атрибутами, без візуального ембединга.
-- Ідемпотентний: Id детермінований (md5 від назви), повторний запуск оновлює ті самі записи.
--
-- Запуск:
--   docker exec -i online-shelf-db psql -U admin -d onlineShelf_db < backend/Microservices/scripts/seed_test_wardrobe.sql
--
-- Ключі довідника (shelf_service."AttributesValues"):
--   Type:     1 shirt, 2 top/t-shirt/sweatshirt, 3 sweater, 4 cardigan, 5 jacket, 6 vest, 7 pants, 8 shorts, 9 skirt,
--             10 coat, 11 dress, 12 jumpsuit, 15 hat, 19 watch, 20 belt, 23 shoe, 25 scarf
--   Season:   1-3 зима, 4-6 весна, 7-9 літо, 10-12 осінь (рання/середня/пізня); 0 — всесезонна
--   Pattern:  1 Solid, 2 Stripes, 3 Floral, 4 Camouflage, 5 Dots, 6 Logo Print, 7 Glitter, 8 Distressed
--   Material: 1 Cotton, 2 Polyester, 3 Wool, 4 Silk, 5 Linen, 6 Denim, 7 Leather, 8 Nylon

\set user_id   '''d1437f42-8b54-4a02-b0a5-800426cd7580'''
\set wardrobe  '''22222222-2222-2222-2222-222222222222'''
\set hallway   '''11111111-1111-1111-1111-111111111111'''

INSERT INTO shelf_service."Items"
    ("Id", "Name", "UserId", "ShelfId", "DateCreated", "UpdatedAt",
     "AttributeType", "AttributeSeason", "AttributePattern", "AttributeMatterial",
     "AttributeColorMain", "AttributeColorSecond", "isFavorite")
SELECT md5('seed-wardrobe:' || v.name)::uuid, v.name, :user_id::uuid,
       CASE v.shelf WHEN 'hallway' THEN :hallway::uuid ELSE :wardrobe::uuid END,
       now(), now(),
       v.type, v.season, v.pattern, v.material, v.color_main, v.color_second, v.favorite
FROM (VALUES
    -- name                          shelf       type season pattern material color_main color_second favorite
    -- Верх
    ('Біла футболка',               'wardrobe',  2,   8,     1,      1,       '#FFFFFF', '#FFFFFF', false),  -- літня: восени відсіюється
    ('Чорний світшот',              'wardrobe',  2,   0,     6,      1,       '#000000', '#FFFFFF', true),
    ('Блакитна сорочка',            'wardrobe',  1,   11,    1,      1,       '#87CEEB', '#87CEEB', false),
    ('Сорочка в смужку',            'wardrobe',  1,   10,    2,      5,       '#FFFFFF', '#000080', false),
    ('Сірий светр',                 'wardrobe',  3,   12,    1,      3,       '#808080', '#808080', true),
    ('Бежевий кардиган',            'wardrobe',  4,   11,    1,      3,       '#F5F5DC', '#F5F5DC', false),
    ('Оливковий жилет',             'wardrobe',  6,   12,    1,      3,       '#556B2F', '#556B2F', false),
    -- Низ
    ('Сині джинси',                 'wardrobe',  7,   0,     1,      6,       '#1E3A8A', '#1E3A8A', true),
    ('Чорні штани',                 'wardrobe',  7,   11,    1,      2,       '#000000', '#000000', false),
    ('Бежеві шорти',                'wardrobe',  8,   8,     1,      5,       '#F5DEB3', '#F5DEB3', false),  -- літні
    ('Бордова спідниця',            'wardrobe',  9,   10,    1,      2,       '#8B0000', '#8B0000', false),
    -- Сукні / комбінезони
    ('Трикотажна сукня',            'wardrobe',  11,  11,    1,      3,       '#800020', '#800020', false),
    ('Літня сукня в квітку',        'wardrobe',  11,  7,     3,      5,       '#FFC0CB', '#FFFFFF', false),  -- літня
    ('Джинсовий комбінезон',        'wardrobe',  12,  10,    1,      6,       '#4682B4', '#4682B4', false),
    -- Верхній одяг
    ('Джинсова куртка',             'hallway',   5,   10,    1,      6,       '#4169E1', '#4169E1', false),
    ('Шкіряна куртка',              'hallway',   5,   11,    1,      7,       '#000000', '#000000', true),
    ('Вовняне пальто',              'hallway',   10,  1,     1,      3,       '#C19A6B', '#C19A6B', false),
    ('Пуховик',                     'hallway',   10,  2,     1,      8,       '#2F4F4F', '#2F4F4F', false),  -- середина зими: зараз відсіюється
    -- Взуття
    ('Білі кросівки',               'hallway',   23,  0,     6,      7,       '#FFFFFF', '#FF0000', true),
    ('Коричневі черевики',          'hallway',   23,  12,    1,      7,       '#3E2723', '#3E2723', false),
    ('Сандалі',                     'hallway',   23,  8,     1,      7,       '#D2B48C', '#D2B48C', false),  -- літні
    -- Аксесуари (у образ потрапляють лише через Include)
    ('Картатий шарф',               'hallway',   25,  12,    2,      3,       '#B22222', '#000000', false),
    ('Чорна шапка',                 'hallway',   15,  1,     1,      3,       '#000000', '#000000', false),
    ('Шкіряний ремінь',             'wardrobe',  20,  0,     1,      7,       '#5D4037', '#5D4037', false),
    ('Срібний годинник',            'wardrobe',  19,  0,     1,      0,       '#C0C0C0', '#C0C0C0', false)
) AS v(name, shelf, type, season, pattern, material, color_main, color_second, favorite)
ON CONFLICT ("Id") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "ShelfId" = EXCLUDED."ShelfId",
    "UpdatedAt" = now(),
    "AttributeType" = EXCLUDED."AttributeType",
    "AttributeSeason" = EXCLUDED."AttributeSeason",
    "AttributePattern" = EXCLUDED."AttributePattern",
    "AttributeMatterial" = EXCLUDED."AttributeMatterial",
    "AttributeColorMain" = EXCLUDED."AttributeColorMain",
    "AttributeColorSecond" = EXCLUDED."AttributeColorSecond",
    "isFavorite" = EXCLUDED."isFavorite";

SELECT count(*) AS seeded_items
FROM shelf_service."Items"
WHERE "Id" IN (SELECT md5('seed-wardrobe:' || "Name")::uuid FROM shelf_service."Items");
