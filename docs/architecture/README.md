# Kiến trúc skeleton

Skeleton này bám theo tài liệu `FPTU Grading System for Paper-Based PE` phiên bản 1.0 ngày 18/06/2026.

## Quyết định chính

- Monorepo, một solution .NET 10.
- API Gateway dùng YARP.
- Tám bounded context triển khai thành tám Web API độc lập.
- Một SQL Server instance cho MVP, mỗi bounded context sở hữu schema riêng.
- RabbitMQ dành cho domain/integration events.
- Hangfire dành cho scheduled jobs, retry, export và cleanup.
- MinIO hoặc local storage giữ file scan và artifact xử lý.
- AI chấm vòng đầu và lưu `AiScore`, evidence, feedback; Teacher chấm lại và `TeacherScore` trở thành điểm chính thức khi finalize.

## Ranh giới service

| Service | Schema | Trách nhiệm |
| --- | --- | --- |
| Identity | `identity` | User, role, JWT, refresh token |
| Exam & Rubric | `academic`, `exam` | Subject, exam, question, rubric criteria |
| Submission | `submission` | Batch upload, file metadata, trạng thái đầu vào |
| Document Processing | `ocr` | Trích text/ảnh từ PDF, DOCX, JPG, PNG |
| AI Grading | `grading` | Rubric-first grading, AI score, evidence, confidence |
| Review & Score | `score` | Lưu riêng AI score, Teacher chấm lại và finalize điểm |
| Report & Audit | `system` | Audit log, export Excel/PDF, notification |
| Job Status | `system` | Tiến độ batch/submission, retry và job visibility |

## Event flow

`BatchUploaded -> OcrCompleted/OcrFailed -> AiGradingCompleted/AiGradingFailed -> TeacherGradingCompleted -> ScoreFinalized`

Các contract ban đầu nằm trong `src/BuildingBlocks/Fptu.Pgs.Contracts`. Khi triển khai broker thật, mỗi consumer phải idempotent và lưu trạng thái xử lý để tránh chấm trùng.

AI Grading hỗ trợ BYOK cho Teacher. Credential cá nhân được mã hóa trong schema
`grading`; hệ thống chỉ lưu `User/System/None` vào kết quả chấm. Key hệ thống vẫn
nằm trong secret configuration của backend.

## Cách phát triển từng service

Trong mỗi API project, mở rộng theo vertical slice:

- `Domain`: entity, value object, domain rule.
- `Application`: command/query, validation, interface.
- `Infrastructure`: EF Core, RabbitMQ, storage, provider adapter.
- `Endpoints`: HTTP contract và mapping.

Không cho service truy cập trực tiếp DbContext/schema của service khác. Giao tiếp liên service qua HTTP ở luồng đồng bộ và event ở luồng bất đồng bộ.
