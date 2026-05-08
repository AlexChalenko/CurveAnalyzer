# Research: Индексный контекст ОФЗ

## Решение: использовать MOEX ISS `stock/index` history как primary source

**Decision**: загружать дневную историю индексов через:

```text
https://iss.moex.com/iss/history/engines/stock/markets/index/securities/{SECID}.json
```

с параметрами `from`, `till`, `iss.meta=off`.

**Rationale**: endpoint отдает историю для `RGBI`, `RGBITR` и duration-сегментов
в той же дневной гранулярности, что и OFZ activity. Проверка на период
`2026-03-24` - `2026-05-08` вернула 32 строки для каждой проверенной серии и
поля `CLOSE`, `VALUE`, `DURATION`, `YIELD`.

**Alternatives considered**:

- Web-страницы MOEX индексов. Отвергнуто: HTML не является стабильным
  машинным контрактом для приложения.
- Считать общий индекс из загруженных выпусков ОФЗ. Отложено: это уже не
  официальный фон MOEX и потребует методологии весов.
- Использовать только existing breadth/activity aggregates. Недостаточно:
  они описывают выбранный набор, но не официальный market index context.

## Решение: whole-market V1 = `RGBI` и `RGBITR`

**Decision**: V1 считает обязательными whole-market series:

- `RGBI`: ценовой индекс государственных облигаций;
- `RGBITR`: индекс полной доходности государственных облигаций.

**Rationale**: оба ряда есть в roadmap, доступны в ISS history/current и
покрывают две разные интерпретации рынка: price-only движение и total-return
контекст.

**Alternatives considered**:

- Только `RGBI`. Отвергнуто: пользователь потеряет контекст полной доходности.
- Только `RGBITR`. Отвергнуто: price-only движение проще сопоставлять с
  изменениями доходностей и цен.

## Решение: duration-сегменты V1 через `RUGBICP*` и `RUGBITR*`

**Decision**: использовать доступные segment series:

- price segment: `RUGBICP1Y`, `RUGBICP3Y`, `RUGBICP5Y`, `RUGBICP7Y+`;
- total-return segment: `RUGBITR1Y`, `RUGBITR3Y`, `RUGBITR5Y`, `RUGBITR7Y+`.

**Rationale**: все восемь серий найдены в ISS index securities list и отдают
history rows с `CLOSE`, `YIELD`, `DURATION` за проверенный период. Такой набор
достаточен для короткого, среднего и длинного контекста без отдельной модели
группировки выпусков.

**Alternatives considered**:

- Использовать только `RUGBICP*`. Отложено: price series хороши для движения
  цены, но не заменяют total-return view.
- Использовать `5+`, `5Y7Y`, `10Y` варианты. Отложено: V1 должен быть
  компактным; дополнительные buckets можно добавить позже, если UI выдержит
  плотность.
- Автоматически загружать все индексы, содержащие `RUGBI`. Отвергнуто для V1:
  растет шум и сложность интерпретации.

## Решение: сохранять index points в SQLite cache

**Decision**: добавить persistent cache для index daily points и читать index
context из локальной базы после загрузки/обновления.

**Rationale**: OFZ activity workflow уже строится вокруг прогрева локальной
истории и повторного открытия экрана без обязательного сетевого запроса.
Индексный слой должен вести себя так же, включая restart scenario и offline
availability для уже загруженного периода.

**Alternatives considered**:

- Загружать индексы каждый раз из сети. Отвергнуто: ломает текущую модель
  cache-first analytics и делает UI зависимым от availability ISS.
- Хранить только latest index values. Недостаточно: нужно сопоставлять
  activity/breadth days с историческим фоном.

## Решение: current endpoint только для provisional snapshot

**Decision**: использовать current endpoint

```text
https://iss.moex.com/iss/engines/stock/markets/index/securities/{SECID}.json
```

как источник текущего provisional context, если выбранный период включает
текущий торговый день.

**Rationale**: current endpoint отдает `marketdata` с `CURRENTVALUE`,
`LASTCHANGE`, `LASTCHANGEPRC`, `TRADEDATE`, `TIME`, `SYSTIME`. Эти значения
полезны как snapshot, но не должны притворяться финальной историей.

**Alternatives considered**:

- Не показывать current-day index context. Отложено: экран уже помечает
  current-day данные как preliminary, и индексный слой должен быть согласован.
- Перезаписывать history row current snapshot. Отвергнуто: смешивает
  provisional и historical states.

## Решение: daily move считать только от предыдущей торговой точки той же серии

**Decision**: `DailyChange`, `DailyChangePercent` и direction считаются только
когда есть предыдущий `OfzMarketIndexPoint` той же `SecId` с валидным `Close`.

**Rationale**: weekends, holidays и неполные серии не должны создавать ложные
нули или календарные gaps. Это повторяет no-look-ahead подход существующего
activity/breadth workflow.

**Alternatives considered**:

- Сравнивать с предыдущим календарным днем. Отвергнуто из-за выходных и
  неторговых дней.
- Forward-fill missing values. Отвергнуто: скрывает отсутствие данных и
  противоречит FR-004/FR-012.

## Решение: meaningful move threshold для V1

**Decision**: whole-market движение считается meaningful, если выполнено хотя бы
одно условие:

- абсолютное дневное изменение `Close` по `RGBI` или `RGBITR` >= `0.25%`;
- абсолютное дневное изменение `Yield` >= `0.10 п.п.`.

Для duration-сегментов применяется тот же price threshold `0.25%`, а yield
threshold используется как дополнительное evidence, если `Yield` доступен.

**Rationale**: пороги отсекают мелкий дневной шум и при этом остаются
достаточно чувствительными для контекстных выводов. Значения должны жить в
options, чтобы их можно было изменить без переписывания модели.

**Alternatives considered**:

- Любое ненулевое изменение. Отвергнуто: будет создавать слишком много
  слабых findings.
- Жесткий высокий порог `1%`. Отложено: для индексов ОФЗ это может скрывать
  значимые дневные движения.
- Только yield threshold. Отвергнуто: price/total-return index context должен
  работать даже когда `Yield` отсутствует.

## Решение: index context является фоном, а не фильтрованным типовым индексом

**Decision**: фильтр типа ОФЗ не пересчитывает official index context. Он
ограничивает activity/breadth/special analytics, а индексный фон остается
общерыночным или segment-series context.

**Rationale**: `RGBI`/`RGBITR` и segment indices являются официальными рядами,
а не агрегатами текущего набора выпусков в UI. Пересчет по фильтру создал бы
другую метрику с другим названием и методологией.

**Alternatives considered**:

- Считать per-type synthetic index из выбранных выпусков. Отложено как
  отдельная feature после появления явной методологии.
