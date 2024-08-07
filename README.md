# Mediator.GettingStarted

A simple project to get started with MediatR.

It includes examples for the most relevant features.

- Request/Response
- Notifications
- Pipeline Behaviors (generic and non-generic)
  - Validation behavior using FluentValidation
- Exception Handling
- Pre-persistence events using EF Core and MediatR

```
dotnet tool list --global
dotnet tool install dotnet-ef --global
dotnet ef database update --project .\Mediator.GettingStarted\Mediator.GettingStarted.csproj
EXEC dbo.sp_changedbowner @loginame = N'sa'
```