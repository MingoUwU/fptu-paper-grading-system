# FPTU Paper Grading System

Monorepo microservices skeleton cho Assignment PRN232, xây dựng bằng .NET 10 theo tài liệu kiến trúc ngày 18/06/2026.

> Đây là nền móng có thể build và chạy, chưa phải implementation nghiệp vụ hoàn chỉnh. Các endpoint hiện trả dữ liệu mẫu để nhóm phát triển database, RabbitMQ, Hangfire, OCR và AI theo từng vertical slice.

## Thành phần

```text
src/
├── ApiGateway/                 # YARP - cổng vào duy nhất
├── BuildingBlocks/
│   ├── Fptu.Pgs.BuildingBlocks
│   └── Fptu.Pgs.Contracts     # DTO, event, submission status dùng chung
├── Clients/
│   ├── AdminPanel             # placeholder, chưa chốt frontend stack
│   └── TeacherDesktop         # WPF .NET 10
└── Services/
    ├── Identity
    ├── Exam
    ├── Submission
    ├── DocumentProcessing
    ├── AiGrading
    ├── ReviewScore
    ├── ReportAudit
    └── JobStatus
deploy/
├── docker-compose.yml
└── Dockerfile.service
tests/
└── Fptu.Pgs.Architecture.Tests
```

## Yêu cầu

- .NET SDK `10.0.301` hoặc feature band tương thích.
- Docker Desktop nếu chạy toàn bộ stack.
- Visual Studio 2026/JetBrains Rider/VS Code tùy workflow của nhóm.

## Build và test

```powershell
dotnet restore Fptu.Pgs.sln
dotnet build Fptu.Pgs.sln --configuration Release
dotnet test Fptu.Pgs.sln --configuration Release
```

## Chạy bằng Docker Compose

```powershell
Copy-Item deploy/.env.example deploy/.env
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up --build
```

Các địa chỉ chính:

- API Gateway: `http://localhost:5000`
- RabbitMQ Management: `http://localhost:15672` (credential trong `deploy/.env`)
- MinIO Console: `http://localhost:9001`
- SQL Server: `localhost,1433`

Không commit file `deploy/.env`. Hãy đổi toàn bộ password mẫu trước khi đưa lên host demo.

## Chạy trực tiếp trong IDE

Solution đã cố định port để YARP route đúng:

| Project | URL |
| --- | --- |
| API Gateway | `http://localhost:5000` |
| Identity | `http://localhost:5101` |
| Exam & Rubric | `http://localhost:5102` |
| Submission | `http://localhost:5103` |
| Document Processing | `http://localhost:5104` |
| AI Grading | `http://localhost:5105` |
| Review & Score | `http://localhost:5106` |
| Report & Audit | `http://localhost:5107` |
| Job Status | `http://localhost:5108` |

Chạy tám service trước, sau đó chạy API Gateway. Mỗi service có:

- `/` để xem service identity.
- `/health` để health check.
- `/openapi/v1.json` khi chạy môi trường Development.

## Endpoint skeleton

- `POST /auth/login`
- `POST /auth/refresh`
- `POST /exams`
- `POST /questions/{id}/rubric-criteria`
- `GET /exams/{id}/rubric`
- `POST /batches/upload`
- `GET /batches/{id}`
- `POST /ocr/jobs`
- `GET /ocr/results/{submissionId}`
- `POST /grading/jobs`
- `GET /grading/suggestions/{submissionId}`
- `PUT /scores/{id}`
- `POST /scores/finalize`
- `GET /reports/export?batchId=...`
- `GET /audit-logs`
- `GET /jobs/{id}`
- `GET /batches/{id}/progress`

## Thứ tự triển khai đề xuất

1. EF Core + SQL Server, tách schema theo bounded context.
2. Identity/JWT và seed hai role `Admin`, `Teacher`.
3. Batch upload + MinIO/local storage.
4. RabbitMQ events và idempotent consumers.
5. Document extraction cho DOCX/PDF/JPG/PNG.
6. AI grading theo rubric-first JSON contract; giữ `Mock` provider cho demo.
7. Teacher review/finalize và audit old/new score.
8. Hangfire retry, cleanup và export Excel/PDF.

Chi tiết ranh giới service và event flow nằm tại [docs/architecture/README.md](docs/architecture/README.md).
