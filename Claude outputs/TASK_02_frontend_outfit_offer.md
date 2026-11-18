# Завдання для Claude Code №2: інтеграція підбору образу (Outfit Offer) у frontend/admin-portal

> Працюй **лише у `frontend/admin-portal`**. Бекенд не змінюй. Якщо для UI бракує даних з API, не обходь це на фронті, а запиши в «Відкриті питання» у звіті.
> Перед роботою прочитай `CLAUDE.md` у корені. Після кожної тематичної зміни додавай рядок у `CHANGELOG.md`.

## 1. Контекст

Бекенд уже вміє генерувати образи (сценарій Б магістерської роботи):
- **OutfitOfferService** (через Gateway `http://localhost:5000`, маршрути `/offers/*`) бере гардероб користувача, погоду і сезон. Він фільтрує речі жорсткими обмеженнями (CSP), будує образи beam search'ем за шаблонами `верх + низ + взуття (+ верхній одяг)` та `сукня/комбінезон + взуття (+ верхній одяг)` і оцінює їх моделлю сумісності з OutfitNetworkService.
- Поки ONNX-модель не натренована, скорер — заглушка, і всі образи мають однаковий скор `0.75`. **UI має коректно показувати однакові скори** і не повинен покладатися на те, що вони різні.

Вимога FR-3 зі звіту з практики: UI Wizard з кроками **Context / Include / Exclude / Reference**. Крок Reference бекенд поки не підтримує: показуй його як вимкнений крок з підписом «скоро».

## 2. Існуючі патерни (дотримуйся їх)

- HTTP: `src/api/httpClient.ts` (axios, baseURL Gateway, Bearer-токен та refresh-інтерцептор уже є). API-модулі лежать у `src/api/<domain>/<domain>Api.ts`, типи поруч (див. `src/api/shelfs/itemsApi.ts`).
- Дані: TanStack Query. Хуки лежать у `src/hooks/<domain>/useXxx.ts` (`useQuery` для читання, `useMutation` для дій; приклади: `src/hooks/shelfs/useShelfs.ts`, `src/hooks/items/useCreateItem.ts`).
- UI: antd 5 + `@ant-design/icons`. Сторінки лежать у `src/pages/<name>/`, роути в `src/App.tsx` під `<ProtectedRoute />`, навігація в `src/components/Header/Header.tsx`.
- Нотифікації: `src/notification/useNotification.tsx`.
- Картинки речей: `useImage(imageName)` з `src/hooks/items/useImage.ts` (повертає blob URL через `/shelfs/image/get-image/{name}`).
- Гардероб: `useShelfs()` повертає полиці з `items: { id, name, smallImage }[]`.
- JWT: `src/hooks/jwtauth/AuthService.ts`. **Id користувача лежить у claim `nameid`** (бекенд кладе `JwtRegisteredClaimNames.NameId`). Хелпера для нього поки немає, додай `authService.getUserId()`.

## 3. API-контракт

### 3.1. `GET /offers/get-weather?latitude={lat}&longitude={lon}`
```json
{ "currentWeather": 4, "temperatureCelsius": 19.7 }
```
`currentWeather` приходить числом (enum `Weather`):

| 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|
| Unknown | Hot | Shiny | Clear | Cloudy | Rainy | Snowy | Thunderstorm |

Кеш на бекенді 30 хв. Координати округлюються до 2 знаків, тож частий виклик безпечний.

