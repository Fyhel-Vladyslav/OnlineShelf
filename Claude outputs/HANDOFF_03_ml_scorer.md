# HANDOFF №3: ML-скорер сумісності (MVP) — передача роботи в локальний Claude Code

> Документ для нового чату Claude Code, що працює **на комп'ютері користувача** (Windows, PowerShell).
> Тут зібрано все, що зроблено у хмарній сесії 2026-10-09…2026-10-24: контекст, рішення, результати,
> незавершені кроки і правила роботи з користувачем. Повний текст завдання — у Додатку А.

---

## 0. Інструкції для нового чату (прочитай першим)

1. **Мова спілкування — українська.** Код, коментарі в коді — як у репо (українські коментарі, англійські рядки CHANGELOG).
2. **Git не чіпати.** Користувач явно попросив: жодних `git commit`, `git push`, створення/перемикання гілок,
   `git apply`, `git stash` тощо. Комітами й пушем керує сам користувач. Читати стан (`git status`, `git diff`,
   `git log`, `git branch --show-current`) можна.
3. **CHANGELOG.** На кожну свою тематичну зміну в коді — один рядок у `CHANGELOG.md` у корені (правило з `CLAUDE.md`):
   під заголовком `## YYYY-MM-DD` (поточна дата, найновіші зверху), формат
   `- <Added|Changed|Fixed|Removed> <що саме> (<сервіс/файл>)`, англійською.
4. **Інструкції користувачу — детальні.** Користувач прямо скаржився, що короткі інструкції незрозумілі.
   Коли просиш щось зробити: повні абсолютні шляхи, з якої теки запускати, що кожна команда робить,
   скільки триває, який вивід очікувати, що робити при помилці, що надіслати назад. Не використовуй
   заглушки на кшталт `...` чи `<run>` без пояснення, звідки взяти значення — краще знайди значення сам.
5. **Тепер ти працюєш на машині користувача** — можеш сам дивитися файли, запускати тести й короткі команди.
   Обмеження із завдання щодо довгих задач (повне обчислення ембедингів, повне навчання) формально діють:
   спершу запропонуй команду з оцінкою часу й спитай, чи запускати самому, чи користувач запустить сам.
   Навчання займає ~8 с/епоху (див. §6), тож перенавчання — це хвилини, а не години.
6. **Не читати** `.env` та інші файли із секретами. **Не заходити** в
   `backend/Microservices/ParseImageService/training_sandbox/data/images/train/`.
7. **Архітектурні зміни поза описаним у завданні спершу узгоджувати з користувачем** (з аргументацією).
8. Перед будь-якою дією **перевір фактичний стан** (§3): гілку, наявність файлів, що вже запускалося.

---

## 1. Що за задача

Завдання №3 (повний текст — Додаток А): замінити заглушку `StubGraphCompatibilityScorer` (константа 0.75) в
OutfitNetworkService на натреновану модель сумісності образу: CLIP-ембединги речей → CSA-Net (попарна сумісність)
+ NGNN-lite (агрегація) → ONNX → .NET. Навчання на Polyvore Outfits (disjoint). Контракт моделі змінено (розділ 3
завдання): категорія вузла = `OutfitSlot` (0..6) замість id атрибутів з БД.

Порядок роботи із завдання (§7): 1) датасет → 2) каркас `ml/`, модель, тести, smoke → **стоп** →
3) оцінка, baselines, експорт, паритет → 4) .NET-інтеграція (5.7, можна паралельно з 3) → 5) перевірка на
реальному гардеробі (5.8) → звіт `Claude outputs/REPORT_03_ml_scorer_mvp.md` → коротко підсумок у чаті.

**Де ми зараз:** кроки 1–3 фактично пройдені (є навчені моделі, оцінка, ONNX, паритет), але треба
перенавчити довше і вирішити, яку модель ставити (§7). Крок 4 (.NET), 5.8, MODEL_CARD і звіт — не почато.

---

## 2. Середовище користувача (перевірені факти)

