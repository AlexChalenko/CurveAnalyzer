# Research: анализ активности ОФЗ

## Decision: использовать ISS bonds history `TQOB` как primary source

**Decision**: Основной источник дневных данных по ОФЗ -
`https://iss.moex.com/iss/history/engines/stock/markets/bonds/boards/TQOB/securities.json`.
Для каждой даты загружаются записи блока `history`, включая `SECID`,
`SHORTNAME`, `TRADEDATE`, `NUMTRADES`, `VALUE`, `VOLUME`, `OPEN`, `LOW`,
`HIGH`, `CLOSE`, `WAPRICE`, `YIELDCLOSE`, `YIELDATWAP`, `DURATION`,
`ZSPREAD`, `ZSPREADATWAPRICE`, `MATDATE`, `FACEVALUE`, `FACEUNIT`,
`CURRENCYID`, `BONDTYPE`, `BONDSUBTYPE`.

**Rationale**: Этот endpoint уже агрегирует дневную торговую историю по
бумагам board `TQOB`, содержит нужные для MVP поля объема, доходности, цены и
дюрации, а также отдает `history.cursor` для контролируемой pagination.

**Alternatives considered**:

- `zcyc.xml?iss.only=securities`: содержит bid/ask/trade yields и duration,
  но это snapshot по кривой, а не дневная история оборотов.
- `candles` по отдельной бумаге: подходит для detail chart, но требует
  отдельный запрос на каждый `SECID` и не дает обзор всего рынка.
- `trades` и `orderbook`: слишком детально для MVP, создает большой объем
  данных и не нужно для дневных всплесков.

## Decision: current-day snapshot брать из bonds `securities,marketdata`

**Decision**: Для текущего дня и быстрой проверки живого рынка использовать
`https://iss.moex.com/iss/engines/stock/markets/bonds/boards/TQOB/securities.json?iss.only=securities,marketdata`.
Из `securities` брать справочные поля выпуска, из `marketdata` - `BID`,
`OFFER`, `SPREAD`, `LAST`, `YIELD`, `WAPRICE`, `YIELDATWAPRICE`,
`NUMTRADES`, `VOLTODAY`, `VALTODAY`, `DURATION`, `ZSPREAD`,
`ZSPREADATWAPRICE`.

**Rationale**: History endpoint обычно является надежным источником завершенных
дней, а marketdata endpoint нужен для today/intraday snapshot без хранения
каждой сделки.

**Alternatives considered**:

- Всегда использовать только history: проще, но текущий торговый день может
  быть недоступен или неполным.
- Всегда использовать только marketdata: нет исторической базы для
  относительного score.

## Decision: агрегированный индекс активности строить из history, aggregates использовать как context

**Decision**: Индекс активности ОФЗ в MVP считать по сохраненным
`OfzDailyTrade` за день: суммарный `VALUE`, суммарный `NUMTRADES`, медианный
activity score. Endpoint
`https://iss.moex.com/iss/statistics/engines/stock/markets/bonds/aggregates.json`
использовать как внешний market context, если нужен обзор "ОФЗ против
корпоративных/муниципальных".

**Rationale**: Собственный индекс по тем же данным, что и anomalies, остается
согласованным с фильтрами пользователя и не зависит от классификаций aggregate
endpoint.

**Alternatives considered**:

- Использовать только `aggregates`: быстро, но нельзя провалиться до выпуска и
  нельзя объяснить всплеск.
- Не показывать общий индекс: теряется контекст, был ли всплеск локальным или
  рыночным.

## Decision: фильтрация ОФЗ через board `TQOB` плюс признаки выпуска

**Decision**: Базовый набор ограничивается board `TQOB`; внутри него выпуск
считается ОФЗ, если `SHORTNAME` или `SECNAME` указывает на ОФЗ. Валютные и
нестандартные выпуски не отбрасываются автоматически, но маркируются через
`FACEUNIT`, `CURRENCYID`, `BONDTYPE` и `BONDSUBTYPE`.

**Rationale**: `TQOB` является режимом гособлигаций, но в ответах могут
встречаться валютные ОФЗ и разные подтипы. Пользователю важно видеть такие
выпуски отдельно, а не терять их без объяснения.

**Alternatives considered**:

- Фильтровать только по `SHORTNAME starts with "ОФЗ"`: просто, но хрупко к
  форматам названий.
- Включать все облигации рынка bonds: выходит за scope первой feature и
  смешивает федеральные, корпоративные и муниципальные риски.

## Decision: activity score считать относительно собственной истории выпуска

**Decision**: Основная метрика всплеска:

```text
ActivityScore = VALUE текущей записи / median(VALUE предыдущих 20 доступных торговых записей выпуска)
```

Минимальная база для уверенного score - 10 предыдущих торговых записей с
положительным `VALUE`. Если база меньше, запись помечается как
`InsufficientBaseline`.

