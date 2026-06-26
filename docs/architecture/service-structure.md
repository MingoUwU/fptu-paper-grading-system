# Service structure convention

Every backend microservice project follows the same three-layer folder shape:

```text
Fptu.Pgs.<Service>.Api/
  Application/
  Domain/
  Infrastructure/
  Program.cs
```

- `Domain`: entities, value objects, enums, domain rules, domain exceptions.
- `Application`: use cases, command/query handlers, mappers, service interfaces.
- `Infrastructure`: EF Core DbContext, migrations, external adapters, storage, broker integration.

Some services can have extra folders when needed:

- `Providers`: external provider implementations, for example Gemini in `AiGrading`.
- `Endpoints`: optional HTTP endpoint grouping when `Program.cs` becomes too large.

Marker files are intentionally kept in newly scaffolded folders so Visual Studio shows the
folder structure before real business code is added.
