# FPTU Paper Grading System

FPTU Paper Grading System (FPTU PGS) là hệ thống hỗ trợ chấm bài PE trên giấy/file scan cho assignment PRN232. Hệ thống cho phép Admin quản lý tài khoản, đề thi, rubric, upload bài theo batch; AI đưa ra gợi ý điểm theo rubric; Teacher dùng ứng dụng desktop để review, chỉnh điểm và finalize kết quả cuối cùng.

Tài liệu kiến trúc gốc: `FPTU_PGS_Assignment_System_Architecture.docx` - phiên bản 1.0, ngày 18/06/2026.

> Trạng thái hiện tại: dự án đã có monorepo .NET 10, API Gateway, các service chính, WPF desktop client, EF Core migration cho một số bounded context và test nghiệp vụ cốt lõi. Một số phần vẫn là skeleton/mock để phục vụ demo và cần hoàn thiện trước khi end-to-end production-like.

## Mục Tiêu MVP

- Chỉ có 2 role: `Admin` và `Teacher`.
- Teacher thao tác qua Desktop Tool `.exe`, không chấm trực tiếp trên web.
- Admin import đề/rubric, upload batch bài làm và phân công bài cho Teacher.
- AI chỉ là nguồn gợi ý điểm; Teacher là người quyết định điểm cuối cùng.
- Hỗ trợ chấm theo rubric-first JSON contract, lưu cả AI score và Teacher score để audit.
- Backend demo qua Docker Compose với SQL Server, RabbitMQ, MinIO và các API service.

## Tech Stack

| Layer | Công nghệ |
| --- | --- |
| Backend | .NET 10, ASP.NET Core Minimal APIs |
| API Gateway | YARP Reverse Proxy, Swagger UI tổng hợp |
| Desktop client | WPF `.NET 10 Windows` |
| Persistence | SQL Server, EF Core Code First, schema theo bounded context |
| Async/infra | RabbitMQ, Hangfire theo kiến trúc mục tiêu; hiện chưa nối workflow thật |
| File storage | MinIO/local storage theo cấu hình Docker; hiện Submission/OCR còn skeleton |
| AI provider | Mock provider, Gemini provider, system key pool, BYOK cho Teacher |
| Tests | xUnit, EF Core InMemory |

## Kiến Trúc Repository

```text
src/
  ApiGateway/
    Fptu.Pgs.ApiGateway                 # YARP + Swagger UI tổng hợp
  BuildingBlocks/
    Fptu.Pgs.BuildingBlocks             # health check, service defaults
    Fptu.Pgs.Contracts                  # DTO, events, enum dùng chung
  Clients/
    AdminPanel                          # placeholder frontend web admin
    TeacherDesktop                      # WPF app cho Admin/Teacher workflows
  Services/
    Identity                            # user, role, login, dev token
    Exam                                # subject, exam import, rubric
    Submission                          # batch upload skeleton
    DocumentProcessing                  # OCR/document extraction skeleton
    AiGrading                           # mock/Gemini grading, BYOK, sync score
    ReviewScore                         # assignment, AI score, teacher grade, finalize
    ReportAudit                         # report/audit skeleton
    JobStatus                           # job/batch progress skeleton
deploy/
  docker-compose.yml
  Dockerfile.service
  start.ps1
docs/
  architecture/
tests/
  Fptu.Pgs.Architecture.Tests
```

## Service Ownership

| Service | Schema/State | Trách nhiệm | Trạng thái |
| --- | --- | --- | --- |
| API Gateway | N/A | Route toàn bộ request và gom Swagger service | Done |
| Identity | `identity` | Login, dev token, Admin user management, seed users | In progress |
| Exam & Rubric | `exam`, subject in-memory | Subject, import đề, lưu/publish rubric | In progress |
| Submission | mục tiêu `submission` | Upload batch, file metadata, trạng thái đầu vào | Skeleton |
| Document Processing | mục tiêu `ocr` | OCR/extract text, image, table từ PDF/DOCX/JPG/PNG | Skeleton |
| AI Grading | `grading` | Chấm rubric-first bằng Mock/Gemini, lưu AI result, BYOK | In progress |
| Review & Score | `score` | Phân công bài, teacher grade, finalize, audit score | In progress |
| Report & Audit | mục tiêu `system` | Export Excel/PDF, audit log toàn hệ thống | Skeleton |
| Job Status | mục tiêu `system` | Theo dõi job/submission/batch progress | Skeleton |
| Teacher Desktop | local WPF | Admin/Teacher UI gọi Gateway | In progress |
| Admin Panel Web | TBD | Web admin nếu nhóm cần thêm | Not started |

