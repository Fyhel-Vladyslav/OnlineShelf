# OnlineShelf — інструкції для Claude Code

## Changelog
- Після кожної тематичної зміни в коді дописуй один рядок у `CHANGELOG.md` у корені проєкту.
- Один рядок = одна логічна зміна, коротко англійською, наприклад:
  - `- Added GetItems request (ShelfsService)`
  - `- Changed proto to pass new field in AnalyzeClothing request (parse-image.proto)`
- Рядки групуються під заголовком `## YYYY-MM-DD` (поточна дата, найновіші зверху).
- Кілька змін в одному коміті означають кілька рядків. Рядок додається в тому ж коміті, що й сама зміна.
