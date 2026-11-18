# Перевірка черги еталонних фото в Docker (фаза 1 плану PLAN_04).
#   powershell -File backend\Microservices\scripts\test_reference_queue.ps1
# Потрібні запущені контейнери gateway, outfit-offer-service, image-service, parse-image-service, online-shelf-db.
# Файли фото потрапляють у D:\visualStudio\Projects\OnlineShelf\images (том ImageService), рядки в схему outfit_offer_service.
param(
    [int]$Count = 20,
    [string]$PhotosDir = "D:\visualStudio\Projects\OnlineShelf\backend\Microservices\ParseImageService\training_sandbox\data\images\val",
    [string]$Gateway = "http://localhost:5000",
    # Фіксований власник: повторний запуск показує дублікати, а не нові фото
    [string]$Owner = "11111111-2222-3333-4444-555555555555",
    [int]$TimeoutMinutes = 15
)

$ErrorActionPreference = "Stop"

function Invoke-Sql([string]$sql) {
    # SQL іде через stdin: Windows PowerShell 5.1 при передачі -c втрачає лапки в іменах таблиць
    $sql | docker exec -i online-shelf-db psql -U admin -d onlineShelf_db -At -F " | "
}

function Send-Photo([string]$batchId, [string]$path) {
    # curl.exe з Windows: multipart із полями ownerUserId і file
    curl.exe -s -w " HTTP %{http_code}" -F "ownerUserId=$Owner" -F "file=@$path" "$Gateway/offers/reference/batches/$batchId/photos"
}

Write-Host "== 1. Партія" -ForegroundColor Cyan
$batch = Invoke-RestMethod -Method Post -Uri "$Gateway/offers/reference/batches" -ContentType "application/json" `
    -Body (@{ ownerUserId = $Owner; name = "docker check $(Get-Date -Format s)" } | ConvertTo-Json)
$batchId = $batch.id
Write-Host "batch $batchId"

Write-Host "== 2. Завантаження $Count фото" -ForegroundColor Cyan
$files = Get-ChildItem $PhotosDir -Filter *.jpg | Sort-Object Name | Select-Object -First $Count
foreach ($f in $files) { Write-Host ("{0,-14} {1}" -f $f.Name, (Send-Photo $batchId $f.FullName)) }

Write-Host "== 3. Негативні випадки" -ForegroundColor Cyan
Write-Host "дублікат першого файла (очікується 200, duplicate true):"
Send-Photo $batchId $files[0].FullName
Write-Host "`nне картинка під іменем .jpg (очікується 400):"
$fake = Join-Path $env:TEMP "fake.jpg"
Set-Content -Path $fake -Value "це не фото" -Encoding utf8
Send-Photo $batchId $fake
Write-Host "`nнеіснуюча партія (очікується 404):"
Send-Photo ([guid]::NewGuid()) $files[0].FullName
Write-Host ""

Write-Host "== 4. Фото з EXIF-поворотом (вертикальне з телефона)" -ForegroundColor Cyan
$exif = Join-Path $env:TEMP "exif_portrait.jpg"
# Піксельні дані повернуті на бік, а EXIF Orientation = 6 каже показувати їх вертикально
python -c "from PIL import Image; im=Image.open(r'$($files[1].FullName)').convert('RGB').rotate(90, expand=True); e=Image.Exif(); e[0x0112]=6; im.save(r'$exif', exif=e, quality=92); print('saved', im.size)"
Send-Photo $batchId $exif
Write-Host ""

Write-Host "== 5. Очікування обробки (до $TimeoutMinutes хв)" -ForegroundColor Cyan
$deadline = (Get-Date).AddMinutes($TimeoutMinutes)
$started = Get-Date
do {
    Start-Sleep -Seconds 5
    $left = Invoke-Sql "select count(*) from outfit_offer_service.""ReferencePhotos"" where ""BatchId""='$batchId' and ""Status"" in ('Queued','Processing')"
    $counts = Invoke-Sql "select ""Status"", count(*) from outfit_offer_service.""ReferencePhotos"" where ""BatchId""='$batchId' group by 1 order by 1"
    Write-Host ("{0:N0} с  {1}" -f ((Get-Date) - $started).TotalSeconds, ($counts -join "; "))
} while ([int]$left -gt 0 -and (Get-Date) -lt $deadline)

Write-Host "== 6. Підсумок" -ForegroundColor Cyan
Write-Host "час обробки партії: $([int]((Get-Date) - $started).TotalSeconds) с"
Write-Host "статуси образів:"
Invoke-Sql "select o.""Status"", count(*), round(avg((select count(*) from outfit_offer_service.""ReferenceItems"" i where i.""OutfitId""=o.""Id"")),1) as avg_items from outfit_offer_service.""ReferenceOutfits"" o join outfit_offer_service.""ReferencePhotos"" p on p.""Id""=o.""PhotoId"" where p.""BatchId""='$batchId' group by 1"
Write-Host "помилки і спроби:"
Invoke-Sql "select ""FileName"", ""Status"", ""Attempts"", left(coalesce(""LastError"",''),80) from outfit_offer_service.""ReferencePhotos"" where ""BatchId""='$batchId' and (""Status""<>'Done' or ""Attempts"">1)"
Write-Host "розміри фото, у пікселях яких задано рамки (перші 5):"
Invoke-Sql "select ""FileName"", ""Width"", ""Height"" from outfit_offer_service.""ReferencePhotos"" where ""BatchId""='$batchId' order by ""CreatedAt"" limit 5"

Write-Host "== 7. EXIF: розмір _big.webp у браузері проти Width/Height у БД" -ForegroundColor Cyan
$row = Invoke-Sql "select ""BigImage"", ""Width"", ""Height"" from outfit_offer_service.""ReferencePhotos"" where ""BatchId""='$batchId' and ""FileName""='exif_portrait.jpg'"
Write-Host "БД: $row"
$big = ($row -split " \| ")[0]
if ($big) {
    $tmp = Join-Path $env:TEMP "exif_big.webp"
    curl.exe -s -o $tmp "$Gateway/shelfs/image/get-image/$big"
    python -c "from PIL import Image, ImageOps; im=Image.open(r'$tmp'); print('webp raw', im.size, 'orientation tag', im.getexif().get(0x0112)); print('webp як у браузері', ImageOps.exif_transpose(im).size)"
    Write-Host "Очікування: «як у браузері» збігається з Width x Height з БД (вертикальне фото)."
}
