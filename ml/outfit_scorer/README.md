# Скорер сумісності образів (ML-пайплайн)

Навчена заміна `StubGraphCompatibilityScorer` в OutfitNetworkService: CSA-Net (попарна сумісність) +
NGNN-lite (агрегація) на CLIP-ембедингах, навчання на Polyvore Outfits, експорт в ONNX.
Опис моделі, даних і метрик — `MODEL_CARD.md` (з'явиться після повного навчання).

Усі команди нижче запускаються **з теки `ml\outfit_scorer`** (PowerShell, Windows).

## 1. Середовище

Python 3.10–3.12 (torch 2.6.0, як у ParseImageService). Окреме середовище в `ml\.venv` (у `.gitignore`):

```powershell
cd <repo>\ml\outfit_scorer
python -m venv ..\.venv
..\.venv\Scripts\Activate.ps1          # якщо заблоковано: Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
python -m pip install --upgrade pip
# CUDA-збірка torch тієї самої версії, що в Parse (cu124 працює з RTX 3050, драйвер ≥ 528)
python -m pip install torch==2.6.0 --index-url https://download.pytorch.org/whl/cu124
python -m pip install -r requirements.txt
# ultralytics ставить torchvision з PyPI без CUDA — замінити на cu124 (інакше NMS YOLO падає на GPU)
python -m pip install torchvision==0.21.0 --index-url https://download.pytorch.org/whl/cu124 --force-reinstall --no-deps
python -c "import torch, torchvision; print(torch.__version__, torchvision.__version__, torch.cuda.is_available())"   # 2.6.0+cu124 0.21.0+cu124 True
```

## 2. Датасет

Polyvore Outfits (Vasileva et al., 2018), HF `mvasil/polyvore-outfits`: gated, потрібен акаунт HF
і схвалений запит на сторінці датасету (~4 ГБ картинок у parquet + ~200 МБ JSON).

```powershell
hf auth login
hf download mvasil/polyvore-outfits --repo-type dataset --local-dir D:\datasets\polyvore_outfits
```

Розпаковувати нічого не треба: картинки лежать у `data\{disjoint,nondisjoint}\{train,validation,test}.parquet`
(рядок = `item_id` + JPEG-байти). Структуру JSON-файлів описано на початку `src/data/polyvore.py`.

Шлях до датасету — змінна `POLYVORE_DIR` або `paths.polyvore_dir` у `config.yaml` (можна абсолютний):

```powershell
$env:POLYVORE_DIR = "D:\visualStudio\training_datasets\hogging_face\ml\data\polyvore_outfits"
```

Кеш ембедингів, прогони й звіти пишуться в `ml\data\` (у `.gitignore`).

## 3. Кроки

| # | Команда | Що робить | Час (оцінка) |
|---|---|---|---|
| 1 | `python -m src.data.check_dataset` | перевірка датасету → `ml\data\check_dataset_report.txt` | ~1 хв |
| 2 | `python -m pytest` | тести (синтетика; паритет з Parse — якщо є його код і ваги YOLO) | ~1 хв |
| 3 | `python -m src.data.embed --check-fp16 256` | паритет fp16 ↔ fp32 (косинус ≥ 0.999) | ~1 хв |
| 4 | `python -m src.data.embed --split valid --limit 500` | бенчмарк швидкості на 500 картинках | < 1 хв |
| 5 | smoke (нижче) | увесь пайплайн на ≤ 2 000 айтемах, 2 епохи | ~3 хв |
| 6 | `python -m src.data.embed --split all` | ембединги disjoint з YOLO-кропом: 156 659 картинок | ~75 хв |
| 7 | `python -m src.train` | CSA + NGNN-lite, early stopping | 5–15 хв |
| 8 | `python -m src.train --gnn-rounds 0 --name pairwise` | baseline «pairwise без GNN» | 5–15 хв |
| 9 | `python -m src.eval --checkpoint ... --pairwise ...` | таблиця метрик на test | ~1 хв |
| 10 | `python -m src.export_onnx --checkpoint ... --metrics ...` | `OutfitNetworkService\Models\outfit_gnn.onnx` | секунди |
| 11 | `python -m src.parity_check --checkpoint ...` | torch ↔ onnxruntime на 1 000 графах | ~10 с |

Під час першого запуску `embed` завантажує `patrickjohncyh/fashion-clip` (~600 МБ) у кеш HF. Той самий кеш
змонтовано в контейнер ParseImageService (`~/.cache/huggingface`), а модель там вантажиться лише локально,
тож у нового розробника вона має з'явитися в кеші до старту Parse. Порівняння CLIP і FashionCLIP на
детекції матеріалу: `python -m src.eval_material`. Зміна моделі ембедингів — три кроки разом: `MODEL_NAME`
у Parse, модель скорера в OutfitNetworkService, `Embeddings:CurrentModel` у ShelfsService (backfill перерахує речі).

**Smoke (крок 5)** — лише айтеми перших 200 образів/питань кожного спліту:

```powershell
python -m src.data.embed --limit-outfits 200
python -m src.train --emb-suffix _fashion-clip_yolo_o200 --limit-outfits 200 --epochs 2 --name smoke
```

**Ембединги (крок 6).** Як і в ParseImageService, перед CLIP річ вирізається YOLO-детектором
(`embedding.crop: yolo` у `config.yaml`: ваги `fashion_best.pt` з Parse, `conf 0.05`, дозволені класи =
типи з БД; якщо нічого не знайдено — усе фото). Без цього модель навчалась би на векторах повних фото,
а в сервісі отримувала б вектори кропів (косинус між ними в середньому 0.89). Назва кешу містить модель
і кроп: `disjoint_<split>_fashion-clip_yolo.npy`; для CLIP — `disjoint_<split>_yolo.npy`, без кропу
(`--crop none`) суфікса `_yolo` немає. train бере суфікс з `config.yaml` або `--embedding-model`/`--emb-suffix`,
eval — із чекпоінта.

Рахуються у fp32 (`embedding.precision`; fp16 — лише якщо крок 3 пройшов: 2026-10-25 він не пройшов,
min 0.9875), батч 128, 4 потоки препроцесингу;
при нестачі відеопам'яті батч зменшується автоматично. Прогрес зберігається кожні 4 096 картинок:
після зупинки (Ctrl+C, сон ноутбука) та сама команда продовжить з місця зупинки.
Повторно перерахувати: видалити `ml\data\embeddings\disjoint_<split>_fashion-clip_yolo.npy`.

Виміряна швидкість на RTX 3050 Mobile: з YOLO-кропом ~42 img/s (YOLO іде по одній картинці, як у Parse,
щоб бокси збігались) → 156 659 картинок ≈ 64 хв; без кропу ~450 img/s ≈ 6 хв.

**Навчання (кроки 7–8)** йде на кешованих ембедингах, модель ~230 тис. параметрів, вміщається
в 4 ГБ з великим запасом. Лог кожної епохи — `ml\data\runs\<час>_<назва>\metrics.jsonl`, найкращий
за valid Compat AUC чекпоінт — `best.pt` там само.

**Оцінка й експорт (кроки 9–11):**

```powershell
python -m src.eval --checkpoint ..\data\runs\<run>_csa_ngnn\best.pt --pairwise ..\data\runs\<run>_pairwise\best.pt
python -m src.export_onnx --checkpoint ..\data\runs\<run>_csa_ngnn\best.pt --metrics ..\data\eval\<час>_test\metrics.json
python -m src.parity_check --checkpoint ..\data\runs\<run>_csa_ngnn\best.pt
```

## 4. Контракт ONNX (OutfitNetworkService)

| Вхід/вихід | Форма | Зміст |
|---|---|---|
| `node_features` | float32[N, 512] | CLIP-ембединг (L2-норм.), відсутній = нулі |
| `node_category` | int64[N] | `OutfitSlot` 0..6 |
| `node_has_embedding` | float32[N] | 1/0; 0 → навчений вектор «немає фото» |
| `edge_index` | int64[2, E] | ребра (як `OutfitGraph.CreateComplete`) |
| `graph_score` | float32[1] | скор образу, [0, 1] |
| `edge_scores` | float32[E] | попарні скори, [0, 1], симетричні |

Метадані: `embedding_model`, `embedding_dim`, `category_vocab=OutfitSlot`, `model_version`, `trained_on`,
`metrics_json`. `tests/fixtures/tiny_scorer.onnx` — той самий контракт з випадковими вагами для .NET-тестів
(`python -m src.export_onnx --random --out tests/fixtures/tiny_scorer.onnx`).

## 5. Структура

```
config.yaml              гіперпараметри, шляхи, мапінг semantic_category → OutfitSlot
src/data/polyvore.py     читання метаданих, сплітів, compatibility, FITB
src/data/check_dataset.py  перевірка датасету
src/data/embed.py        CLIP-ембединги з parquet → кеш (.npy + .ids.txt), продовження після зупинки
src/data/sampler.py      позитиви, негативи (заміна в межах слота), підобрази під шаблони OfferService
src/model/scorer.py      модель (dense, ONNX-friendly) + обгортка під контракт
src/evaluation.py        AUC, FITB, скоринг наборів графів
src/train.py  src/eval.py  src/baselines.py  src/export_onnx.py  src/parity_check.py
tests/                   pytest; tests/synthetic.py — синтетичний датасет і крихітна CLIP для тестів
```

## 6. Паритет ембедингів з ParseImageService

`tests/test_embed.py` порівнює вектор з `ml` і з `VisualEmbedder` Parse (його файл підвантажується
в тесті) на тих самих байтах фото: PIL → RGB → BGR-кроп → `VisualEmbedder.embed` проти
`src.data.embed`. Окремо `test_yolo_crop_matches_parse_clothing_detector` запускає `ClothingDetector`
Parse і `YoloCropper` з `ml` на тих самих фото (з `POLYVORE_DIR` — ще 3 продуктові фото Polyvore)
і вимагає попіксельного збігу кропів. Ембединги без параметрів перевіряються на крихітній CLIP
з випадковими вагами; зі справжньою моделлю і власними кропами:

```powershell
$env:CLIP_MODEL = "patrickjohncyh/fashion-clip"
$env:PARITY_IMAGES_DIR = "<тека з 3 фото-кропами>"
python -m pytest tests/test_embed.py -k parity
```
