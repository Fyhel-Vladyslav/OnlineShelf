# Завдання для Claude Code №3: ML-пайплайн скорера сумісності (MVP)

> Мета: замінити заглушку `StubGraphCompatibilityScorer` (константа 0.75) на натреновану модель, яка дає **осмислений і різний** скор для образів з реального гардероба. Це MVP: потрібна робоча модель з чесно виміряними метриками, а не state-of-the-art.
> Перед роботою прочитай `CLAUDE.md` (правило CHANGELOG), `outfit-compatibility-scoring-theory.md` і розділ 5 у `Claude outputs/REPORT_01_project_research.md`.

## 1. Обмеження (обов'язково)

- **Не заходь** у `backend/Microservices/ParseImageService/training_sandbox/data/images/train/`. Решту `training_sandbox` читати можна, але Polyvore там немає (перевірено 2026-10-10): там YOLO-датасет детекції (29 класів Fashionpedia) і прогони YOLO.
- Не читай `.env` та інші файли з секретами.
- Довгі задачі (завантаження датасету, обчислення ембедингів для всього Polyvore, повне навчання) **не запускай сам**. Підготуй команду, оціни час і зупинись, далі запускаю я. Запускати можна smoke-прогони на підмножині (≤ 2 000 айтемів, ≤ 2 епохи).
- Не коміть ваги, датасети й кеші ембедингів у git (окрім фінального `outfit_gnn.onnx`, див. 6.4).
- Нові бібліотеки дозволені лише в `ml/`. У .NET-сервісах нових пакетів не додавай: `Microsoft.ML.OnnxRuntime` уже є.
- Архітектурні зміни поза описаними в розділі 3 спершу узгодь зі мною й аргументуй.

## 2. Що вже є (не переробляй)

- `ParseImageService` → `EmbedClothing`: YOLO-кроп → `openai/clip-vit-base-patch32` → `get_image_features` → L2-нормалізація, 512 float (`app/models/visual_embedder/visual_embedder.py`). Модель зберігається в `Item.VisualEmbedding` + `EmbeddingModel` (ShelfsService).
- OutfitOfferService: CSP + beam search за шаблонами `Top+Bottom+Shoes(+Outerwear)` і `FullBody+Shoes(+Outerwear)`, пакетний виклик `ScoreOutfits`. Слоти: `OutfitOfferService.Common/Models/OutfitSlot.cs` (`Unknown=0, Top=1, Bottom=2, FullBody=3, Shoes=4, Outerwear=5, Accessory=6`).
- OutfitNetworkService: `OnnxGraphCompatibilityScorer` підключається автоматично, якщо існує `Models/outfit_gnn.onnx`, інакше працює заглушка. `ScoringOrchestrator`: `Final = GraphLevel × Π C_j`.

## 3. Зміна контракту моделі (погоджено)

**Проблема.** Зараз вхід `node_categorical int64[N,4]` = `[AttributeType, Season, Pattern, Matterial]`, тобто ключі з БД. На Polyvore таких ключів немає, сезон і патерн з Parse зараз завжди 0, а самі id залежать від порядку seed. Модель, навчена на Polyvore, ці входи не зможе ні отримати, ні осмислено використати.

**Рішення.** Категорія вузла = слот образу (`OutfitSlot`), спільний для Polyvore і нашої БД:

| Вхід/вихід | Було | Стає |
|---|---|---|
| `node_features` | `float32[N, F]`, F = max довжина, може бути 0 | `float32[N, 512]`, завжди розмірність моделі; відсутній вектор = нулі |
| `node_categorical` | `int64[N, 4]` (id з БД) | **прибрати** |
| `node_category` | — | `int64[N]`, значення `OutfitSlot` (0..6) |
| `node_has_embedding` | — | `float32[N]`, 1/0 (модель підставляє навчений токен «немає фото») |
| `edge_index` | `int64[2, E]` | без змін |
| `graph_score`, `edge_scores` | без змін | без змін, обидва в [0, 1] |

