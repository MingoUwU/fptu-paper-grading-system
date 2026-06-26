# Seed users

Identity Service seeds three development accounts on startup:

| Role | Email | Password | Subject |
| --- | --- | --- | --- |
| Admin | `admin@fptu.edu.vn` | `Admin@123` | - |
| Teacher | `teacher.swt@fptu.edu.vn` | `Teacher@123` | `SWT` |
| Teacher | `teacher.srs@fptu.edu.vn` | `Teacher@123` | `SRS` |

Passwords are stored as PBKDF2 hashes in `[identity].[Users]`, not as plain text.

These accounts are for local development/demo only. Before production, replace this seed flow
with an admin-managed user screen, forced password reset, and real JWT signing settings.
