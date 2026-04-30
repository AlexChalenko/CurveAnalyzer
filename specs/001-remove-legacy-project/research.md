# Research: удаление legacy-проекта и стабилизация supported app

## External references

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core):
  .NET 10 is active LTS, latest patch `10.0.7`, support end `2028-11-14`.
- [.NET 10 downloads](https://dotnet.microsoft.com/en-US/download/dotnet/10.0):
  SDK `10.0.203` includes Desktop Runtime `10.0.7`.
- [Target frameworks in SDK-style projects](https://learn.microsoft.com/en-us/dotnet/standard/frameworks):
  WPF/WinForms projects use Windows-specific TFM such as `net10.0-windows`.
- [NuGet Central Package Management](https://learn.microsoft.com/en-gb/nuget/consume-packages/central-package-management):
  `Directory.Packages.props` with `ManagePackageVersionsCentrally=true` is the
  standard mechanism for central package versions.

## Decision: root `CurveAnalyzer.sln` становится единственной supported solution

**Rationale**: root solution является естественной точкой входа для Visual
Studio и CLI. Сейчас она включает старый `CurveAnalyzer.csproj`, внешний
`..\MoexData\MoexData.csproj` и новые `src` projects, поэтому full build может
падать из-за legacy. Перевод root solution на supported `src` projects снимает
неоднозначность.

**Alternatives considered**:

- Оставить nested solution в `src/CurveAnalyzer.Presentation.WPF`. Отвергнуто:
  новый contributor скорее откроет root solution, а две solution создают
  неоднозначность.
- Оставить обе solution и документировать supported одну. Отвергнуто:
  противоречит принципу "один supported путь".

## Decision: supported projects переводятся на .NET 10 LTS

**Rationale**: .NET 10 является активным LTS-релизом с поддержкой до
2028-11-14 по официальной support policy Microsoft. На рабочей машине уже
установлены .NET SDK `10.0.203` и Desktop Runtime `10.0.7`, поэтому миграция не
требует отдельного bootstrap. Для WPF Microsoft документирует Windows-specific
TFM вида `net10.0-windows`; library projects должны быть выровнены на `net10.0`.

**Alternatives considered**:

- Оставить libraries на `net8.0`, WPF на `net9.0-windows7.0`. Отвергнуто:
  миграция legacy оставит смешанную техническую базу и быстрый package drift.
- Перевести только WPF app. Отвергнуто: supported solution все равно будет
  содержать разные target framework generations без необходимости.
- Перейти на preview `net11.0`. Отвергнуто: feature про стабилизацию, нужен LTS,
  а не preview stack.

## Decision: использовать NuGet Central Package Management

**Rationale**: в supported projects версии пакетов сейчас заданы в `.csproj`,
часть package families повторяется, а EF Core/Microsoft.Extensions уже разъехались
между `9.0.3` и `9.0.9`. CPM через `Directory.Packages.props` делает package
policy явной в одном месте и снижает drift при обновлениях.

**Alternatives considered**:

- Оставить `Version` в каждом `PackageReference`. Отвергнуто: проще точечно, но
  хуже для multi-project migration и будущих обновлений.
- Централизовать только EF Core. Отвергнуто: смешанная модель управления
  версиями будет менее очевидной.

## Decision: обновить все обновляемые NuGet dependencies до latest stable

**Rationale**: `dotnet list package --outdated` по supported projects показывает
доступные обновления: `CommunityToolkit.Mvvm` и `CommunityToolkit.Diagnostics`
с `8.4.0` до `8.4.2`, `Microsoft.EntityFrameworkCore.*` до `10.0.7`,
`Microsoft.Extensions.Hosting` до `10.0.7`. Это совпадает с .NET 10 target и
убирает текущую рассинхронизацию EF Core versions.

**Alternatives considered**:

- Обновить только EF Core/Microsoft.Extensions. Отвергнуто: Toolkit packages
  тоже имеют patch updates и должны попасть в единый upgrade pass.
- Обновлять packages без TFM upgrade. Отвергнуто: EF Core 10 рассчитан на
  .NET 10 stack, поэтому package update и TFM alignment должны идти вместе.
- Делать floating versions. Отвергнуто: сборка должна быть повторяемой.

## Decision: использовать легкий DDD, а не full DDD

**Rationale**: домен небольшой: ZCYC yield curve data, periods, historical
series, spreads и sync. Здесь полезны precise names, value objects и pure
calculation services. Aggregates, domain events и factories сейчас не защищают
сложные инварианты и добавят ceremony.

**Alternatives considered**:

- Full DDD с aggregates/domain events. Отвергнуто: нет нескольких bounded
  contexts, транзакционных инвариантов или сложного lifecycle.
- Anemic DTO-only model. Отвергнуто: расчеты spread/rate/history снова окажутся
  во ViewModels и controls.

## Decision: Application layer не зависит от MVVM messaging

**Rationale**: `DataSyncService` сейчас зависит от `IMessenger`, что тянет
UI/MVVM механизм в Application. Progress и completion можно выразить через
`IProgress<SyncProgress>`, return result или application event abstraction.
Presentation может превратить это в `WeakReferenceMessenger`, если UI все еще
нужен messenger.

**Alternatives considered**:

- Оставить `IMessenger` в Application ради простоты. Отвергнуто: нарушает
  dependency direction и усложняет unit testing.
- Полностью убрать progress. Отвергнуто: UI уже имеет progress bar, пользователю
  нужна обратная связь при sync.

## Decision: EF Core остается Infrastructure detail

**Rationale**: Application должна работать через interfaces и domain/application
types. `DbContext`, migrations, SQLite connection string и query tracking
находятся в Infrastructure registrations.

**Alternatives considered**:

- Прямо внедрять `MoexContext` в Application. Отвергнуто: связывает use cases с
  persistence implementation.
- Убрать repository boundary и делать все в DataSyncService. Отвергнуто:
  ухудшит тестируемость и нарушит слойность.

## Decision: заменить `EnsureCreated()` на явную инициализацию/migrations

**Rationale**: `Database.EnsureCreated()` в constructor имеет скрытый side
effect при создании context. Для supported app лучше явный startup initializer
или migrations path. В рамках tasks можно выбрать минимальный путь: убрать
constructor side effect, добавить `Database.Migrate()` в infrastructure startup
initializer и задокументировать runtime database location.

**Alternatives considered**:

- Оставить `EnsureCreated()`. Отвергнуто: плохо контролируется и не масштабируется
  для schema evolution.
- Полностью отказаться от SQLite. Отвергнуто: не требуется для удаления legacy.

## Decision: сохранить `LiveCharts.Wpf` временно, удалить local `HintPath`

**Rationale**: поддерживаемые views сейчас используют `LiveCharts.Wpf`.
Локальная ссылка на `..\..\..\LiveCharts2\...dll` нарушает repeatable build и не
используется как primary source в XAML/controls. `LiveCharts.Wpf` `0.9.7` уже
дает NU1701 compatibility warnings и не обновляется обычным package upgrade.
Полная chart migration в LiveChartsCore или другую библиотеку должна быть
отдельной feature после стабилизации, если пакет не блокирует .NET 10 build.
Если `LiveCharts.Wpf` блокирует validation на .NET 10, chart migration перестает
быть optional и должна войти в реализацию этой feature.

**Alternatives considered**:

- Немедленно мигрировать все графики на новый chart stack. Отвергнуто: большой
  риск UI regressions и scope creep, если текущий stack все еще собирается.
- Оставить local `HintPath`. Отвергнуто: нарушает repeatable build.

## Decision: добавить focused test project

**Rationale**: тестов сейчас нет. Для миграции нужен минимум автоматической
защиты вокруг доменных расчетов: spread alignment by trading date, period
validation, rate-series grouping if moved. UI smoke можно оставить manual.

**Alternatives considered**:

- Только manual validation. Отвергнуто для расчетов: слишком легко сломать
  spread/rate behavior при переносе.
- Большой integration test suite сразу. Отвергнуто: увеличит scope миграции.