Метадані моделі (`onnx` `metadata_props`): `embedding_model=openai/clip-vit-base-patch32`, `embedding_dim=512`, `category_vocab=OutfitSlot`, `model_version`, `trained_on`, `metrics_json`.

Атрибути `Season/Pattern/Matterial` у моделі MVP **не використовуються**: це роль CSP-фільтра і `Π C_j`. Додамо їх у модель пізніше, коли Parse почне їх правильно повертати і з'являться власні дані.

## 4. Структура `ml/`

```
ml/outfit_scorer/
  README.md               # як відтворити все з нуля: команди, час, вимоги
  MODEL_CARD.md           # дані, архітектура, метрики, обмеження (піде в текст магістерської)
  requirements.txt        # UTF-8, мінімальний; torch/transformers тих самих версій, що в ParseImageService
  config.yaml             # гіперпараметри, шляхи (data_dir за замовчуванням ml/data)
  src/
    data/polyvore.py      # читання метаданих, сплітів, compatibility/FITB, мапінг категорій → OutfitSlot
    data/embed.py         # обчислення CLIP-ембедингів → .npy/.parquet (кеш), з відновленням після зупинки
    data/sampler.py       # позитиви/негативи, підобрази під наші шаблони
    model/scorer.py       # модель (розділ 5), dense, ONNX-friendly
    train.py  eval.py  export_onnx.py  parity_check.py
    baselines.py          # random, mean-cosine CLIP, (опційно) pairwise без GNN
  tests/                  # pytest: мапінг категорій, семплер, форми тензорів, паритет
ml/data/                  # у .gitignore: датасет, кеш ембедингів, чекпоінти
```

## 5. Етапи

### 5.1. Дані (Polyvore Outfits, Vasileva et al. 2018)
- **Джерело:** Hugging Face `mvasil/polyvore-outfits` (gated: потрібен акаунт HF і прийняття умов на сторінці датасету), ~6 ГБ. Там є `polyvore_item_metadata.json`, `categories.csv`, `polyvore_outfit_titles.json`, теки `disjoint/`, `nondisjoint/`, `data/` (картинки). Завантажую я:
  ```
  pip install -U huggingface_hub
  hf auth login
  hf download mvasil/polyvore-outfits --repo-type dataset --local-dir ml/data/polyvore_outfits
  ```
  Якщо картинки лежать в архіві, напиши в README команду розпакування.
- Варіант `disjoint` основний (чесніша оцінка), `nondisjoint` опційно. Використовуй офіційні спліти, `compatibility_{split}.txt` і `fill_in_blank_{split}.json`.
- Скрипт перевірки датасету (`src/data/check_dataset.py`): кількість айтемів/образів, наявність картинок, розподіл `semantic_category`. Спершу перевір фактичну структуру після завантаження: назви файлів у HF-версії можуть відрізнятися від статті. Якщо датасету ще немає, зупинись.
- Мапінг `semantic_category → OutfitSlot` (винести в конфіг): `tops→Top`, `bottoms→Bottom`, `all-body→FullBody`, `shoes→Shoes`, `outerwear→Outerwear`, `bags/accessories/jewellery/hats/sunglasses/scarves→Accessory`. Решту категорій відкинь і порахуй, скільки втрачено.

### 5.2. Ембединги
- **Точно ту саму** функцію, що й у Parse: `CLIPProcessor` + `get_image_features` + L2-нормалізація, `openai/clip-vit-base-patch32`, ті самі версії `transformers`/`torch`. Не імпортуй код Parse напряму: скопіюй логіку й додай тест паритету (5.6).
- Кеш: `ml/data/embeddings/{split}.npy` + індекс `item_id → row`. Батчинг, підтримка CUDA, якщо вона є, продовження з місця зупинки.
- Залізо: **RTX 3050 Mobile (4 ГБ VRAM)**. Використовуй fp16 (`torch.autocast`), batch 64–128, `DataLoader` з `num_workers` 4 і `pin_memory`; при OOM автоматично зменшуй batch. Перед повним прогоном перевір паритет fp16 ↔ fp32 (косинус ≥ 0.999); якщо паритету немає, рахуй у fp32.
- Оціни час на GPU на 500 картинках і запиши в README. Навчання самої моделі на кешованих ембедингах легке і має вміщатися в 4 ГБ.

