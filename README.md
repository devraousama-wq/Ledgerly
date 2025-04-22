# Ledgerly

Self-hosted double-entry accounting and invoicing platform built with .NET 8.

Ledgerly gives small businesses and freelancers a correct, auditable accounting system. Every financial movement is recorded as balanced journal entries against a configurable chart of accounts, with invoicing, bills, tax handling, and standard financial reports layered on top.

## Architecture

```
src/
├── Ledgerly.Domain/          Core entities, value objects, domain rules
├── Ledgerly.Application/     CQRS handlers, DTOs, service interfaces
├── Ledgerly.Infrastructure/  EF Core, auth, jobs, external integrations
├── Ledgerly.Web/             Blazor Server admin UI
├── Ledgerly.Api/             REST API + OpenAPI
└── Ledgerly.Jobs/            Background workers (recurring, webhooks)
```

Clean architecture with strict double-entry invariants. Money is always `decimal` with ISO 4217 currency codes — never floating point.

## Stack

- C# 12 / .NET 8
- ASP.NET Core + Blazor Server
- Entity Framework Core 8 + PostgreSQL
- MediatR, FluentValidation
- QuestPDF, ClosedXML
- xUnit, FluentAssertions, Testcontainers

## Quick start

```bash
docker compose up -d postgres
cp .env.example .env
dotnet restore
dotnet run --project src/Ledgerly.Api
dotnet run --project src/Ledgerly.Web
```

API listens on `http://localhost:5080`, Blazor UI on `http://localhost:5081`.

## Double-entry primer

Every transaction produces journal lines where total debits equal total credits in the entry's base currency. Posted entries are immutable — corrections use reversal entries, never destructive edits.

## Features

- Chart of accounts with hierarchy and multi-currency
- Double-entry journals with draft/posted states
- Customer and vendor management
- Invoice and bill workflows with ledger posting
- Tax codes and liability reports
- Bank reconciliation (CSV/OFX)
- Recurring transactions
- Trial balance, P&L, balance sheet, cash flow
- Fiscal period close and locked books
- RBAC with audit log
- REST API, webhooks, JWT auth

## License

MIT