## Luồng Nghiệp Vụ Mục Tiêu

```text
Admin tạo user/subject
  -> Admin import đề thi và publish rubric
  -> Admin upload batch bài làm
  -> Submission phát BatchUploaded
  -> Document Processing OCR/extract nội dung
  -> AI Grading chấm theo rubric
  -> ReviewScore lưu AI score và tạo workload
  -> Admin phân công bài cho Teacher
  -> Teacher review/chỉnh điểm
  -> Teacher finalize
  -> Report/Audit xuất báo cáo và lưu lịch sử
```

Luồng hiện tại đã chạy tốt nhất ở nhánh `Exam/Rubric -> AI evaluate -> ReviewScore register -> Teacher grade/finalize`. Các đoạn `batch upload -> OCR -> queue -> background jobs -> report export` vẫn cần hoàn thiện.

## Project Tracking

Quy ước trạng thái:

- `Done`: đã có code và test/flow kiểm chứng ở mức phù hợp.
- `In progress`: đã có implementation chính nhưng còn thiếu bảo mật, persistence, integration hoặc UX hoàn chỉnh.
- `Skeleton`: có endpoint/UI/contract giữ chỗ, chưa có nghiệp vụ thật.
- `Not started`: chưa có implementation đáng kể.

### Epic 1 - Nền Tảng Kiến Trúc

| ID | User Story | Trạng thái | Bằng chứng hiện có | Việc còn lại |
| --- | --- | --- | --- | --- |
| E1-US01 | Là developer, tôi muốn có monorepo .NET solution để build/test toàn hệ thống một lệnh. | Done | `Fptu.Pgs.sln`, `src/`, `tests/` | Duy trì CI khi thêm service mới |
| E1-US02 | Là developer, tôi muốn mỗi service có cấu trúc Domain/Application/Infrastructure rõ ràng. | In progress | `docs/architecture/service-structure.md`, marker folders | Tách endpoint khỏi `Program.cs` khi file lớn |
| E1-US03 | Là client, tôi muốn gọi một Gateway duy nhất thay vì gọi từng service. | Done | `Fptu.Pgs.ApiGateway`, YARP routes | Thêm auth forwarding policy khi có JWT thật |
| E1-US04 | Là developer, tôi muốn Swagger của các service hiển thị tập trung. | Done | Gateway Swagger UI trỏ tới `/openapi/...` | Bổ sung mô tả API chi tiết hơn |
| E1-US05 | Là operator, tôi muốn chạy demo stack bằng Docker Compose. | In progress | `deploy/docker-compose.yml`, `deploy/start.ps1` | Health dependency sâu hơn, secret production |

### Epic 2 - Identity Và Phân Quyền

| ID | User Story | Trạng thái | Bằng chứng hiện có | Việc còn lại |
| --- | --- | --- | --- | --- |
| E2-US01 | Là Admin/Teacher, tôi muốn đăng nhập bằng email/password. | In progress | `POST /auth/login`, password hashing, seed users | Thay `DevelopmentTokenStore` bằng JWT/refresh token thật |
| E2-US02 | Là Admin, tôi muốn tạo tài khoản Admin/Teacher. | Done | `POST /users`, WPF User Management | Thêm validation theo policy FPTU nếu cần |
| E2-US03 | Là Admin, tôi muốn bật/tắt tài khoản người dùng. | Done | `PATCH /users/{id}/status` | Audit log thay đổi trạng thái |
| E2-US04 | Là Admin, tôi muốn reset mật khẩu cho user. | Done | `POST /users/{id}/reset-password` | Buộc đổi mật khẩu lần đăng nhập tiếp theo |
| E2-US05 | Là hệ thống, tôi muốn chỉ Admin được gọi API quản trị user. | In progress | endpoint filter kiểm tra dev token admin | Chuyển sang authorization policy bằng JWT claim |

### Epic 3 - Exam, Subject Và Rubric