### 5.3. Модель (`scorer.py`)
Dense-форма, без PyTorch Geometric, щоб ONNX-експорт був тривіальним. Типові N = 3–5.
1. **Вузол:** `h_i = MLP(x_i)` (512→256→d, d=64) + `emb(category_i)`; якщо `has_embedding=0`, замість `MLP(x_i)` береться навчений вектор `missing`.
2. **Попарна сумісність (CSA-Net):** K=5 навчених масок `m_k ∈ R^d`; `ω = softmax(MLP([emb(c_i); emb(c_j)]))`; `p_i = Σ ω_k (m_k ⊙ h_i)`; `s_ij = σ(MLP([p_i; p_j; |p_i−p_j|; p_i⊙p_j]))`. Скор має бути симетричним (`s_ij = s_ji`), перевір тестом.
3. **Агрегація (NGNN-lite):** 2 раунди message passing по повному графу з вагами ребер `s_ij` (у dense-формі — матриця N×N), далі attention-pooling → `σ(·)` → `graph_score`.
4. `edge_scores` = `s_ij`, зібрані за `edge_index` (через `gather`, без scatter).

### 5.4. Навчання
- **Позитиви:** реальні образи + **підобрази під наші шаблони** (Top+Bottom+Shoes(+Outerwear), FullBody+Shoes(+Outerwear)), бо саме такі графи прийдуть з OutfitOffer.
- **Негативи:** заміна 1–2 айтемів на випадкові **того самого слота** (інакше модель вчитиметься розрізняти категорії, а не сумісність).
- **Loss:** BCE на `graph_score` + допоміжний BCE на `edge_scores` (пари з реального образу = 1, пари із заміненим айтемом = 0), вага ~0.3.
- **Аугментації:** з імовірністю 0.1 обнуляти ембединг вузла (`has_embedding=0`), а до ембедингів додавати невеликий гаусівський шум. Це знижує розрив між продуктовими фото Polyvore і фото користувачів.
- Early stopping за валідаційним AUC, фіксований seed, лог метрик у `ml/data/runs/<timestamp>/`.

### 5.5. Оцінка (обов'язкова таблиця для звіту)
| Модель | Compat. AUC (test) | FITB acc. (test) |
|---|---|---|
| Random | 0.50 | 25% |
| Mean-cosine CLIP (без навчання) | | |
| Pairwise CSA без GNN | | |
| **CSA + NGNN-lite (наша)** | | |

- Для довідки, у статті Type-Aware на disjoint: AUC ≈ 0.84, FITB ≈ 55%. **Критерій MVP:** наша модель помітно краща за mean-cosine. Орієнтир: AUC ≥ 0.75.
- Додатково: оцінка на підобразах із 3–4 айтемів (наш реальний випадок) і розподіл `graph_score` (гістограма), щоб скори не злипалися біля 0.5.

### 5.6. Експорт і паритет
- `export_onnx.py`: opset 17, динамічні осі `N` і `E`, метадані з розділу 3 → `backend/Microservices/OutfitNetworkService/Models/outfit_gnn.onnx`.
- `parity_check.py`: на 1 000 випадкових графах (N = 2..6) `|torch − onnxruntime| < 1e-4` для `graph_score` і `edge_scores`.
- Тест паритету ембедингів: на 3 фото-кропах вектор з `ml` і вектор з ParseImageService (`VisualEmbedder`) мають косинус ≥ 0.999. Якщо Parse без docker не запускається, опиши, як я перевірю це вручну.
- Окремо збережи `tests/fixtures/tiny_scorer.onnx` (випадкові ваги, той самий контракт, < 1 МБ) для .NET-тестів.

