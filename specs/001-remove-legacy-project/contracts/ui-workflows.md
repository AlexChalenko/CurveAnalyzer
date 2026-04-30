# Contract: supported UI workflows

Этот контракт фиксирует workflows, которые должны сохраниться после удаления
legacy-проекта.

## Workflow 1: Yield Curve by Date

**Actor**: пользователь WPF-приложения.
**Input**: выбранная trading date.
**Expected result**: chart показывает yield curve для выбранной даты.

Acceptance:

- Date picker доступен после sync/initialization.
- Недоступные даты не ломают UI.
- Если historical data отсутствует, app может запросить online data.
- Empty result отображается явно или выбирает ближайшую доступную дату в рамках
  текущего поведения.

## Workflow 2: Rate History by Period

**Actor**: пользователь WPF-приложения.
**Input**: выбранный period.
**Expected result**: chart показывает time series/rate history для period.

Acceptance:

- Period list заполняется из available periods.
- Выбор period не блокирует UI.
- Series упорядочена по trading date.

## Workflow 3: Spread Between Periods

**Actor**: пользователь WPF-приложения.
**Input**: два разных period.
**Expected result**: chart показывает spread series.

Acceptance:

- Нельзя считать spread для двух одинаковых periods.
- Spread считается по датам, присутствующим в обеих series.
- Formula: `second.Value - first.Value`.
- Изменение любого period обновляет result без fire-and-forget ошибок.

## Workflow 4: Startup Data Sync

**Actor**: пользователь WPF-приложения.
**Input**: запуск приложения.
**Expected result**: app синхронизирует недостающую history и сообщает progress.

Acceptance:

- Progress отображается в UI без application dependency on `IMessenger`.
- Отсутствие новых дат завершается корректно.
- Ошибка network/storage не должна приводить к silent failure.