### 3.2. `POST /offers/generate`
Запит (усі поля, крім `userId`, необов'язкові):
```json
{
  "userId": "d1437f42-8b54-4a02-b0a5-800426cd7580",
  "latitude": 50.62,
  "longitude": 26.25,
  "includeItemIds": ["<guid>"],
  "excludeItemIds": ["<guid>"],
  "onlyFavorite": false,
  "topN": 5
}
```
- `userId` поки передається в тілі. Бери його з JWT (`nameid`); авторизацію бекенд додасть пізніше, тоді поле зникне.
- `latitude` і `longitude` передаються або обидва, або жоден. Без них погодні обмеження не застосовуються, і в `warnings` прийде відповідне повідомлення.
- `topN` має діапазон 1..20 (бекенд обрізає сам).

Відповідь 200:
```json
{
  "outfits": [
    {
      "items": [
        { "itemId": "<guid>", "name": "white tee", "slot": "Top", "bigImage": "abc_big.webp", "isVirtual": false }
      ],
      "finalScore": 0.75,
      "graphLevelScore": 0.75,
      "penaltyMultiplier": 1,
      "appliedPenalties": { "ColorClashPenaltyRule": 1, "VirtualItemPenaltyRule": 1 },
      "pairwiseScores": [ { "itemIdA": "<guid>", "itemIdB": "<guid>", "score": 0.75 } ]
    }
  ],
  "weather": { "condition": 4, "temperatureCelsius": 19.7 },
  "seasonPhase": 10,
  "warnings": ["2 of 11 items have no visual embedding yet; ..."],
  "elapsedMs": 166
}
```
- `slot` може бути `Top | Bottom | FullBody | Shoes | Outerwear | Accessory`. Підписи українською: Верх, Низ, Сукня/комбінезон, Взуття, Верхній одяг, Аксесуар.
- `seasonPhase` 1..12: 1–3 зима (рання/середня/пізня), 4–6 весна, 7–9 літо, 10–12 осінь.
- `weather` дорівнює `null`, якщо координат немає або погодний сервіс недоступний.
- `outfits` може бути порожнім (наприклад, у гардеробі немає низу). Тоді причина буде в `warnings`.
- `bigImage` може бути `null`; `isVirtual` поки завжди `false` (заділ під пораду «докупити», FR-5).

Помилки:
- **400** у форматі FastEndpoints:
  `{ "statusCode": 400, "message": "...", "errors": { "userId": ["..."], "generalErrors": ["Only one Top item can be included, got 'a' and 'b'."] } }`.
  Типові причини: два включені айтеми в одному слоті, сукня разом із верхом/низом, включений айтем не знайдено, нема `userId`.
- **503** у форматі ProblemDetails: `{ "status": 503, "detail": "Downstream service is unavailable: ..." }`. Означає, що ShelfsService або OutfitNetworkService недоступні.

## 4. Що зробити

1. **API та типи:** `src/api/offers/offersApi.ts` (`getWeather`, `generateOutfits`) і TS-типи з розділу 3, включно з мапами enum → українські підписи (`Weather`, `slot`, `seasonPhase`).
2. **Auth-хелпер:** `authService.getUserId(): string | null` (з claim `nameid`), без зміни наявної поведінки.
3. **Хуки:** `src/hooks/offers/useWeather.ts` (`useQuery`, `enabled` лише за наявності координат, `staleTime` 30 хв) і `src/hooks/offers/useGenerateOutfits.ts` (`useMutation`; помилки 400/503 показуються через `useNotification` з текстами з `errors.generalErrors`, полів `errors.*` або `detail`).
4. **Сторінка `/outfits` «Підбір образу»** (antd `Steps`), роут під `ProtectedRoute`, пункт у `Header`:
   - **Context:**
     - локація через `navigator.geolocation` з ручним введенням lat/lon як fallback і можливістю продовжити без локації;
     - картка погоди (іконка/підпис стану, температура), поточний сезон;
     - перемикач «лише улюблені» і кількість варіантів (`topN`, 1..20).
   - **Include:** вибір речей з гардероба (`useShelfs`, мініатюри через `useImage(smallImage)`), речі, які обов'язково мають бути в образі.
   - **Exclude:** вибір речей, яких не має бути. Одна річ не може бути одночасно в Include та Exclude: блокуй це в UI.
   - **Reference:** вимкнений крок «скоро».
   - **Результати:**
     - картки образів, впорядковані як прийшли з бекенду;
     - у кожній картці речі з фото (`useImage(bigImage)`), підпис слота, назва;
     - скор у відсотках, множник штрафів;
     - розгортуваний блок «Чому так» з `appliedPenalties` і `pairwiseScores` (імена речей замість GUID);
     - `warnings` як antd `Alert` над результатами;
     - `elapsedMs` дрібним текстом;
     - кнопка «Згенерувати ще раз» з тими самими параметрами;
     - порожній стан, якщо `outfits` порожній.
5. **Стан форми** між кроками не губиться (одна сторінка, локальний state або `Form`). Кнопка генерації неактивна під час запиту (`isPending`).

## 5. Обмеження

- Не додавай нових бібліотек без потреби (є antd, TanStack Query, axios, dnd-kit).
- Не змінюй `httpClient.ts`, окрім випадку, коли без цього неможливо (тоді поясни у звіті).
- Не виправляй сторонні баги фронту в цьому завданні, лише запиши їх у звіт.
- `!localHttpClient.ts` не використовуй.

## 6. Критерії приймання

- `npm run build` і `npm run lint` проходять без нових помилок (наявні до тебе помилки, якщо є, перелічи окремо).
- З піднятим docker-compose сценарій «Context → Include → Generate» показує образи. Для 400 (два верхи в Include) з'являється зрозуміле повідомлення. Без координат з'являється warning, а не помилка.
- Однакові скори (заглушка) відображаються коректно.

## 7. Результат

Створи `Claude outputs/REPORT_02_frontend_outfit_offer.md` українською:
1. Що зроблено (файли).
2. Як перевірити вручну (кроки).
3. Скріншоти не потрібні, достатньо опису.
4. Відкриті питання (зокрема, чого бракує в API для кращого UI).

Після звіту коротко (3–5 рядків) напиши в чаті підсумок.