### 5.7. Інтеграція в .NET
1. `offer-network.proto` (один файл, OutfitOffer підключає його як клієнт): в `ItemNode` додати `int32 slot = 11` (значення `OutfitSlot`) і `string embedding_model = 12`.
2. OutfitOfferService (`NetworkOutfitScorer.cs`): заповнювати `slot` і `embedding_model` з `WardrobeItem`.
3. OutfitNetworkService:
   - `ItemNode` (entity) + мапінг у `OutfitNetworkGrpcService.cs`: поля `Slot`, `EmbeddingModel`;
   - `OnnxGraphCompatibilityScorer`: новий контракт; розмірність і `embedding_model` читати з метаданих моделі при старті; вузол без вектора **або** з іншим `embedding_model` → нулі + `has_embedding=0` (зараз, якщо в усіх вузлів немає вектора, `Max(...)` дає F=0, і модель упаде);
   - залогувати при старті `model_version` і метрики з метаданих;
   - `.csproj`: копіювати `Models/**` у output; `Dockerfile` перевірити. У `docker-compose.yml` змонтувати `./OutfitNetworkService/Models:/app/Models:ro`, щоб оновлювати модель без перезбирання образу.
4. Тести: новий `OutfitNetworkService/tests/OutfitNetworkService.Tests` (за зразком `OutfitOfferService/tests`): форми тензорів, вузли без ембединга, чужий `embedding_model`, симетрія попарних скорів, `tiny_scorer.onnx` повертає значення в [0, 1], `ScoringOrchestrator` з фейковим скорером.
5. Кожна тематична зміна = рядок у `CHANGELOG.md`.

### 5.8. Перевірка на реальному гардеробі (MVP-критерій «можна показувати людям»)
- Скрипт `ml/outfit_scorer/src/sanity_wardrobe.py`: через gateway (`POST /offers/generate`, токен передаю я) або напряму gRPC отримати топ-N образів для мого користувача й зберегти їх у markdown-таблицю (назви, слоти, `finalScore`, `pairwiseScores`).
- Перевірки: скори різні (std > 0.05); `elapsedMs` генерації ≤ 500 мс (NFR-1); ручна оцінка: я позначаю 20 образів «ок / не ок», ти рахуєш точність топ-5 і порівнюєш із заглушкою.
- Seed-скрипт `scripts/seed_test_wardrobe.sql` має речі без фото. Для цієї перевірки потрібні речі з фото: якщо їх мало, напиши, скільки і яких слотів бракує.

## 6. Результат

1. Код у `ml/outfit_scorer/` з README, MODEL_CARD, тестами (`pytest` зелений).
2. `outfit_gnn.onnx` + `tiny_scorer.onnx`.
3. Зміни в OfferService/NetworkService з тестами, `dotnet test` зелений, рядки в `CHANGELOG.md`.
4. Розмір `outfit_gnn.onnx`: якщо ≤ 20 МБ, комітимо звичайно, інакше пропонуй Git LFS або завантаження скриптом.
5. `Claude outputs/REPORT_03_ml_scorer_mvp.md` українською: що зроблено (файли); таблиця метрик (5.5); паритет (5.6); результати перевірки 5.8; відомі обмеження (domain gap Polyvore ↔ фото користувача, якість YOLO-кропу: `fashion_best.pt` = `best.pt` з прогону `fashion_yolov8s_prod`, який зупинився на 3-й епосі з 50 при mAP50 ≈ 0.34; атрибути поза моделлю); відкриті питання; що робити далі (fine-tune на власних `TrainingCollection`, FashionCLIP, Gap Analysis).

## 7. Порядок роботи і зупинки

1. 5.1 (перевірка/опис датасету) → **стоп**, якщо датасету немає.
2. Каркас `ml/`, мапінг, семплер, модель, тести, smoke-прогін на підмножині → **стоп**: дай мені команди повного обчислення ембедингів і навчання з оцінкою часу.
3. Після моїх прогонів: оцінка, baselines, експорт, паритет.
4. Інтеграція .NET (5.7) можна робити паралельно з п. 3, використовуючи `tiny_scorer.onnx`.
5. Перевірка 5.8 → звіт → коротко (3–5 рядків) напиши в чаті підсумок.
