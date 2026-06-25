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

## EF Core Code First

Repository có local tool manifest cho `dotnet-ef` và migration ban đầu của hai
bounded context `grading` và `score`:

```powershell
dotnet tool restore

dotnet tool run dotnet-ef database update `
  --project src/Services/AiGrading/Fptu.Pgs.AiGrading.Api

dotnet tool run dotnet-ef database update `
  --project src/Services/ReviewScore/Fptu.Pgs.ReviewScore.Api
```

Hai service dùng cùng database `PaperGardingSystem`, nhưng mỗi service sở hữu
schema và migration history riêng.

## AI chấm và Teacher chấm lại

Luồng điểm:

```text
OCR/Document Processing
→ POST /grading/evaluate
→ AI Score được lưu trong schema grading
→ Review Score nhận bản AI grade
→ Teacher sửa điểm theo từng criterion
→ TeacherGraded
→ Finalized (Teacher Score là điểm chính thức)
```

AI Score không bị ghi đè khi Teacher chấm lại. Hệ thống giữ cả hai điểm và audit
log để so sánh độ lệch.

Mặc định service dùng `Mock` để phát triển local. Để dùng Gemini:

```env
AI_PROVIDER=Gemini
GOOGLE_API_KEY=api-key-1
GOOGLE_API_KEYS=api-key-2,api-key-3,api-key-4
```

Nếu có `GOOGLE_API_KEY` hoặc `GOOGLE_API_KEYS` mà không khai báo
`AI_PROVIDER`, service tự chọn Gemini. Đặt `AI_PROVIDER=Mock` khi muốn ép dùng
mock dù máy đang có key.

API nhận `ExtractedText` và/hoặc PDF dạng Base64. DOCX nên được Document
Processing Service tách text, bảng, ảnh và chuyển sang PDF trước khi gửi sang AI.

### Nhiều system API key

AI Grading Service đọc:

```env
GOOGLE_API_KEY=api-1
GOOGLE_API_KEYS=api-2,api-3,api-4,api-5
```

- `GOOGLE_API_KEY` là key chính và cũng tham gia pool.
- `GOOGLE_API_KEYS` là danh sách key bổ sung, phân cách bằng dấu phẩy.
- Service loại bỏ key trùng và chọn điểm bắt đầu theo round-robin.
- Nếu một key trả lỗi quota/auth/tạm thời, service thử key tiếp theo.
- Kết quả chỉ lưu `CredentialSource=System`; không lưu key hoặc vị trí key.
- Có thể chỉ cấu hình một trong hai biến.

Key cá nhân do Teacher nhập vẫn là một key duy nhất và được ưu tiên. Nếu Teacher
không có key, hệ thống dùng pool. Nếu key cá nhân lỗi, pool chỉ được dùng khi
Teacher bật tùy chọn fallback.

Lưu ý: các key thuộc cùng Google Cloud project thường chia sẻ cùng quota project.
Pool hữu ích cho các credential/project hợp lệ khác nhau và khả năng dự phòng,
không nên dùng để né giới hạn dịch vụ.

### API key cá nhân của Teacher (BYOK)

Teacher Desktop có khu vực `Gemini API cá nhân`. Key được gửi qua backend và:

- Mã hóa bằng ASP.NET Core Data Protection trước khi lưu database.
- Không trả key gốc về WPF; API chỉ trả chuỗi đã che.
- Không ghi key vào grading result, audit log hoặc exception.
- Ưu tiên key cá nhân khi chấm.
- Nếu Teacher bật fallback và key cá nhân lỗi quota/auth, hệ thống thử key chung.
- Nếu Teacher không cấu hình key cá nhân, hệ thống dùng key chung.

Các endpoint:

```text
GET    /grading/credentials/{teacherId}?provider=Gemini
PUT    /grading/credentials/{teacherId}
POST   /grading/credentials/{teacherId}/test?provider=Gemini
DELETE /grading/credentials/{teacherId}?provider=Gemini
```

`POST /grading/evaluate` yêu cầu thêm `TeacherId`. Kết quả chỉ lưu nguồn credential:
`User`, `System` hoặc `None`.

Key ring Data Protection được giữ tại `.keys` khi chạy local và Docker volume
`ai-grading-keys` khi chạy Compose. Production nên chuyển key ring sang secret
store/KMS phù hợp. Khi JWT thật được triển khai, backend phải lấy `TeacherId` từ
claim đăng nhập thay vì tin vào ID do client gửi.

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

## Publish Teacher Desktop thành EXE

Teacher Desktop sử dụng WPF trên .NET 10. Profile `Windows-x64` tạo một file EXE
self-contained, máy giáo viên không cần cài .NET Runtime:

```powershell
dotnet publish src/Clients/TeacherDesktop/Fptu.Pgs.TeacherDesktop/Fptu.Pgs.TeacherDesktop.csproj `
  /p:PublishProfile=Windows-x64
```

Output:

```text
src/Clients/TeacherDesktop/Fptu.Pgs.TeacherDesktop/bin/Publish/win-x64/Fptu.Pgs.TeacherDesktop.exe
```

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
- `POST /grading/evaluate`
- `GET /grading/results/{submissionId}`
- `GET /scores/submissions/{submissionId}`
- `PUT /scores/submissions/{submissionId}/teacher-grade`
- `POST /scores/submissions/{submissionId}/finalize`
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
