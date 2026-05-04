# UI Contract: экран активности ОФЗ

## Navigation

- Main window содержит новый пункт навигации: `Активность ОФЗ`.
- Выбор пункта открывает `OfzActivity` screen в основной области приложения.
- Существующие пункты `Yield Curve`, `Rate Change` и `Spread Change` остаются
  доступными.

## Initial State

- При первом открытии экран предлагает диапазон дат по умолчанию: последние
  доступные 30 торговых дней или ближайший доступный диапазон, если данных
  меньше.
- До загрузки данных top anomalies, heatmap и detail panel находятся в пустом
  управляемом состоянии.
- При первом открытии экран может запустить предварительную загрузку недавней
  истории ОФЗ по дням и показать progress/status; пользовательские controls
  остаются доступными.
- Если локальной истории нет, пользователь видит состояние загрузки, а не
  устаревшие данные.

## Date Range Workflow

1. Пользователь выбирает `StartDate` и `EndDate`.
2. Экран запускает проверку локальной истории, использует заранее скачанные
   дневные данные и догружает только недостающие даты.
3. Во время загрузки отображается progress/status.
4. После загрузки экран показывает:
   - top anomalies table;
   - activity heatmap;
   - пустой detail panel до выбора выпуска.
5. Если secondary context view уже реализован, экран также может показывать
   activity index summary; отсутствие индекса не блокирует основной сценарий.

## Top Anomalies Table

### Columns

- Rank
- TradeDate
- SecId
- ShortName
- Currency/Type marker
- Value
- NumTrades
- ActivityScore
- YieldMove
- Duration

### Behavior

- Сортировка по умолчанию: `ActivityScore` descending.
- Клик по строке выбирает выпуск и дату, обновляя detail panel.
- Записи с недостаточной baseline базой не входят в основной top list, но
  могут отображаться в отдельном информационном состоянии.

## Activity Heatmap

### Axes

- Rows: выпуски ОФЗ.
- Columns: торговые даты.
- Cell color: bucket `ActivityScore`.

### Behavior

- Клик по ячейке выбирает выпуск и дату.
- Ячейка без данных отличается от нулевой активности.
- Ячейка с недостаточной baseline базой отличается от уверенного score.
- При смене диапазона старые ячейки очищаются до отображения новых данных.

## Issue Detail Panel

### Content

- Header: `SecId`, `ShortName`, currency/type marker, maturity date when known.
- Time-series:
  - turnover `Value`;
  - `NumTrades`;
  - yield series using preferred yield;
  - price series using preferred price.
- Selected anomaly facts: score, baseline median, yield move, duration.

### Behavior

- Если выбран выпуск без доходности, yield series показывает empty state, но
  volume/turnover остаются доступными.
- Выбор другого выпуска полностью заменяет previous detail data.

## Duration/Yield Scatter

### Content

- Secondary view размещается на вкладке `Сегменты` рядом с детальной вкладкой
  выбранного выпуска.
- Activity index: компактный график агрегированного дневного индекса активности.
- X axis: duration in years.
- Y axis: yield.
- Point size or color: turnover/activity.
- Point label/tooltip: trade date, `SecId`, `ShortName`, `Value`,
  `ActivityScore`.

### Behavior

- Scatter является secondary view и может быть скрыт, если данных по duration
  или yield недостаточно.
- Для диапазона scatter оставляет одну лучшую точку на выпуск, чтобы широкий
  период не был забит повторами одного `SecId`.
- Фильтр типа ОФЗ применяется к activity index и scatter так же, как к таблице
  и heatmap.

## Empty and Error States

- No data for range: показать сообщение, что за период нет пригодных данных, и
  оставить controls доступными.
- Partial load failure: показать предупреждение с количеством не загруженных
  дат и продолжить отображать успешные даты.
- Insufficient baseline: объяснить, что выпуску не хватает предыдущей истории
  для уверенного relative score.
- Network unavailable: показать retry action и сохранить уже загруженные
  локальные данные.

## Performance Expectations

- Диапазон до 90 торговых дней должен открывать рейтинг и heatmap не дольше
  10 секунд на типовом локальном наборе.
- UI controls должны оставаться responsive во время загрузки.