| Що | Значення |
|---|---|
| ОС / shell | Windows, PowerShell (Windows PowerShell 5.1) |
| Корінь репо | `D:\visualStudio\Projects\OnlineShelf` |
| Датасет Polyvore | `D:\visualStudio\training_datasets\hogging_face\ml\data\polyvore_outfits` (поза репо, так і задумано) |
| Python | 3.12.6, `C:\Users\vladf\AppData\Local\Programs\Python\Python312\python.exe` |
| Лаунчер `py` | **відсутній** — використовувати `python` |
| venv для ML | `D:\visualStudio\Projects\OnlineShelf\ml\.venv` (активація: `..\.venv\Scripts\Activate.ps1` з `ml\outfit_scorer`) |
| GPU | RTX 3050 Mobile, 4 ГБ VRAM |
| Робоча тека ML-команд | `D:\visualStudio\Projects\OnlineShelf\ml\outfit_scorer` (усі `python -m src...` — звідси) |
| Змінна датасету | `$env:POLYVORE_DIR = "D:\visualStudio\training_datasets\hogging_face\ml\data\polyvore_outfits"` — **живе лише в поточному вікні PowerShell**, задавати в кожному новому |
| Кеші/прогони | `D:\visualStudio\Projects\OnlineShelf\ml\data\` (`embeddings\`, `runs\`, `eval\`) |

Користувач не мав доступу до моїх хмарних комітів: код `ml\outfit_scorer` він отримав **zip-архівом** і
розпакував у корінь репо. Хмарні коміти не запушені (GitHub повертав 403) — на них не розраховуй.

---

## 3. Що перевірити на старті (стан не до кінця відомий)

```powershell
cd D:\visualStudio\Projects\OnlineShelf
git branch --show-current
git status --short
Test-Path CHANGELOG.md, CLAUDE.md, "backend\Microservices\OutfitNetworkService\OutfitNetworkService.csproj", "backend\Microservices\ParseImageService\app\models\visual_embedder\visual_embedder.py"
Select-String -Path .gitignore -Pattern "/ml/data/"
```

- Увесь потрібний код (OutfitNetworkService, `EmbedClothing`/`VisualEmbedder` у Parse, `CLAUDE.md`, `CHANGELOG.md`,
  `outfit-compatibility-scoring-theory.md`, `Claude outputs/REPORT_01_project_research.md`) є лише в гілці
  **`offer_service`** (у `main` його немає). Якщо локально відкрита `main` — скажи користувачу, що для .NET-частини
  потрібна `offer_service` (перемикає він сам).
- `outfit_gnn.onnx` уже експортовано в `backend\Microservices\OutfitNetworkService\Models\outfit_gnn.onnx`
  (export створює теку, якщо її не було, тож це ще не доводить наявність сервісу — перевір `.csproj`).
- **`.gitignore` і `CHANGELOG.md` швидше за все НЕ оновлені**: патч `gitignore_changelog.patch` користувач
  запускав не з тієї теки (з теки датасету), він не застосувався. Див. §8, пункт 1.

---

## 4. Датасет Polyvore Outfits (структура HF `mvasil/polyvore-outfits`, перевірено)

Завантажено повністю (gated, доступ отримано). **Нічого розпаковувати не треба** — картинки в parquet.

```
polyvore_outfits/
  polyvore_item_metadata.json   100 МБ, 251 008 айтемів: {item_id: {url_name, description, catgeories, title, related, category_id, semantic_category}}
  polyvore_outfit_titles.json   {set_id: {url_name, title}}
  categories.csv                category_id,fine_name,semantic_category (є "2,undefined," без semantic)
  disjoint/ nondisjoint/        {train,valid,test}.json, compatibility_{split}.txt, fill_in_blank_{split}.json, typespaces.p
  data/disjoint/ data/nondisjoint/   {train,validation,test}.parquet  (! "validation", а не "valid")
  maryland_polyvore_hardneg/    compatibility/FITB для Maryland Polyvore — НЕ використовується (образів Maryland у датасеті немає)
  README.md .gitattributes .gitignore .cache/