| ID | User Story | Trạng thái | Bằng chứng hiện có | Việc còn lại |
| --- | --- | --- | --- | --- |
| E3-US01 | Là Admin, tôi muốn xem/tạo subject. | Skeleton | `GET/POST /subjects`, in-memory store | Persist subject vào DB/schema `academic` |
| E3-US02 | Là Admin, tôi muốn gán Teacher vào subject. | Skeleton | `/subjects/{code}/teachers`, in-memory assignment | Đồng bộ với Identity/teacher eligibility thật |
| E3-US03 | Là Admin, tôi muốn import đề thi DOCX/PDF. | Done | `POST /exams/import`, EF `ExamDefinition` | Lưu file qua object storage nếu kích thước tăng |
| E3-US04 | Là Admin, tôi muốn tạo/sửa rubric criteria cho đề. | Done | `PUT /exams/{id}/rubric` | UI validation nâng cao, versioning rubric |
| E3-US05 | Là Admin, tôi muốn publish rubric trước khi AI chấm. | Done | `POST /exams/{id}/rubric/publish` | Lock rubric hoặc tạo phiên bản sau publish |
| E3-US06 | Là AI service, tôi muốn lấy published rubric theo exam. | Done | `GET /exams/{id}/rubric`, `evaluate-from-exam` | Cache/rate-limit service-to-service calls |

### Epic 4 - Submission Batch Và File Storage

| ID | User Story | Trạng thái | Bằng chứng hiện có | Việc còn lại |
| --- | --- | --- | --- | --- |
| E4-US01 | Là Admin, tôi muốn upload nhiều bài làm một lần. | Skeleton | `POST /batches/upload` nhận multipart và trả `BatchId` | Lưu file metadata, storage object key, submission rows |
| E4-US02 | Là Admin/Teacher, tôi muốn xem danh sách submission trong batch. | Skeleton | `GET /batches/{id}` trả dữ liệu giả | DB query thật, paging/filter |
| E4-US03 | Là hệ thống, tôi muốn lưu trạng thái từng submission. | Not started | `SubmissionStatus` contract | Bảng `Submissions`, `StatusHistory` |
| E4-US04 | Là hệ thống, tôi muốn phát event `BatchUploaded`. | Not started | event contracts trong BuildingBlocks | RabbitMQ publisher/idempotency |
| E4-US05 | Là operator, tôi muốn file lưu ở MinIO/local storage. | Not started | Compose có MinIO config | Storage adapter, bucket init, cleanup |

### Epic 5 - OCR / Document Processing

| ID | User Story | Trạng thái | Bằng chứng hiện có | Việc còn lại |
| --- | --- | --- | --- | --- |
| E5-US01 | Là hệ thống, tôi muốn tạo OCR job cho từng submission. | Skeleton | `POST /ocr/jobs` trả accepted | Hangfire job thật, persisted job state |
| E5-US02 | Là hệ thống, tôi muốn trích text từ PDF/DOCX/JPG/PNG. | Not started | kiến trúc mô tả OpenXML/OCR | Implement extractor, OCR provider, file size policy |
| E5-US03 | Là AI service, tôi muốn lấy extracted text/images theo submission. | Skeleton | `GET /ocr/results/{submissionId}` trả empty result | Persist OCRResult, text blocks, image refs |
| E5-US04 | Là hệ thống, tôi muốn phát `OCRCompleted/OCRFailed`. | Not started | event names trong docs/contracts | RabbitMQ consumer/publisher + retry |
| E5-US05 | Là Teacher/Admin, tôi muốn thấy lỗi OCR rõ ràng. | Not started | JobStatus skeleton | Error model, UI display, manual retry |

### Epic 6 - AI Grading

| ID | User Story | Trạng thái | Bằng chứng hiện có | Việc còn lại |
| --- | --- | --- | --- | --- |
| E6-US01 | Là hệ thống, tôi muốn chấm bài theo rubric-first contract. | Done | `POST /grading/evaluate`, validators, result entity | Bổ sung prompt regression tests |
| E6-US02 | Là hệ thống, tôi muốn chấm từ published exam rubric. | Done | `POST /grading/evaluate-from-exam` | Tối ưu service-to-service resiliency |
| E6-US03 | Là developer, tôi muốn dùng Mock provider để demo ổn định. | Done | `MockGradingProvider` | Dữ liệu mock sát rubric/demo hơn |
| E6-US04 | Là hệ thống, tôi muốn dùng Gemini khi có API key. | In progress | `GeminiGradingProvider`, `AI_PROVIDER`, `GOOGLE_API_KEY(S)` | Quan sát quota, structured error mapping |
| E6-US05 | Là Teacher, tôi muốn dùng API key Gemini cá nhân. | Done | credential endpoints, Data Protection, tests | Lấy `TeacherId` từ JWT thay vì client gửi |
| E6-US06 | Là hệ thống, tôi muốn fallback qua pool system key. | Done | `SystemApiKeyPool`, retry tests | Thêm metrics/quota dashboard |
| E6-US07 | Là ReviewScore, tôi muốn nhận AI grade tự động sau khi chấm. | In progress | `ReviewScoreClient.TryRegisterAsync` | Chuyển sang event-based sync hoặc outbox |

