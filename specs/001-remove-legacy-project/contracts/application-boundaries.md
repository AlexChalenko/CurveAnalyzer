# Contract: application boundaries

Этот контракт описывает целевые границы между слоями. Это не public API для
внешних клиентов, а проверяемый внутренний контракт архитектуры.

## Presentation -> Application

Presentation может:

- вызывать application use cases;
- передавать `CancellationToken`;
- передавать `IProgress<SyncProgress>` или подписываться на UI-neutral result;
- преобразовывать application results в chart series.

Presentation не должна:

- создавать `MoexContext`;
- вызывать MOEX XML client напрямую;
- читать/писать SQLite напрямую;
- требовать от Application `IMessenger` или WPF dispatcher.

## Application -> Core

Application может:

- использовать domain records/value objects;
- вызывать pure calculation services;
- объявлять interfaces для storage и online data.

Application не должна:

- зависеть от WPF;
- зависеть от EF Core;
- зависеть от chart libraries;
- зависеть от XML serialization details.

## Infrastructure/ApiServices -> Application

Infrastructure и ApiServices реализуют application interfaces:

- historical repository/storage;
- MOEX online data access;
- startup database initialization.

Они не должны управлять UI state или chart rendering.

## Composition Root

WPF `App.xaml.cs` остается outermost composition root, но registrations должны
быть сгруппированы:

- `services.AddApplication()`
- `services.AddInfrastructure(configuration)`
- `services.AddMoexApi(configuration)`
- `services.AddPresentation()`

## Progress Contract

Sync progress должен быть UI-neutral:

```text
SyncProgress
├── CompletedCount
├── TotalCount
├── CurrentDate
└── Ratio
```

Application сообщает progress, Presentation решает, как показать progress bar,
messages или completion state.
