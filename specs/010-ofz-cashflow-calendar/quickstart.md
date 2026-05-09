# Quickstart: Календарь денежных потоков ОФЗ

## Цель проверки

Проверить, что календарь:

- загружает coupons/amortizations/offers из MOEX ISS;
- показывает события по активным выпускам;
- связывает issue-level activity с событиями в окне `±3` дня;
- не превращает missing amounts/percent в нули;
- попадает в structured JSON;
- не ломает существующие режимы активности ОФЗ.

## Команды

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

Targeted:

```powershell
dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release
dotnet test tests\CurveAnalyzer.Infrastructure.Tests\CurveAnalyzer.Infrastructure.Tests.csproj -c Release
```

## Проверка MOEX ISS

```powershell
Invoke-RestMethod "https://iss.moex.com/iss/statistics/engines/stock/markets/bonds/bondization/SU26238RMFS4.json?iss.meta=off"
```

Ожидаемо:

- block `coupons`;
- block `amortizations`;
- block `offers`;
- date fields: `coupondate`, `amortdate`, `offerdate`.

## Ручной сценарий

1. Запустить WPF-приложение.
2. Открыть `Активность ОФЗ`.
3. Выбрать период `24.03.2026` - `08.05.2026`.
4. Нажать `Загрузить`.
5. В `Обзор` проверить блок ближайших cashflow events.
6. Выбрать выпуск из top contributors.
7. Проверить календарь выпуска: купоны, амортизации/погашения, оферты.
8. Проверить finding, если activity day находится рядом с event date.
9. Сохранить JSON и проверить `cashflowContext`.

## Условия приемки v1

- Для активных выпусков сохраняются и читаются `OfzCashflowEvent`.
- `Coupon`, `Amortization`, `Maturity`, `Offer` строятся из `bondization`.
- `NEXTCOUPON`, `MATDATE`, `OFFERDATE`, `BUYBACKDATE`, `CALLOPTIONDATE`,
  `PUTOPTIONDATE` из current securities используются как snapshot fallback.
- Current/fallback events помечаются source/provisional status и limitations.
- UI показывает `n/a` для missing amount/percent/record date.
- Build и tests проходят.