### Epic 7 - Review, Assignment Và Final Score

| ID | User Story | Trạng thái | Bằng chứng hiện có | Việc còn lại |
| --- | --- | --- | --- | --- |
| E7-US01 | Là Admin, tôi muốn phân công một submission cho Teacher. | Done | `POST /assignments` | Kiểm tra Teacher đủ quyền subject/exam |
| E7-US02 | Là Admin, tôi muốn bulk assign nhiều submission. | Done | `POST /assignments/bulk` | Import từ batch thật |
| E7-US03 | Là Admin, tôi muốn chia bài round-robin cho nhiều Teacher. | Done | `POST /assignments/distribute` | Rule workload/capacity nâng cao |
| E7-US04 | Là Teacher, tôi muốn xem workload của mình. | Done | `GET /assignments/teachers/{teacherId}` | UI lọc theo trạng thái/hạn chấm |
| E7-US05 | Là Teacher, tôi muốn chỉ chấm bài được phân công. | Done | domain tests `GradingAssignmentTests` | Enforce bằng auth claim trên API |
| E7-US06 | Là Teacher, tôi muốn sửa điểm/feedback theo criterion. | Done | `PUT /scores/submissions/{id}/teacher-grade`, WPF Review | UX so sánh evidence/missing points |
| E7-US07 | Là Teacher, tôi muốn finalize điểm cuối cùng. | Done | `POST /scores/submissions/{id}/finalize`, tests | Lock chỉnh sửa sau finalize bằng policy rõ ràng |
| E7-US08 | Là hệ thống, tôi muốn AI score không bị ghi đè bởi Teacher score. | Done | `TeacherGradingFlowTests` | Hiển thị diff trong report |

### Epic 8 - Report, Audit Và Job Tracking

| ID | User Story | Trạng thái | Bằng chứng hiện có | Việc còn lại |
| --- | --- | --- | --- | --- |
| E8-US01 | Là Admin/Teacher, tôi muốn export report theo batch. | Skeleton | `GET /reports/export` trả queued giả | Tạo Excel/PDF thật, lưu export file |
| E8-US02 | Là Admin, tôi muốn xem audit log hệ thống. | Skeleton | `GET /audit-logs` trả empty items | Gom audit từ scoring/user/report |
| E8-US03 | Là Teacher/Admin, tôi muốn xem tiến độ batch. | Skeleton | `GET /batches/{id}/progress` | Tính progress từ submission/job state thật |
| E8-US04 | Là operator, tôi muốn xem trạng thái từng job. | Skeleton | `GET /jobs/{id}` trả progress giả | Persist Hangfire/job state, retry metadata |
| E8-US05 | Là hệ thống, tôi muốn retry job lỗi. | Not started | kiến trúc đề xuất Hangfire | Implement retry policy và dead-letter handling |

### Epic 9 - Teacher Desktop UX

| ID | User Story | Trạng thái | Bằng chứng hiện có | Việc còn lại |
| --- | --- | --- | --- | --- |
| E9-US01 | Là user, tôi muốn login vào desktop app. | In progress | WPF gọi `/auth/login` | Token refresh và secure local storage |
| E9-US02 | Là Admin, tôi muốn quản lý user trên desktop. | Done | WPF User Management | Loading/empty/error polish |
| E9-US03 | Là Admin, tôi muốn import exam và edit rubric. | Done | WPF Exams & Rubrics | Rubric versioning/preview document |
| E9-US04 | Là Admin, tôi muốn upload batch. | Skeleton | UI và endpoint skeleton | Nối với Submission DB/storage thật |
| E9-US05 | Là Admin, tôi muốn phân công bài cho Teacher. | In progress | WPF assignment flow + ReviewScore API | Chọn từ batch/submission thật |
| E9-US06 | Là Teacher, tôi muốn xem bài được giao. | In progress | workload endpoint/UI | Thêm detail route từ selected work item |
| E9-US07 | Là Teacher, tôi muốn review AI score và finalize. | Done | WPF Review Scores gọi `/scores/...` | Load từ assignment thay vì nhập submission id thủ công |
| E9-US08 | Là Teacher, tôi muốn cấu hình Gemini API key cá nhân. | Done | WPF BYOK controls | UX giải thích fallback/quota rõ hơn |