**Rationale**: Абсолютный оборот завышает ликвидные benchmark-выпуски. Сравнение
с собственной медианой показывает именно необычность активности.

**Alternatives considered**:

- Среднее вместо медианы: сильнее искажается единичными большими днями.
- Z-score по среднему и стандартному отклонению: полезно позже, но хуже
  объясняется пользователю и менее устойчиво на короткой истории.
- Ранжирование только по `VALUE`: не отделяет "обычно ликвидно" от "аномально".

## Decision: yield move считать по `YIELDATWAP` с fallback на `YIELDCLOSE`

**Decision**: Для изменения доходности использовать:

```text
YieldMove = currentYield - previousAvailableYield
```

где `currentYield` и `previousAvailableYield` берутся из `YIELDATWAP`; если оно
отсутствует, используется `YIELDCLOSE`.

**Rationale**: Средневзвешенная доходность лучше соответствует торговому
обороту дня, а closing yield сохраняет частичную информативность, если
weighted поле пустое.

**Alternatives considered**:

- Использовать только `YIELDCLOSE`: проще, но хуже связывает всплеск с реально
  проторгованным объемом.
- Не показывать yield move: пользователь увидит всплеск, но не поймет, был ли
  он связан с переоценкой.

## Decision: синхронизация гибридная - предварительное окно плюс on-demand диапазон

**Decision**: Для MVP данные можно заранее скачивать по дням в локальную базу.
Базовый warm-up при первом открытии экрана или по явной команде загружает
недавнее окно истории: последние 120 торговых дней, что покрывает типовой
диапазон 90 дней и 20 предыдущих записей baseline lookback с запасом. Для
старых или нестандартных диапазонов остается on-demand догрузка выбранного
диапазона плюс минимум 20 предыдущих доступных торговых записей по каждому
выпуску перед `StartDate`. Сервис проверяет, какие торговые даты уже сохранены,
догружает недостающие даты с ISS, сохраняет записи и статус загрузки по дате.
Повторное открытие диапазона работает из локальной истории.

**Rationale**: Полная загрузка истории с 2012 года избыточна для первого
релиза и ухудшит старт приложения. Но заранее прогретое недавнее окно делает
основной экран быстрее и решает baseline для стандартного сценария без сетевой
догрузки при каждом выборе. Пользовательские сценарии ограничены диапазоном до
90 торговых дней, но без lookback до `StartDate` первые дни диапазона не
смогут получить корректную медианную базу.

**Alternatives considered**:

- Загружать всю историю при старте: просто для UI, но долго и не нужно для MVP.
- Работать только online без сохранения: медленно, нестабильно и невозможно
  быстро пересчитывать baseline.
- Только lazy range sync без warm-up: проще, но первый стандартный просмотр
  будет зависеть от сетевой догрузки и может не уложиться в ожидаемую
  отзывчивость.

## Decision: `zcyc.db` является disposable cache

**Decision**: Добавлять таблицы активности ОФЗ через EF migrations в тот же
SQLite-файл, но считать файл локальным cache storage. Если приложение находит
старую `EnsureCreated`-базу без `__EFMigrationsHistory`, оно удаляет
`zcyc.db`/`zcyc.db-wal`/`zcyc.db-shm` и создает новую migration-based схему.

**Rationale**: В текущем приложении база не содержит уникальные пользовательские
данные: ZCYC и дневную историю ОФЗ можно повторно скачать из MOEX ISS. Простое
пересоздание legacy cache надежнее, чем попытка прошить старую
`EnsureCreated`-схему baseline migration, и не усложняет дальнейшие migrations.

**Alternatives considered**:

- Создать отдельный `ofz-activity.db`: снижает риск schema conflict, но плодит
  storage boundaries и усложняет backup/configuration.
- Оставить `EnsureCreated`: новая таблица не появится в существующей базе.
- Сохранять старые ZCYC rows через baseline seeding в `__EFMigrationsHistory`:
  возможно, но избыточно для disposable cache и рискованнее в сопровождении.

## Decision: heatmap реализовать как WPF matrix, графики - через LiveCharts2

**Decision**: Heatmap строить как виртуализируемую WPF matrix/table с converter
цвета по `ActivityScoreBucket`. Time-series деталей выпуска и scatter по
дюрации/доходности строить через текущий supported LiveCharts2 stack.

**Rationale**: Heatmap требует табличной навигации по выпускам и датам, где WPF
matrix проще контролировать по пустым/частичным данным. LiveCharts2 остается
подходящим для line/bar/scatter visualizations.

**Alternatives considered**:

- Делать heatmap только chart series: риск API/UX нюансов и сложнее выделять
  строки/ячейки как интерактивные элементы.
- Добавлять новую visualization library: неоправданно для MVP.
