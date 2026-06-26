# Admin and Teacher assignment design

## Roles

The MVP has two roles:

- `Admin`: manages subjects, exams, rubrics, upload batches, and assigns submissions to teachers.
- `Teacher`: reviews only assigned submissions, adjusts AI scores, and finalizes scores.

## Service ownership

| Service | Owns | Notes |
| --- | --- | --- |
| Identity | Users, roles, login claims | User role must be `Admin` or `Teacher`. |
| Exam | Subjects, exams, rubrics, teacher-subject capability | A teacher can be assigned to one or more subjects. |
| Submission | Batches and submitted files | Admin uploads PE files here. |
| AI Grading | AI grading result | AI scores every submission before teacher review. |
| ReviewScore | Grading assignments and final scoring | Decides which teacher can review which submission. |

## Main flow

```text
Admin creates subject/exam/rubric
  -> Admin uploads submission batch
  -> DocumentProcessing extracts text/images/tables
  -> AiGrading grades each submission
  -> Admin assigns submissions to teachers
  -> Teacher opens own workload
  -> Teacher reviews AI score
  -> Teacher finalizes score
```

## Assignment rules

- One submission has one active grading assignment.
- One teacher can have many assigned submissions.
- A teacher can only grade/finalize a submission assigned to that teacher.
- Admin can distribute submissions manually or round-robin across multiple teachers.
- Teacher-subject assignment is used to decide which teachers are eligible for a subject.

## Important API groups

Exam service:

- `GET /subjects`
- `POST /subjects`
- `POST /subjects/{subjectCode}/teachers`
- `GET /subjects/{subjectCode}/teachers`
- `GET /teachers/{teacherId}/subjects`

ReviewScore service:

- `POST /assignments`
- `POST /assignments/bulk`
- `POST /assignments/distribute`
- `GET /assignments/teachers/{teacherId}`
- `GET /assignments/exams/{examId}`
- `POST /assignments/{assignmentId}/start?teacherId=...`
- `POST /assignments/{assignmentId}/cancel?adminId=...`

The current skeleton keeps `Exam` subject assignment in memory for quick UI/API progress.
`ReviewScore` assignment is persisted in SQL because it protects grading authorization and auditability.