### Epic 10 - Quality, Security Và Deployment

| ID | User Story | Trạng thái | Bằng chứng hiện có | Việc còn lại |
| --- | --- | --- | --- | --- |
| E10-US01 | Là developer, tôi muốn test rule nghiệp vụ quan trọng. | Done | xUnit tests cho BYOK, assignment, score flow | Thêm integration tests qua WebApplicationFactory |
| E10-US02 | Là developer, tôi muốn EF migration cho context có DB thật. | In progress | Identity, Exam, AiGrading, ReviewScore migrations | Migration cho Submission/OCR/System |
| E10-US03 | Là operator, tôi muốn secret không hard-code trong repo. | In progress | `.env`, DataProtection key volume | Secret manager/KMS cho production |
| E10-US04 | Là hệ thống, tôi muốn queue/background jobs xử lý async. | Not started | RabbitMQ/Hangfire config định hướng | Broker abstraction, consumers, outbox |
| E10-US05 | Là nhóm demo, tôi muốn publish Teacher Desktop thành EXE. | In progress | WPF project publish profile | Installer/signing, config gateway URL |

## Endpoint Map Hiện Có

| Nhóm | Endpoint |
| --- | --- |
| Identity | `POST /auth/login`, `POST /auth/refresh` |
| Users | `GET /users`, `GET /users/{id}`, `POST /users`, `PUT /users/{id}`, `PATCH /users/{id}/status`, `POST /users/{id}/reset-password` |
| Subjects | `GET /subjects`, `POST /subjects`, `POST /subjects/{subjectCode}/teachers`, `GET /subjects/{subjectCode}/teachers`, `GET /teachers/{teacherId}/subjects` |
| Exams/Rubrics | `GET /exams`, `POST /exams/import`, `GET /exams/{id}/rubric`, `PUT /exams/{id}/rubric`, `POST /exams/{id}/rubric/publish`, `GET /exams/{id}/document`, `DELETE /exams/{id}` |
| Submission | `POST /batches/upload`, `GET /batches/{id}` |
| OCR | `POST /ocr/jobs`, `GET /ocr/results/{submissionId}` |
| AI Grading | `POST /grading/evaluate`, `POST /grading/evaluate-from-exam`, `GET /grading/results/{submissionId}`, `GET /grading/suggestions/{submissionId}` |
| AI Credentials | `GET /grading/credentials/{teacherId}`, `PUT /grading/credentials/{teacherId}`, `POST /grading/credentials/{teacherId}/test`, `DELETE /grading/credentials/{teacherId}` |
| Assignments | `POST /assignments`, `POST /assignments/bulk`, `POST /assignments/distribute`, `GET /assignments/teachers/{teacherId}`, `GET /assignments/exams/{examId}`, `POST /assignments/{id}/start`, `POST /assignments/{id}/cancel` |
| Scores | `POST /scores/ai-grade`, `GET /scores/submissions/{submissionId}`, `PUT /scores/submissions/{submissionId}/teacher-grade`, `POST /scores/submissions/{submissionId}/finalize` |
| Reports/Audit | `GET /reports/export`, `GET /audit-logs` |
| Jobs | `GET /jobs/{jobId}`, `GET /batches/{batchId}/progress` |

## Build Và Test

Yêu cầu:

- .NET SDK `10.0.301` hoặc feature band tương thích.
- Docker Desktop nếu chạy toàn bộ stack.
- Visual Studio/Rider/VS Code cho workflow phát triển.

```powershell
dotnet restore Fptu.Pgs.sln
dotnet build Fptu.Pgs.sln --configuration Release
dotnet test Fptu.Pgs.sln --configuration Release
```

## EF Core Code First

Repository có local tool manifest cho `dotnet-ef`.

```powershell
dotnet tool restore
```

Các bounded context đang có migration:

```powershell
dotnet tool run dotnet-ef database update `
  --project src/Services/Identity/Fptu.Pgs.Identity.Api

dotnet tool run dotnet-ef database update `
  --project src/Services/Exam/Fptu.Pgs.Exam.Api

dotnet tool run dotnet-ef database update `
  --project src/Services/AiGrading/Fptu.Pgs.AiGrading.Api

dotnet tool run dotnet-ef database update `
  --project src/Services/ReviewScore/Fptu.Pgs.ReviewScore.Api
```