```

- `{split}.json`: `[{"set_id": "...", "items": [{"item_id": "...", "index": 1}, ...]}]`.
- `compatibility_{split}.txt`: `label set_index set_index ...`, де `set_index = "<set_id>_<index>"`.
- `fill_in_blank_{split}.json`: `[{"question": [set_index...], "answers": [4 x set_index], "blank_position": k}]`.
  **Правильна відповідь у довільній позиції** — визначається як варіант з тим самим `set_id`, що в питанні,
  і `index == blank_position`.
- parquet: один row group на файл, колонки `item_id: string`, `image: struct<bytes: binary, path: string>`
  (JPEG). Айтеми кожного parquet **на 100 % збігаються** з айтемами відповідного JSON-спліту.

| | disjoint | nondisjoint |
|---|---|---|
| образи train / valid / test | 16 995 / 3 000 / 15 145 | 53 306 / 5 000 / 10 000 |
| айтеми (parquet rows) train / valid / test | 71 967 / 14 657 / 70 035 | 204 679 / 25 132 / 47 854 |
| compatibility рядків | 33 990 / 6 000 / 30 290 | 106 612 / 10 000 / 20 000 |
| FITB питань | 16 995 / 3 000 / 15 145 | 53 306 / 5 000 / 10 000 |
| parquet розмір | 1,4 ГБ | 2,4 ГБ |

`semantic_category` (11 значень, **усі мапляться на слоти, втрат 0**): shoes 44 850, jewellery 41 414, bags 40 717,
tops 32 998, bottoms 27 670, all-body 18 478, outerwear 17 065, sunglasses 9 874, accessories 6 973, hats 6 071,
scarves 4 898. Мапінг у `config.yaml`: tops→Top(1), bottoms→Bottom(2), all-body→FullBody(3), shoes→Shoes(4),
outerwear→Outerwear(5), bags/accessories/jewellery/hats/sunglasses/scarves→Accessory(6).

---

## 5. Що побудовано: `ml/outfit_scorer/`

```
README.md            відтворення з нуля: середовище, датасет, кроки 1–11 з оцінкою часу, контракт ONNX
requirements.txt     torch==2.6.0, transformers==5.12.0, pillow==11.0.0, numpy==2.1.1 (як у Parse), pyyaml, pyarrow, tqdm, onnx, onnxruntime, pytest
                     (CUDA-torch ставиться окремо: pip install torch==2.6.0 --index-url https://download.pytorch.org/whl/cu124)
config.yaml          paths (data_dir ml/data, polyvore_dir), category_map, outfit_slots, embedding, model, train
pytest.ini           pythonpath=., testpaths=tests
src/data/polyvore.py       читання метаданих/сплітів/compatibility/FITB; SlotMapper; SplitData.limited(n), referenced_item_ids()
src/data/check_dataset.py  перевірка датасету → ml/data/check_dataset_report.txt
src/data/embed.py          CLIP-ембединги з parquet → ml/data/embeddings/{variant}_{split}.npy + .ids.txt
src/data/sampler.py        ItemTable, позитиви/негативи, підобрази під шаблони OfferService, collate + аугментації
src/model/scorer.py        OutfitScorer (dense CSA + NGNN-lite) і OnnxOutfitScorer (обгортка під контракт)
src/evaluation.py          roc_auc (ранговий), fitb_accuracy, build_eval_set, model_scorer, histogram
src/train.py               навчання, early stopping за valid Compat AUC → ml/data/runs/<YYYYMMDD_HHMMSS>_<name>/
src/eval.py                таблиця 5.5 на test + AUC на підобразах 3–4 + розподіл скорів → ml/data/eval/<ts>_test/
src/baselines.py           Random, Mean-cosine CLIP (pairwise без GNN = train --gnn-rounds 0)
src/export_onnx.py         ONNX opset 17, динамічні N/E, metadata_props → OutfitNetworkService/Models/outfit_gnn.onnx
src/parity_check.py        torch ↔ onnxruntime на 1 000 випадкових графах (N=2..6), допуск 1e-4
tests/                     26 тестів (усі зелені в хмарі): polyvore, sampler, model, metrics, onnx, embed, pipeline
tests/synthetic.py         синтетичний Polyvore (стиль-колір на образ → навчуваний сигнал) + крихітна CLIP з випадковими вагами
tests/fixtures/tiny_scorer.onnx   118 КБ, той самий контракт, випадкові ваги — для .NET-тестів
```

### Ключові рішення й відхилення (усі пояснені користувачу)

- **Ембединги** (`embed.py`): `CLIPImageProcessor` + `get_image_features` (+ `pooler_output` для transformers 5.x)
  + L2-нормалізація — та сама функція, що `VisualEmbedder` у Parse (Parse викликає `CLIPProcessor(images=...)`,
  який делегує в той самий image processor). Декодування PIL → RGB.
  - **Пул потоків замість DataLoader з процесами** (відхилення від тексту завдання): на Windows кожен воркер-процес
    копіював би весь parquet (один row group до 1,8 ГБ). PIL/numpy відпускають GIL; препроцесинг наступного батча
    йде паралельно з GPU; pinned memory на CUDA.
  - fp16 через `torch.autocast` (лише CUDA); `--check-fp16 N` порівнює fp16/fp32 (поріг косинуса 0.999).
  - CUDA OOM → батч автоматично ділиться навпіл.
  - Кеш частинами по 4 096 (`{name}.parts/part_XXXXX.npy` + `.ids.txt`; частина без ids.txt вважається недописаною),
    продовження після зупинки, у кінці злиття в один файл.
  - `--limit N` (бенчмарк, суфікс `_nN`), `--limit-outfits K` (smoke: лише айтеми перших K образів/питань, суфікс `_oK`).
- **Семплер**: позитив = реальний образ (обрізка до `max_nodes: 8`) або з імовірністю `template_prob: 0.5`
  підобраз Top+Bottom+Shoes(+Outerwear з p=0.5) / FullBody+Shoes(+Outerwear). Негатив = заміна 1–2 айтемів на
  випадкові **того самого слота** не з цього образу. Мітки ребер: обидва оригінальні = 1, інакше 0.
  Аугментації: обнулення ембединга з p=0.1 (`has_embedding=0`), гаусів шум σ=0.01 + ренормалізація.
- **Модель** (`scorer.py`), ~230 тис. параметрів:
  1. вузол `h = has·MLP((x−μ)·σ) + (1−has)·missing + emb(slot)`; MLP 512→256→64.
     **μ, σ — буфери моделі** (середній ембединг train і масштаб): без центрування навчання ~250 кроків стоїть
     на плато, з ним стартує в 2–3 рази швидше (перевірено на синтетиці). Зберігаються в чекпоінті та ONNX.
  2. CSA: K=5 масок, ω = softmax(MLP([e_i+e_j; e_i⊙e_j])) — симетричний вхід; p_i = h_i ⊙ Σω_k m_k;
     s_ij = σ(MLP([p_i; p_j; |p_i−p_j|; p_i⊙p_j])), **логіти усереднюються з транспонованими** → s_ij = s_ji точно.
  3. NGNN-lite: 2 раунди message passing з вагами s_ij (рядково нормовані), LayerNorm + residual;
     attention-pooling; голова на `[pooled; mean s; min s]` → graph_logit.
     `gnn_rounds: 0` → baseline «pairwise без GNN»: graph_score = середнє s_ij.
  4. ONNX-обгортка: `graph_score = sigmoid(...)` форми [1]; `edge_scores = gather(flat(s), src·N + dst)`.
- **Тест паритету з Parse** (`tests/test_embed.py`) виконує вихідний код `visual_embedder.py` (через `exec`, щоб не
  лишати `__pycache__` у Parse) і порівнює з `ml` на тих самих байтах; пропускається, якщо файлу Parse немає
  (гілка без EmbedClothing). За замовчуванням — крихітна CLIP із випадковими вагами; зі справжньою:
  `CLIP_MODEL=openai/clip-vit-base-patch32`, `PARITY_IMAGES_DIR=<3 фото-кропи>`, потрібен `opencv-python-headless==4.13.0.92`.
- **eval/train** групують графи за розміром; модель для сервісу обирати **за valid AUC**, не за test.

---

## 6. Результати (перший повний прогін користувача, 2026-10-24)

Ембединги disjoint пораховано повністю (`embed --split all`; `disjoint_test.npy` існує). Навчання: ~7–10 с/епоху.

Прогони (`D:\visualStudio\Projects\OnlineShelf\ml\data\runs\`):
- `20261024_222138_csa_ngnn` — 30 епох (ліміт `epochs: 30`), best_epoch **30**, best valid AUC **0.846**
- `20261024_223824_pairwise` — 30 епох, best_epoch **30**, best valid AUC **0.889**

Valid AUC GNN по епохах: 0.511, 0.671, 0.732, 0.738, 0.742, 0.760, 0.776, 0.772, 0.782, 0.777, 0.779, 0.792, 0.781,
0.784, 0.791, 0.793, 0.804, 0.808, 0.811, 0.815, 0.811, 0.825, 0.823, 0.836, 0.838, 0.837, 0.837, 0.845, 0.843, 0.846
→ **обидві моделі ще вчилися, навчання обрізане лімітом епох**, early stopping не спрацював.

Оцінка на test (`ml\data\eval\20261024_230004_test\report.md`):

| Модель | Compat AUC | FITB | AUC на підобразах 3–4 |
|---|---|---|---|
| Random | 0.502 | 24.6% | 0.508 |
| Mean-cosine CLIP (без навчання) | 0.638 | 42.1% | 0.603 |
| Pairwise CSA без GNN | **0.862** | **56.8%** | **0.731** |
| CSA + NGNN-lite (наша) | 0.827 | 51.0% | 0.719 |
| *стаття Type-Aware, disjoint* | *≈0.84* | *≈55%* | — |

Compat-питань 30 290, FITB 15 145, підобразів 14 468 (половина негативи), пропущено 0.
Розподіл graph_score (наша модель, підобрази): std **0.238** (критерій > 0.05), середнє позитивів 0.559,
негативів 0.377; гістограма 0.0–1.0 по 0.1: 1068, 1404, 1583, 1707, 1817, 1946, 2057, 1791, 894, 201.

**Критерій MVP виконано** (помітно краще за mean-cosine, AUC ≥ 0.75). Проблема: GNN поступається pairwise.

Експорт і паритет (з чекпоінта `20261024_222138_csa_ngnn`):
- `outfit_gnn.onnx` **923 КБ** → комітити звичайно (≤ 20 МБ, LFS не потрібен);
- parity: max |Δ| graph 3.6e-7, edges 3.6e-7 → `OK: tolerance 0.0001`.

**Невідомо** (користувач не надсилав): вивід `check_dataset`, `pytest` на його машині, `embed --check-fp16 256`
(тобто чи рахувалося у fp16), бенчмарк швидкості `embed --split valid --limit 500` (img/s; у README стоїть
заглушка «_заповнити після бенчмарку_»), вивід smoke. Попроси або запусти сам (вони короткі).

---

## 7. Відкрите рішення (чекає відповіді користувача)

Я запропонував і **чекаю згоди**:
1. **Перенавчити обидві моделі довше** (без змін коду): у `config.yaml` → `train.patience: 10`, запуск з `--epochs 150`.
2. **Якщо GNN і далі гірша за pairwise** — архітектурна зміна агрегації (потрібна згода користувача!):
   `graph_logit = logit(mean s_ij) + head([pooled; mean s; min s])`, тобто GNN дає **поправку** до середнього
   попарного скору (останній шар голови ініціалізувати нулями, щоб старт = pairwise). Тоді модель не гірша за
   pairwise за побудовою. Контракт ONNX/.NET не змінюється. Рекомендований варіант.
3. Альтернатива: у сервіс іде модель з кращим **valid** AUC (зараз pairwise 0.889 > GNN 0.846; pairwise має той
   самий ONNX-контракт), у звіті чесно написати, що GNN-агрегація на Polyvore виграшу не дала.
4. Також запитав, чи починати .NET-інтеграцію паралельно — відповіді ще немає (вона від вибору моделі не залежить).

---

## 8. Незавершені кроки для користувача (останні вимоги, ще НЕ виконані)

Пиши користувачу так само детально, як нижче. Можеш виконати частину сам, якщо користувач погодиться.

### 8.1. Оновити `.gitignore` і `CHANGELOG.md` (патч не застосувався)

Додати в `D:\visualStudio\Projects\OnlineShelf\.gitignore` (після блоку `**/training_sandbox/`), щоб кеші й
venv не потрапили в git:

```
# ML: датасети (Polyvore), кеш ембедингів, чекпоінти й прогони навчання
/ml/data/
/ml/.venv/
/ml/**/__pycache__/
```

Додати в `CHANGELOG.md` (над найновішим наявним заголовком дати) рядки моїх змін:

```
## 2026-10-10
- Added CLIP embedding cache from Polyvore parquet with resume, fp16 parity check and OOM batch fallback (ml/outfit_scorer/src/data/embed.py)
- Added graph sampler with same-slot negatives and OfferService template subgraphs (ml/outfit_scorer/src/data/sampler.py)
- Added CSA + NGNN-lite dense outfit scorer with ONNX wrapper for the slot-based contract (ml/outfit_scorer/src/model/scorer.py)
- Added training with early stopping, test evaluation and baselines (ml/outfit_scorer/src/train.py, eval.py, baselines.py, evaluation.py)
- Added ONNX export with metadata and torch/onnxruntime parity check (ml/outfit_scorer/src/export_onnx.py, parity_check.py)
- Added tiny_scorer.onnx with random weights for .NET contract tests (ml/outfit_scorer/tests/fixtures)
- Added tests for sampler, model, metrics, ONNX, embedding parity with ParseImageService and end-to-end smoke (ml/outfit_scorer/tests)
- Changed dataset check to verify the actual HF layout: parquet images, splits, templates (ml/outfit_scorer/src/data/check_dataset.py)
- Added requirements and README with reproduction steps (ml/outfit_scorer)
- Added ml/.venv and ml __pycache__ to .gitignore (.gitignore)
- Added Polyvore reader for outfits, compatibility and FITB splits with OutfitSlot mapping (ml/outfit_scorer/src/data/polyvore.py)
- Added pytest setup and Polyvore reader tests (ml/outfit_scorer/tests)
- Added ML pipeline config with Polyvore semantic_category → OutfitSlot mapping (ml/outfit_scorer/config.yaml)
- Added Polyvore dataset inventory script for JSON/Parquet/archive layouts (ml/outfit_scorer/src/data/check_dataset.py)

## 2026-10-09
- Added ml/data/ to .gitignore for Polyvore dataset, embedding cache and checkpoints (.gitignore)
```

(Ці файли — не git-операція; редагувати можна. Якщо `CHANGELOG.md` немає — локально відкрита не `offer_service`, див. §3.)

### 8.2. Довше навчання (обидві моделі)

1. `D:\visualStudio\Projects\OnlineShelf\ml\outfit_scorer\config.yaml`, розділ `train:` → `patience: 5` замінити на
   `patience: 10` (valid AUC коливається ±0.01, 5 епох замало).
2. З `ml\outfit_scorer`, у вікні з `(.venv)` і заданим `$env:POLYVORE_DIR`:
   ```powershell
   python -m src.train --epochs 150
   python -m src.train --epochs 150 --gnn-rounds 0 --name pairwise
   ```
   `--epochs 150` — лише верхня межа; early stopping зупинить раніше. До ~20 хв кожна.
   Наприкінці — блок з `"best_epoch"` і `"best_valid_compat_auc"`; у `ml\data\runs\` дві нові теки.

### 8.3. Оцінка нових моделей (крок 9)

```powershell
$main = Get-ChildItem ..\data\runs -Directory -Filter *_csa_ngnn | Sort-Object Name | Select-Object -Last 1
$pair = Get-ChildItem ..\data\runs -Directory -Filter *_pairwise | Sort-Object Name | Select-Object -Last 1
$main.FullName
$pair.FullName
python -m src.eval --checkpoint "$($main.FullName)\best.pt" --pairwise "$($pair.FullName)\best.pt"
```
Перевірити, що надруковані шляхи — нові прогони (не `20261024_222138` / `20261024_223824`).

### 8.4. Експорт і паритет (кроки 10–11) — лише після вибору моделі (§7)

Для обраного чекпоінта (`$ckpt` = повний шлях до його `best.pt`):
```powershell
$eval = Get-ChildItem ..\data\eval -Directory | Sort-Object Name | Select-Object -Last 1
python -m src.export_onnx --checkpoint $ckpt --metrics "$($eval.FullName)\metrics.json"
python -m src.parity_check --checkpoint $ckpt
```
Очікувано: `saved ...\OutfitNetworkService\Models\outfit_gnn.onnx (~900 KB)` і `OK: tolerance 0.0001`.

### 8.5. Дозбирати дані для звіту/README

```powershell
python -m pytest
python -m src.data.check_dataset
python -m src.data.embed --check-fp16 256
python -m src.data.embed --split valid --limit 500     # швидкість img/s → вписати в README замість заглушки
```
(`--limit 500` пише окремий кеш `disjoint_valid_n500`, основний не зачіпає.)

---

## 9. Що залишилось зробити тобі (після §7–8)

1. **Якщо користувач погодиться** — реалізувати residual-агрегацію (§7 п.2) з тестом
   (модель із нульовою поправкою = pairwise), прогнати `pytest`, дати команди перенавчання.
2. **.NET-інтеграція (5.7)** — факти з коду гілки `offer_service`:
   - proto: `backend/Microservices/OutfitNetworkService/src/OutfitNetworkService.Host/Protos/offer-network.proto`,
     `message ItemNode` має поля 1–10 (`item_id, attribute_type, is_virtual, is_pinned, feature_vector,
     attribute_color_main, attribute_color_second, attribute_season, attribute_pattern, attribute_matterial`).
     Додати `int32 slot = 11;` і `string embedding_model = 12;`. OfferService підключає цей самий файл як клієнт
     (`OutfitOfferService.csproj`: `<Protobuf Include="..\OutfitNetworkService\...\offer-network.proto" GrpcServices="Client" .../>`).
   - OfferService: `OutfitOfferService/src/OutfitOfferService.Host/Services/Grpc/NetworkOutfitScorer.cs`, метод
     `ToNode(WardrobeItem)`. У `WardrobeItem` (`OutfitOfferService.Common/Models/WardrobeItem.cs`) є
     `VisualEmbedding`, `EmbeddingModel`, `AttributeType`, але **немає слота** — слот виводиться з `AttributeType`
     через конфіг `OutfitGeneration:Slots` (знайди, де OfferService резолвить слот, і передай його в `ToNode`).
   - NetworkService: `ItemNode` entity (`src/OutfitNetworkService.Repository.EfCore/Entities/ItemNode.cs`) +
     мапінг у `src/OutfitNetworkService.Host/Grpc/OutfitNetworkGrpcService.cs` — поля `Slot`, `EmbeddingModel`.
   - `src/OutfitNetworkService.Host/Services/OutfitCompatibilityScorer/OnnxGraphCompatibilityScorer.cs`: зараз входи
     `node_features [N,F]` (F = max довжина — **падає, якщо в усіх вузлів немає вектора**), `node_categorical [N,4]`,
     `edge_index`. Новий контракт: `node_features [N,512]`, `node_category int64[N]`, `node_has_embedding float[N]`,
     `edge_index int64[2,E]`; виходи `graph_score [1]`, `edge_scores [E]`. Розмірність і `embedding_model` читати з
     `metadata_props` при старті (`InferenceSession.ModelMetadata.CustomMetadataMap`); вузол без вектора **або** з
     іншим `embedding_model` → нулі + `has_embedding=0`; залогувати `model_version` і `metrics_json`.
     `OutfitGraph.CreateComplete` будує всі впорядковані пари i≠j — модель це підтримує.
   - Реєстрація: `Extentions/OutfitNetworkServiceExtentions.cs` (ONNX, якщо файл існує, інакше stub);
     `appsettings.json` → `"ModelPath": "Models/outfit_gnn.onnx"`.
   - `.csproj` копіювати `Models/**` в output; перевірити `OutfitNetworkService/Dockerfile`; у `docker-compose.yml`
     сервіс `outfitnetworkservice` — додати `volumes: - ./OutfitNetworkService/Models:/app/Models:ro`.
   - Новий тест-проєкт `OutfitNetworkService/tests/OutfitNetworkService.Tests` за зразком
     `OutfitOfferService/tests/OutfitOfferService.Tests` (і додати в `Microservices.sln`, виключити з build сервісу,
     як зроблено в OfferService): форми тензорів, вузли без ембединга, чужий `embedding_model`, симетрія попарних
     скорів, `tiny_scorer.onnx` (скопіювати з `ml/outfit_scorer/tests/fixtures/`) повертає [0,1], `ScoringOrchestrator`
     з фейковим скорером. Нових NuGet-пакетів у сервіси не додавати (`Microsoft.ML.OnnxRuntime` 1.29.0 уже є).
   - `dotnet test` зелений; рядки в CHANGELOG.
3. **MODEL_CARD.md** (`ml/outfit_scorer/`): дані, архітектура, метрики (таблиця §6 + оновлені), обмеження.
4. **5.8 `src/sanity_wardrobe.py`**: топ-N образів через gateway `POST /offers/generate` (токен дає користувач) або gRPC,
   markdown-таблиця (назви, слоти, finalScore, pairwiseScores), std > 0.05, `elapsedMs` ≤ 500 мс, ручна оцінка 20 образів,
   точність топ-5 vs заглушка. У `scripts/seed_test_wardrobe.sql` речі без фото — порахувати, скільки речей з фото бракує по слотах.
5. **Звіт** `Claude outputs/REPORT_03_ml_scorer_mvp.md` (українською, структура — Додаток А §6.5) і підсумок 3–5 рядків у чаті.
6. Паритет ембедингів зі справжньою CLIP (§5) — на 3 фото-кропах користувача.

---

## 10. Корисні нюанси / граблі

- Усі команди `python -m src...` — **лише з `ml\outfit_scorer`** (інакше `No module named src`).
- `POLYVORE_DIR` треба задавати в кожному новому вікні PowerShell; альтернатива — вписати абсолютний шлях у
  `config.yaml` → `paths.polyvore_dir` (абсолютні шляхи підтримуються).
- `eval` потребує `ml\data\embeddings\disjoint_test.npy`; `train` — `disjoint_train.npy` і `disjoint_valid.npy`.
- Щоб перерахувати ембединги — видалити відповідний `.npy` (інакше `already cached, skipping`).
- `.ps1`-скрипти блокуються політикою виконання → `powershell -ExecutionPolicy Bypass -File ...` або
  `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`.
- `export_onnx.py --random --out tests/fixtures/tiny_scorer.onnx` перегенеровує фікстуру (TINY_MODEL_CFG, seed 0).
- Чекпоінт `best.pt` містить `model_state`, `model_cfg`, `emb_dim`, `epoch`, `valid`, `variant`; завантаження з
  `weights_only=False`.
- Polyvore = продуктові фото на білому тлі; у користувачів фото з YOLO-кропу (`fashion_best.pt`, прогін зупинився
  на 3-й епосі з 50, mAP50 ≈ 0.34) — domain gap, писати в обмеженнях.

---

## Додаток А. Повний текст завдання TASK_03 (від користувача)

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
