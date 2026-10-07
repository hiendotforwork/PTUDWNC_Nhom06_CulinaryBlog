-- Local Docker database only. Run after Docker migrations and Lab2Seeder.
-- Repeatable: stable IDs and ON CONFLICT preserve existing records.
BEGIN;
DO $$
BEGIN
    IF current_database() <> 'culinary_blog' THEN
        RAISE EXCEPTION 'This seed requires the local culinary_blog database';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM culinary."AspNetUsers" WHERE "Id" = 'lab2-chuong-demo-author') THEN
        RAISE EXCEPTION 'Run the existing Lab 2 seed first';
    END IF;
END $$;
WITH samples AS (
    SELECT i,
        CASE WHEN i % 2 = 0 THEN 'Phở bò'
             ELSE (ARRAY['Bún bò Huế', 'Cơm gà Hội An', 'Bánh mì thịt', 'Gỏi cuốn tôm', 'Cá kho tộ'])[1 + ((i / 2) % 5)] END AS dish,
        CASE WHEN i < 48 THEN 1 WHEN i < 54 THEN 0 ELSE 2 END AS status
    FROM generate_series(0, 59) AS i
)
INSERT INTO culinary."Recipes" (
    "Id", "Title", "Slug", "Description", "Instructions", "PrepTime", "CookTime", "Servings",
    "Difficulty", "Status", "PublishedAt", "CategoryId", "AuthorId", "CreatedAt", "IsDeleted", "RowVersion"
)
SELECT md5('fr-srch-demo-' || i)::uuid,
    dish || ' - mẫu kiểm thử ' || lpad((i + 1)::text, 2, '0'),
    'fr-srch-demo-' || lpad((i + 1)::text, 2, '0'),
    'Dữ liệu mẫu để kiểm thử tìm kiếm tiếng Việt không dấu, lọc và phân trang. Món ' || dish ||
        CASE WHEN i % 4 = 0 THEN ' có nước dùng thơm, thịt bò và hành lá.' ELSE ' dùng nguyên liệu tươi và rau thơm.' END,
    'Đây là dữ liệu kiểm thử phần mềm.',
    5 + (i % 6) * 5, 10 + (i % 8) * 10, 1 + (i % 6),
    1 + (i % 3), status,
    CASE WHEN status = 1 THEN timestamptz '2026-10-01 08:00:00+07' - i * interval '1 day' ELSE NULL END,
    (SELECT "Id" FROM culinary."Categories" WHERE "Slug" = 'lab2-chuong-category-' || (i % 20)),
    'lab2-chuong-demo-author', timestamptz '2026-10-01 08:00:00+07' - i * interval '1 day',
    false, decode(md5('fr-srch-demo-version-' || i), 'hex')
FROM samples
ON CONFLICT ("Id") DO NOTHING;
COMMIT;
SELECT "Status", count(*) AS recipes FROM culinary."Recipes"
WHERE "Slug" LIKE 'fr-srch-demo-%' GROUP BY "Status" ORDER BY "Status";