Các service dùng chung database demo `PaperGardingSystem`, nhưng mỗi service sở hữu schema và migration history riêng.

## Chạy Bằng Docker Compose

```powershell
Copy-Item deploy/.env.example deploy/.env
# Cập nhật password/API key trong deploy/.env
.\deploy\start.ps1
```

Hoặc chạy trực tiếp:

```powershell
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up --build
```

Địa chỉ chính:

| Thành phần | URL |
| --- | --- |
| API Gateway | `http://localhost:5000` |
| Swagger UI | `http://localhost:5000/swagger/index.html` |
| RabbitMQ Management | `http://localhost:15672` |
| MinIO Console | `http://localhost:9001` |
| SQL Server | `localhost,1433` |

Không commit `deploy/.env`. Đổi toàn bộ password mẫu trước khi demo trên máy/host chung.

## Chạy Trực Tiếp Trong IDE

Các port mặc định:

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

Mỗi service có:

- `/` để xem service identity.
- `/health` để kiểm tra health.
- `/openapi/v1.json` khi bật OpenAPI.

## Cấu Hình AI

Mặc định service dùng `Mock` để demo ổn định.

```env
AI_PROVIDER=Mock
```

Dùng Gemini bằng system key:

```env
AI_PROVIDER=Gemini
GOOGLE_API_KEY=api-key-1
GOOGLE_API_KEYS=api-key-2,api-key-3
```

Ghi chú:

- `GOOGLE_API_KEY` là key chính và cũng tham gia pool.
- `GOOGLE_API_KEYS` là danh sách key bổ sung, phân cách bằng dấu phẩy.
- Service bỏ key trùng và thử key tiếp theo nếu gặp lỗi quota/auth/tạm thời.
- Nếu có Google key nhưng không set `AI_PROVIDER`, service tự chọn Gemini.
- Key cá nhân của Teacher được ưu tiên hơn system key; fallback sang system key chỉ khi Teacher bật fallback.

## BYOK Cho Teacher

Teacher Desktop có khu vực cấu hình Gemini API key cá nhân. Backend xử lý key như sau:

- Mã hóa bằng ASP.NET Core Data Protection trước khi lưu database.
- Không trả key gốc về client; chỉ trả masked key.
- Không lưu key trong grading result, audit log hoặc exception.
- Kết quả grading chỉ lưu `CredentialSource`: `User`, `System` hoặc `None`.

Endpoint:

```text
GET    /grading/credentials/{teacherId}?provider=Gemini
PUT    /grading/credentials/{teacherId}
POST   /grading/credentials/{teacherId}/test?provider=Gemini
DELETE /grading/credentials/{teacherId}?provider=Gemini
```

Khi triển khai JWT thật, backend phải lấy `TeacherId` từ claim đăng nhập thay vì tin vào ID do client gửi.

## Publish Teacher Desktop Thành EXE

```powershell
dotnet publish src/Clients/TeacherDesktop/Fptu.Pgs.TeacherDesktop/Fptu.Pgs.TeacherDesktop.csproj `
  /p:PublishProfile=Windows-x64
```

Output:

```text
src/Clients/TeacherDesktop/Fptu.Pgs.TeacherDesktop/bin/Publish/win-x64/Fptu.Pgs.TeacherDesktop.exe
```

## Roadmap Ưu Tiên Tiếp Theo

1. Hoàn thiện Submission Service: DB schema, file metadata, MinIO adapter, `BatchUploaded` event.
2. Hoàn thiện Document Processing: extractor cho DOCX/PDF/image, OCR result persistence, `OCRCompleted/OCRFailed`.
3. Nối RabbitMQ và Hangfire thật cho luồng batch grading bất đồng bộ.
4. Thay `DevelopmentTokenStore` bằng JWT/refresh token production-like và authorization policy.
5. Hoàn thiện Report/Audit: export Excel/PDF, audit log từ scoring/user/job.
6. Cải thiện Teacher Desktop: chọn submission từ workload/batch thay vì nhập GUID thủ công.
7. Thêm integration tests cho Gateway + service endpoints và smoke test Docker Compose.

## Tài Liệu Liên Quan

- `docs/architecture/README.md`: quyết định kiến trúc và ranh giới service.
- `docs/architecture/service-structure.md`: convention cấu trúc service.
- `docs/architecture/admin-teacher-assignment.md`: thiết kế phân công Admin/Teacher.
- `FPTU_PGS_Assignment_System_Architecture.docx`: tài liệu kiến trúc assignment gốc.
