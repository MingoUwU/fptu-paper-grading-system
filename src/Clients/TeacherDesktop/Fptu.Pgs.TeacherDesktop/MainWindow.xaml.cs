using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Fptu.Pgs.Contracts;
using Microsoft.Win32;

namespace Fptu.Pgs.TeacherDesktop;

public partial class MainWindow : Window
{
    private const string VietnameseLanguage = "vi";
    private const string EnglishLanguage = "en";

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5000/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly ObservableCollection<CriterionReviewRow> _criteria = [];
    private readonly ObservableCollection<UserAccountResponse> _users = [];
    private readonly ObservableCollection<ExamSummaryResponse> _exams = [];
    private readonly ObservableCollection<RubricCriterionEditorRow> _rubricCriteria = [];
    private readonly Dictionary<object, string> _localizedObjects = [];
    private readonly Dictionary<DataGridColumn, string> _localizedColumns = [];
    private ScoreComparisonResponse? _currentScore;
    private LoginResponse? _currentUser;
    private ExamSummaryResponse? _selectedExam;
    private bool _suppressExamSelection;
    private string _language = VietnameseLanguage;

    private static readonly Brush ActiveNavigationBackground =
        new SolidColorBrush(Color.FromRgb(247, 127, 0));

    private static readonly Brush ActiveNavigationForeground = Brushes.White;

    private static readonly Brush InactiveNavigationBackground =
        new SolidColorBrush(Color.FromRgb(229, 231, 235));

    private static readonly Brush InactiveNavigationForeground =
        new SolidColorBrush(Color.FromRgb(17, 24, 39));

    private static readonly IReadOnlyDictionary<string, string> InitialTextKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Admin Console"] = "App.AdminConsole",
            ["Teacher Desktop"] = "App.TeacherDesktop",
            ["Teacher Workspace"] = "App.TeacherWorkspace",
            ["Assigned submissions only"] = "App.AssignedOnly",
            ["Not signed in"] = "User.NotSignedIn",
            ["Language"] = "App.Language",
            ["Dashboard"] = "Nav.Dashboard",
            ["User Management"] = "Nav.UserManagement",
            ["Exams & Rubrics"] = "Nav.ExamsRubrics",
            ["Exams &amp; Rubrics"] = "Nav.ExamsRubrics",
            ["Import an existing PE exam and define the rubric used by AI."] = "Exams.ImportDescription",
            ["Import existing exam"] = "Exams.ImportTitle",
            ["Exam code"] = "Exams.Code",
            ["Exam name"] = "Exams.Name",
            ["Semester"] = "Exams.Semester",
            ["Exam file (DOCX/PDF)"] = "Exams.File",
            ["Browse"] = "Exams.Browse",
            ["Import exam"] = "Exams.Import",
            ["Imported exams"] = "Exams.List",
            ["Rubric criteria"] = "Exams.RubricCriteria",
            ["Select an imported exam to edit its rubric."] = "Exams.SelectHint",
            ["AI instructions"] = "Exams.AiInstructions",
            ["Add criterion"] = "Exams.AddCriterion",
            ["Remove selected"] = "Exams.RemoveCriterion",
            ["Save draft"] = "Exams.SaveDraft",
            ["Publish rubric"] = "Exams.Publish",
            ["Import an exam, add criteria, save the draft, then publish it for AI grading."] = "Exams.WorkflowHint",
            ["Upload Batch"] = "Nav.UploadBatch",
            ["Assignments"] = "Nav.Assignments",
            ["My Assignments"] = "Nav.MyAssignments",
            ["Review Scores"] = "Nav.ReviewScores",
            ["Reports"] = "Nav.Reports",
            ["Logout"] = "Nav.Logout",
            ["Tong quan nhanh ve he thong cham bai PE."] = "Dashboard.Description",
            ["Pending review"] = "Dashboard.PendingReview",
            ["AI graded"] = "Dashboard.AiGraded",
            ["Finalized"] = "Dashboard.Finalized",
            ["Gateway"] = "Dashboard.Gateway",
            ["Next step"] = "Dashboard.NextStep",
            ["Man hinh nay se hien thong ke batch, so bai can cham lai, va canh bao quota API."] = "Dashboard.NextStepDescription",
            ["Quan ly de PE va tieu chi cham diem."] = "Exams.Description",
            ["Rubric setup"] = "Exams.RubricSetup",
            ["Sau nay Teacher se nhap rubric/criteria tai day. AI se dua vao cac tieu chi nay de cham bai."] = "Exams.RubricDescription",
            ["Upload file DOCX/PDF bai lam de Document Processing tach text, bang va hinh."] = "Upload.Description",
            ["Batch input"] = "Upload.BatchInput",
            ["Drop file area placeholder - se noi voi Submission Service va Document Processing Service."] = "Upload.DropPlaceholder",
            ["Bao cao diem, audit log va export."] = "Reports.Description",
            ["Report center"] = "Reports.Center",
            ["Sau nay man hinh nay dung de xem do lech AI/Teacher, export Excel/PDF va tra cuu audit log."] = "Reports.CenterDescription",
            ["Teacher Re-grading"] = "Review.Title",
            ["AI cháº¥m vÃ²ng Ä‘áº§u. Teacher kiá»ƒm tra tá»«ng tiÃªu chÃ­ vÃ  quyáº¿t Ä‘á»‹nh Ä‘iá»ƒm chÃ­nh thá»©c."] = "Review.Description",
            ["ChÆ°a táº£i bÃ i"] = "Status.NotLoaded",
            ["Chua tai bai"] = "Status.NotLoaded",
            ["Gemini API cÃ¡ nhÃ¢n (BYOK)"] = "Credential.Title",
            ["Äang kiá»ƒm tra..."] = "Credential.Checking",
            ["Dang kiem tra..."] = "Credential.Checking",
            ["Gemini API Key"] = "Credential.ApiKey",
            ["LÆ°u key"] = "Credential.SaveKey",
            ["Kiá»ƒm tra"] = "Credential.Test",
            ["XÃ³a"] = "Credential.Delete",
            ["Náº¿u key cÃ¡ nhÃ¢n háº¿t quota hoáº·c lá»—i, cho phÃ©p dÃ¹ng API key há»‡ thá»‘ng"] = "Credential.Fallback",
            ["Submission ID"] = "Review.SubmissionId",
            ["Táº£i káº¿t quáº£ AI"] = "Review.LoadAiResult",
            ["AI Score"] = "Review.AiScore",
            ["Teacher Score"] = "Review.TeacherScore",
            ["Difference"] = "Review.Difference",
            ["Final Score"] = "Review.FinalScore",
            ["Äiá»ƒm theo rubric"] = "Review.RubricScores",
            ["AI feedback"] = "Review.AiFeedback",
            ["Teacher feedback"] = "Review.TeacherFeedback",
            ["Nháº­p Submission ID Ä‘á»ƒ báº¯t Ä‘áº§u review."] = "Message.EnterSubmission",
            ["Nhap Submission ID de bat dau review."] = "Message.EnterSubmission",
            ["LÆ°u Ä‘iá»ƒm Teacher"] = "Review.SaveTeacherGrade",
            ["Finalize Ä‘iá»ƒm"] = "Review.Finalize",
            ["Login to Paper Grading System"] = "Login.Subtitle",
            ["Email"] = "Login.Email",
            ["Password"] = "Login.Password",
            ["Seed users: admin@fptu.edu.vn / Admin@123, teacher.swt@fptu.edu.vn / Teacher@123"] = "Login.SeedUsers",
            ["Login"] = "Login.Button",
            ["Grading Assignments"] = "Assignments.Title",
            ["Assign submissions by subject and workload to teachers."] = "Assignments.Description",
            ["Unassigned"] = "Assignments.Unassigned",
            ["Assigned"] = "Assignments.Assigned",
            ["Completed"] = "Assignments.Completed",
            ["Assignment management"] = "Assignments.Management",
            ["Select an exam batch, filter teachers by subject, then distribute submissions equally or assign them manually."] = "Assignments.ManagementDescription",
            ["Create assignment"] = "Assignments.Create",
            ["Only submissions assigned to this teacher are shown here."] = "TeacherAssignments.Description",
            ["Waiting"] = "TeacherAssignments.Waiting",
            ["In progress"] = "TeacherAssignments.InProgress",
            ["Assigned submissions"] = "TeacherAssignments.List",
            ["Choose an assigned submission and open the review workspace to verify the AI score."] = "TeacherAssignments.ListDescription",
            ["Open review workspace"] = "TeacherAssignments.OpenReview",
            ["Create and manage Admin and Teacher accounts."] = "Users.Description",
            ["Refresh"] = "Users.Refresh",
            ["Full name"] = "Users.FullName",
            ["Role"] = "Users.Role",
            ["Subject"] = "Users.Subject",
            ["Active"] = "Users.Active",
            ["Last login"] = "Users.LastLogin",
            ["Enable / Disable"] = "Users.ToggleStatus",
            ["New password"] = "Users.NewPassword",
            ["Reset password"] = "Users.ResetPassword",
            ["Select one account before changing its status."] = "Users.SelectHint",
            ["Create account"] = "Users.CreateAccount",
            ["Subject code"] = "Users.SubjectCode",
            ["Create user"] = "Users.CreateUser",
            ["User data is managed by Identity Service."] = "Users.ManagedByIdentity",
            ["Teacher"] = "Users.Teacher",
            ["Admin"] = "Users.Admin"
        };

    private static readonly IReadOnlyDictionary<string, LocalizedText> Texts =
        new Dictionary<string, LocalizedText>(StringComparer.Ordinal)
        {
            ["App.AdminConsole"] = new("Khu vực quản trị", "Admin Console"),
            ["App.TeacherDesktop"] = new("Ứng dụng giảng viên", "Teacher Desktop"),
            ["App.TeacherWorkspace"] = new("Khu vực giảng viên", "Teacher Workspace"),
            ["App.AssignedOnly"] = new("Chỉ hiển thị bài được phân công", "Assigned submissions only"),
            ["App.Language"] = new("Ngôn ngữ", "Language"),
            ["User.NotSignedIn"] = new("Chưa đăng nhập", "Not signed in"),
            ["User.Role"] = new("Vai trò", "Role"),
            ["Nav.Dashboard"] = new("Tổng quan", "Dashboard"),
            ["Nav.UserManagement"] = new("Quản lý người dùng", "User Management"),
            ["Nav.ExamsRubrics"] = new("Đề thi & Rubric", "Exams & Rubrics"),
            ["Nav.UploadBatch"] = new("Upload bài thi", "Upload Batch"),
            ["Nav.Assignments"] = new("Phân công chấm", "Assignments"),
            ["Nav.MyAssignments"] = new("Bài được phân công", "My Assignments"),
            ["Nav.ReviewScores"] = new("Chấm lại điểm", "Review Scores"),
            ["Nav.Reports"] = new("Báo cáo", "Reports"),
            ["Nav.Logout"] = new("Đăng xuất", "Logout"),
            ["Dashboard.Description"] = new("Tổng quan nhanh về hệ thống chấm bài PE.", "Quick overview of the PE grading system."),
            ["Dashboard.PendingReview"] = new("Chờ chấm lại", "Pending review"),
            ["Dashboard.AiGraded"] = new("AI đã chấm", "AI graded"),
            ["Dashboard.Finalized"] = new("Đã finalize", "Finalized"),
            ["Dashboard.Gateway"] = new("Cổng API", "Gateway"),
            ["Dashboard.NextStep"] = new("Bước tiếp theo", "Next step"),
            ["Dashboard.NextStepDescription"] = new("Màn hình này sẽ hiển thị thống kê batch, số bài cần chấm lại và cảnh báo quota API.", "This screen will show batch statistics, pending review counts, and API quota alerts."),
            ["Exams.Description"] = new("Quản lý đề PE và tiêu chí chấm điểm.", "Manage PE exams and grading criteria."),
            ["Exams.ImportDescription"] = new("Nhập đề PE đã có và thiết lập rubric để AI sử dụng khi chấm.", "Import an existing PE exam and define the rubric used by AI."),
            ["Exams.ImportTitle"] = new("Nhập đề thi có sẵn", "Import existing exam"),
            ["Exams.Code"] = new("Mã đề", "Exam code"),
            ["Exams.Name"] = new("Tên đề thi", "Exam name"),
            ["Exams.Semester"] = new("Học kỳ", "Semester"),
            ["Exams.File"] = new("File đề thi (DOCX/PDF)", "Exam file (DOCX/PDF)"),
            ["Exams.Browse"] = new("Chọn file", "Browse"),
            ["Exams.Import"] = new("Nhập đề thi", "Import exam"),
            ["Exams.List"] = new("Danh sách đề thi", "Imported exams"),
            ["Exams.RubricCriteria"] = new("Tiêu chí chấm", "Rubric criteria"),
            ["Exams.SelectHint"] = new("Chọn một đề thi để chỉnh sửa rubric.", "Select an imported exam to edit its rubric."),
            ["Exams.AiInstructions"] = new("Hướng dẫn cho AI", "AI instructions"),
            ["Exams.AddCriterion"] = new("Thêm tiêu chí", "Add criterion"),
            ["Exams.RemoveCriterion"] = new("Xóa tiêu chí chọn", "Remove selected"),
            ["Exams.SaveDraft"] = new("Lưu bản nháp", "Save draft"),
            ["Exams.Publish"] = new("Publish rubric", "Publish rubric"),
            ["Exams.WorkflowHint"] = new("Nhập đề thi, thêm tiêu chí, lưu bản nháp rồi publish để AI chấm theo rubric này.", "Import an exam, add criteria, save the draft, then publish it for AI grading."),
            ["Exams.TotalFormat"] = new("Tổng điểm: {0:0.##}", "Total: {0:0.##}"),
            ["Exams.SelectedFormat"] = new("{0} — {1} | {2}", "{0} — {1} | {2}"),
            ["Exams.ImportRequired"] = new("Nhập đủ mã đề, tên đề, môn, học kỳ và chọn file DOCX/PDF.", "Enter the exam code, name, subject, semester, and select a DOCX/PDF file."),
            ["Exams.Imported"] = new("Đã nhập đề thi. Bây giờ hãy thêm tiêu chí chấm.", "Exam imported. Now add grading criteria."),
            ["Exams.LoadedFormat"] = new("Đã tải {0} đề thi.", "Loaded {0} exams."),
            ["Exams.SelectFirst"] = new("Chọn một đề thi trước nha bro.", "Select an exam first."),
            ["Exams.CriterionRequired"] = new("Mỗi tiêu chí cần tên, mô tả, hướng dẫn AI và điểm tối đa lớn hơn 0.", "Every criterion needs a name, description, AI instructions, and a positive maximum score."),
            ["Exams.Saved"] = new("Đã lưu rubric dạng bản nháp.", "Rubric draft saved."),
            ["Exams.Published"] = new("Đã publish rubric. AI có thể dùng tiêu chí này để chấm.", "Rubric published. AI can now grade with these criteria."),
            ["Exams.StatusDraft"] = new("Bản nháp", "Draft"),
            ["Exams.StatusPublished"] = new("Đã publish", "Published"),
            ["Exams.RubricSetup"] = new("Thiết lập rubric", "Rubric setup"),
            ["Exams.RubricDescription"] = new("Sau này giảng viên sẽ nhập rubric/criteria tại đây. AI sẽ dựa vào các tiêu chí này để chấm bài.", "Teachers will enter rubrics/criteria here. AI will grade submissions based on these criteria."),
            ["Upload.Description"] = new("Upload file DOCX/PDF bài làm để Document Processing tách text, bảng và hình.", "Upload DOCX/PDF submissions so Document Processing can extract text, tables, and images."),
            ["Upload.BatchInput"] = new("Thông tin batch", "Batch input"),
            ["Upload.DropPlaceholder"] = new("Khu vực kéo thả file - sau này sẽ nối với Submission Service và Document Processing Service.", "Drop file area placeholder - this will connect to Submission Service and Document Processing Service."),
            ["Reports.Description"] = new("Báo cáo điểm, audit log và export.", "Score reports, audit logs, and exports."),
            ["Reports.Center"] = new("Trung tâm báo cáo", "Report center"),
            ["Reports.CenterDescription"] = new("Sau này màn hình này dùng để xem độ lệch AI/Teacher, export Excel/PDF và tra cứu audit log.", "This screen will show AI/Teacher differences, Excel/PDF export, and audit log lookup."),
            ["Review.Title"] = new("Giảng viên chấm lại", "Teacher Re-grading"),
            ["Review.Description"] = new("AI chấm vòng đầu. Giảng viên kiểm tra từng tiêu chí và quyết định điểm chính thức.", "AI grades first. The teacher reviews each criterion and decides the official score."),
            ["Status.NotLoaded"] = new("Chưa tải bài", "Not loaded"),
            ["Credential.Title"] = new("Gemini API cá nhân (BYOK)", "Personal Gemini API (BYOK)"),
            ["Credential.Checking"] = new("Đang kiểm tra...", "Checking..."),
            ["Credential.ApiKey"] = new("Gemini API Key", "Gemini API Key"),
            ["Credential.SaveKey"] = new("Lưu key", "Save key"),
            ["Credential.Test"] = new("Kiểm tra", "Test"),
            ["Credential.Delete"] = new("Xóa", "Delete"),
            ["Credential.Fallback"] = new("Nếu key cá nhân hết quota hoặc lỗi, cho phép dùng API key hệ thống", "If the personal key runs out of quota or fails, allow using the system API key"),
            ["Credential.SavedFormat"] = new("Đã lưu: {0}", "Saved: {0}"),
            ["Credential.SystemKey"] = new("Đang dùng API key hệ thống", "Using system API key"),
            ["Credential.BackendUnavailable"] = new("Không kết nối được backend", "Cannot connect to backend"),
            ["Credential.SavedMessage"] = new("Đã mã hóa và lưu API key cá nhân trên backend.", "Personal API key was encrypted and saved on the backend."),
            ["Credential.Valid"] = new("Gemini API key cá nhân hoạt động.", "Personal Gemini API key works."),
            ["Credential.Deleted"] = new("Đã xóa API key cá nhân. Hệ thống sẽ dùng key chung.", "Personal API key deleted. The system will use the shared key."),
            ["Credential.Required"] = new("Nhập Gemini API key trước khi lưu.", "Enter a Gemini API key before saving."),
            ["Review.SubmissionId"] = new("Submission ID", "Submission ID"),
            ["Review.LoadAiResult"] = new("Tải kết quả AI", "Load AI result"),
            ["Review.AiScore"] = new("Điểm AI", "AI Score"),
            ["Review.TeacherScore"] = new("Điểm giảng viên", "Teacher Score"),
            ["Review.Difference"] = new("Độ lệch", "Difference"),
            ["Review.FinalScore"] = new("Điểm cuối", "Final Score"),
            ["Review.RubricScores"] = new("Điểm theo rubric", "Rubric scores"),
            ["Review.AiFeedback"] = new("Feedback AI", "AI feedback"),
            ["Review.TeacherFeedback"] = new("Feedback giảng viên", "Teacher feedback"),
            ["Review.SaveTeacherGrade"] = new("Lưu điểm giảng viên", "Save teacher grade"),
            ["Review.Finalize"] = new("Finalize điểm", "Finalize score"),
            ["Review.InvalidSubmissionId"] = new("Submission ID không hợp lệ.", "Submission ID is invalid."),
            ["Review.Loaded"] = new("Đã tải điểm AI. Giảng viên có thể chỉnh từng tiêu chí.", "AI score loaded. The teacher can adjust each criterion."),
            ["Review.LoadFirst"] = new("Hãy tải kết quả AI trước.", "Load the AI result first."),
            ["Review.SaveSuccess"] = new("Đã lưu điểm giảng viên. Điểm này chưa chính thức cho tới khi finalize.", "Teacher score saved. It is not official until finalized."),
            ["Review.FinalizeFirst"] = new("Hãy tải và chấm lại bài trước.", "Load and re-grade the submission first."),
            ["Review.FinalizeSuccess"] = new("Đã finalize. Điểm giảng viên là điểm chính thức.", "Finalized. Teacher score is the official score."),
            ["Message.EnterSubmission"] = new("Nhập Submission ID để bắt đầu review.", "Enter a Submission ID to start reviewing."),
            ["Message.LoginRequired"] = new("Hãy login trước.", "Please log in first."),
            ["Login.Subtitle"] = new("Đăng nhập hệ thống chấm bài", "Login to Paper Grading System"),
            ["Login.Email"] = new("Email", "Email"),
            ["Login.Password"] = new("Mật khẩu", "Password"),
            ["Login.SeedUsers"] = new("Tài khoản mẫu: admin@fptu.edu.vn / Admin@123, teacher.swt@fptu.edu.vn / Teacher@123", "Seed users: admin@fptu.edu.vn / Admin@123, teacher.swt@fptu.edu.vn / Teacher@123"),
            ["Login.Button"] = new("Đăng nhập", "Login"),
            ["Login.Required"] = new("Nhập email và mật khẩu trước nha bro.", "Enter email and password first."),
            ["Login.InProgress"] = new("Đang đăng nhập...", "Logging in..."),
            ["Login.Invalid"] = new("Sai email hoặc mật khẩu.", "Invalid email or password."),
            ["Login.EmptyResponse"] = new("Identity API trả về dữ liệu rỗng.", "Identity API returned an empty response."),
            ["Login.SuccessFormat"] = new("Đã đăng nhập: {0} ({1}).", "Signed in: {0} ({1})."),
            ["Assignments.Title"] = new("Phân công chấm bài", "Grading Assignments"),
            ["Assignments.Description"] = new("Phân bài cho giảng viên theo môn phụ trách và khối lượng công việc.", "Assign submissions by subject and workload to teachers."),
            ["Assignments.Unassigned"] = new("Chưa phân công", "Unassigned"),
            ["Assignments.Assigned"] = new("Đã phân công", "Assigned"),
            ["Assignments.Completed"] = new("Đã hoàn thành", "Completed"),
            ["Assignments.Management"] = new("Quản lý phân công", "Assignment management"),
            ["Assignments.ManagementDescription"] = new("Chọn batch bài thi, lọc giảng viên theo môn rồi chia đều hoặc phân công thủ công.", "Select an exam batch, filter teachers by subject, then distribute submissions equally or assign them manually."),
            ["Assignments.Create"] = new("Tạo phân công", "Create assignment"),
            ["TeacherAssignments.Description"] = new("Chỉ những bài được phân cho giảng viên này mới xuất hiện tại đây.", "Only submissions assigned to this teacher are shown here."),
            ["TeacherAssignments.Waiting"] = new("Đang chờ", "Waiting"),
            ["TeacherAssignments.InProgress"] = new("Đang chấm", "In progress"),
            ["TeacherAssignments.List"] = new("Danh sách bài được giao", "Assigned submissions"),
            ["TeacherAssignments.ListDescription"] = new("Chọn một bài được giao và mở khu vực chấm để kiểm tra điểm AI.", "Choose an assigned submission and open the review workspace to verify the AI score."),
            ["TeacherAssignments.OpenReview"] = new("Mở màn hình chấm", "Open review workspace"),
            ["Users.Description"] = new("Tạo và quản lý tài khoản Admin và giảng viên.", "Create and manage Admin and Teacher accounts."),
            ["Users.Refresh"] = new("Làm mới", "Refresh"),
            ["Users.FullName"] = new("Họ tên", "Full name"),
            ["Users.Role"] = new("Vai trò", "Role"),
            ["Users.Subject"] = new("Môn", "Subject"),
            ["Users.Status"] = new("Trạng thái", "Status"),
            ["Users.Active"] = new("Hoạt động", "Active"),
            ["Users.LastLogin"] = new("Đăng nhập gần nhất", "Last login"),
            ["Users.ToggleStatus"] = new("Khóa / Mở khóa", "Enable / Disable"),
            ["Users.NewPassword"] = new("Mật khẩu mới", "New password"),
            ["Users.ResetPassword"] = new("Đặt lại mật khẩu", "Reset password"),
            ["Users.SelectHint"] = new("Chọn một tài khoản trước khi đổi trạng thái.", "Select one account before changing its status."),
            ["Users.CreateAccount"] = new("Tạo tài khoản", "Create account"),
            ["Users.SubjectCode"] = new("Mã môn phụ trách", "Subject code"),
            ["Users.CreateUser"] = new("Tạo người dùng", "Create user"),
            ["Users.ManagedByIdentity"] = new("Dữ liệu tài khoản được quản lý bởi Identity Service.", "User data is managed by Identity Service."),
            ["Users.Teacher"] = new("Giảng viên", "Teacher"),
            ["Users.Admin"] = new("Quản trị", "Admin"),
            ["Users.LoadedFormat"] = new("Đã tải {0} tài khoản.", "Loaded {0} accounts."),
            ["Users.Created"] = new("Đã tạo tài khoản mới.", "User account created."),
            ["Users.StatusUpdated"] = new("Đã cập nhật trạng thái tài khoản.", "User status updated."),
            ["Users.Required"] = new("Nhập đủ họ tên, email và mật khẩu tối thiểu 8 ký tự.", "Enter full name, email, and a password of at least 8 characters."),
            ["Users.SubjectRequired"] = new("Teacher phải có mã môn phụ trách.", "A teacher must have a subject code."),
            ["Users.SelectUser"] = new("Chọn một tài khoản trước nha bro.", "Select an account first."),
            ["Users.CannotDisableSelf"] = new("Không thể tự khóa tài khoản Admin đang đăng nhập.", "You cannot disable the currently signed-in admin account."),
            ["Users.PasswordReset"] = new("Đã đặt lại mật khẩu.", "Password reset successfully."),
            ["Users.PasswordRequired"] = new("Mật khẩu mới phải có ít nhất 8 ký tự.", "The new password must contain at least 8 characters."),
            ["Grid.Criterion"] = new("Tiêu chí", "Criterion"),
            ["Grid.Max"] = new("Tối đa", "Max"),
            ["Grid.AI"] = new("AI", "AI"),
            ["Grid.Teacher"] = new("Giảng viên", "Teacher"),
            ["Grid.TeacherFeedback"] = new("Feedback giảng viên", "Teacher feedback"),
            ["Grid.MaxScore"] = new("Điểm tối đa", "Max score"),
            ["Grid.Description"] = new("Mô tả", "Description"),
            ["Score.NotGraded"] = new("Chưa chấm", "Not graded"),
            ["Score.NotFinalized"] = new("Chưa finalize", "Not finalized"),
            ["Score.ApiPrefix"] = new("API", "API"),
            ["Common.Dash"] = new("—", "—")
        };

    public MainWindow()
    {
        InitializeComponent();
        CriteriaGrid.ItemsSource = _criteria;
        UsersGrid.ItemsSource = _users;
        ExamListGrid.ItemsSource = _exams;
        RubricCriteriaGrid.ItemsSource = _rubricCriteria;
        InitializeLocalization();
        LanguageComboBox.SelectedIndex = 0;
    }

    private void InitializeLocalization()
    {
        RegisterLocalizedObjects(this);
        RegisterColumn(CriteriaGrid.Columns[0], "Grid.Criterion");
        RegisterColumn(CriteriaGrid.Columns[1], "Grid.Max");
        RegisterColumn(CriteriaGrid.Columns[2], "Grid.AI");
        RegisterColumn(CriteriaGrid.Columns[3], "Grid.Teacher");
        RegisterColumn(CriteriaGrid.Columns[4], "Grid.TeacherFeedback");
        RegisterColumn(ExamListGrid.Columns[0], "Exams.Code");
        RegisterColumn(ExamListGrid.Columns[1], "Exams.Name");
        RegisterColumn(ExamListGrid.Columns[2], "Users.Status");
        RegisterColumn(RubricCriteriaGrid.Columns[1], "Grid.Criterion");
        RegisterColumn(RubricCriteriaGrid.Columns[2], "Grid.MaxScore");
        RegisterColumn(RubricCriteriaGrid.Columns[3], "Grid.Description");
        RegisterColumn(RubricCriteriaGrid.Columns[4], "Exams.AiInstructions");
        RegisterColumn(UsersGrid.Columns[0], "Users.FullName");
        RegisterColumn(UsersGrid.Columns[1], "Login.Email");
        RegisterColumn(UsersGrid.Columns[2], "Users.Role");
        RegisterColumn(UsersGrid.Columns[3], "Users.Subject");
        RegisterColumn(UsersGrid.Columns[4], "Users.Active");
        RegisterColumn(UsersGrid.Columns[5], "Users.LastLogin");
    }

    private void RegisterLocalizedObjects(DependencyObject root)
    {
        if (root is TextBlock textBlock &&
            InitialTextKeys.TryGetValue(textBlock.Text, out var textKey))
        {
            _localizedObjects[textBlock] = textKey;
        }
        else if (root is ContentControl contentControl &&
            contentControl.Content is string content &&
            InitialTextKeys.TryGetValue(content, out textKey))
        {
            _localizedObjects[contentControl] = textKey;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject dependencyObject)
            {
                RegisterLocalizedObjects(dependencyObject);
            }
        }
    }

    private void RegisterColumn(DataGridColumn column, string textKey) =>
        _localizedColumns[column] = textKey;

    private void ApplyLanguage()
    {
        foreach (var (target, textKey) in _localizedObjects)
        {
            switch (target)
            {
                case TextBlock textBlock:
                    textBlock.Text = T(textKey);
                    break;
                case ContentControl contentControl:
                    contentControl.Content = T(textKey);
                    break;
            }
        }

        foreach (var (column, textKey) in _localizedColumns)
        {
            column.Header = T(textKey);
        }

        RenderCurrentUser();
        if (_currentScore is not null)
        {
            RenderScore(_currentScore);
        }
        else
        {
            AiCredentialSourceTextBlock.Text = $"{T("Score.ApiPrefix")}: {T("Common.Dash")}";
            StatusTextBlock.Text = T("Status.NotLoaded");
        }

        UpdateRubricHeader();
    }

    private string T(string key)
    {
        if (!Texts.TryGetValue(key, out var text))
        {
            return key;
        }

        return string.Equals(_language, EnglishLanguage, StringComparison.OrdinalIgnoreCase)
            ? text.En
            : text.Vi;
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string language)
        {
            return;
        }

        _language = language;
        ApplyLanguage();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ShowLogin();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LoginEmailTextBox.Text) ||
            string.IsNullOrWhiteSpace(LoginPasswordBox.Password))
        {
            ShowLoginMessage(T("Login.Required"));
            return;
        }

        try
        {
            LoginView.IsEnabled = false;
            ShowLoginMessage(T("Login.InProgress"), isError: false);

            var response = await _httpClient.PostAsJsonAsync(
                "auth/login",
                new LoginRequest(
                    LoginEmailTextBox.Text.Trim(),
                    LoginPasswordBox.Password));

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ShowLoginMessage(T("Login.Invalid"));
                return;
            }

            await EnsureSuccessAsync(response);
            _currentUser = await response.Content.ReadFromJsonAsync<LoginResponse>()
                ?? throw new InvalidOperationException(T("Login.EmptyResponse"));

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _currentUser.AccessToken);

            RenderCurrentUser();
            LoginView.Visibility = Visibility.Collapsed;
            AppShell.Visibility = Visibility.Visible;
            AdminNavigationShell.Visibility = IsAdmin
                ? Visibility.Visible
                : Visibility.Collapsed;
            TeacherNavigationShell.Visibility = IsTeacher
                ? Visibility.Visible
                : Visibility.Collapsed;
            ShowView(IsAdmin ? "Dashboard" : "TeacherDashboard");
            ResetReviewScreen();
            if (IsTeacher)
            {
                await LoadCredentialStatusAsync();
            }
            ShowMessage(string.Format(
                T("Login.SuccessFormat"),
                _currentUser.FullName,
                _currentUser.Role));
        }
        catch (Exception exception)
        {
            ShowLoginMessage(exception.Message);
        }
        finally
        {
            LoginView.IsEnabled = true;
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        _currentUser = null;
        _currentScore = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        RenderCurrentUser();
        LoginPasswordBox.Clear();
        ResetReviewScreen();
        ShowLogin();
    }

    private void ShowLogin()
    {
        AppShell.Visibility = Visibility.Collapsed;
        LoginView.Visibility = Visibility.Visible;
        LoginEmailTextBox.Focus();
    }

    private Guid CurrentUserId =>
        _currentUser?.UserId ??
        throw new InvalidOperationException(T("Message.LoginRequired"));

    private void RenderCurrentUser()
    {
        var userText = _currentUser is null
            ? T("User.NotSignedIn")
            : $"{_currentUser.FullName}\n{_currentUser.Email}\n{T("User.Role")}: {_currentUser.Role}";
        CurrentUserTextBlock.Text = userText;
        TeacherCurrentUserTextBlock.Text = userText;
    }

    private bool IsAdmin => string.Equals(
        _currentUser?.Role,
        UserRole.Admin.ToString(),
        StringComparison.OrdinalIgnoreCase);

    private bool IsTeacher => string.Equals(
        _currentUser?.Role,
        UserRole.Teacher.ToString(),
        StringComparison.OrdinalIgnoreCase);

    private void ShowLoginMessage(string message, bool isError = true)
    {
        LoginMessageTextBlock.Text = message;
        LoginMessageTextBlock.Foreground = isError
            ? Brushes.Firebrick
            : Brushes.DimGray;
    }

    private async void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string viewName })
        {
            ShowView(viewName);
            if (viewName == "UserManagement" && IsAdmin)
            {
                await LoadUsersAsync();
            }
            else if (viewName == "ExamsRubrics" && IsAdmin)
            {
                await LoadExamsAsync();
            }
        }
    }

    private void ShowView(string viewName)
    {
        var allowed = IsAdmin
            ? viewName is "Dashboard" or "UserManagement" or "ExamsRubrics" or "UploadBatch" or "Assignments" or "Reports"
            : IsTeacher && (viewName is "TeacherDashboard" or "ReviewScores");

        if (!allowed)
        {
            return;
        }

        DashboardView.Visibility = viewName == "Dashboard"
            ? Visibility.Visible
            : Visibility.Collapsed;
        UserManagementView.Visibility = viewName == "UserManagement"
            ? Visibility.Visible
            : Visibility.Collapsed;
        ExamsRubricsView.Visibility = viewName == "ExamsRubrics"
            ? Visibility.Visible
            : Visibility.Collapsed;
        UploadBatchView.Visibility = viewName == "UploadBatch"
            ? Visibility.Visible
            : Visibility.Collapsed;
        AssignmentsView.Visibility = viewName == "Assignments"
            ? Visibility.Visible
            : Visibility.Collapsed;
        TeacherDashboardView.Visibility = viewName == "TeacherDashboard"
            ? Visibility.Visible
            : Visibility.Collapsed;
        ReviewScoresView.Visibility = viewName == "ReviewScores"
            ? Visibility.Visible
            : Visibility.Collapsed;
        ReportsView.Visibility = viewName == "Reports"
            ? Visibility.Visible
            : Visibility.Collapsed;

        var activeButton = viewName switch
        {
            "Dashboard" => DashboardButton,
            "UserManagement" => UsersButton,
            "ExamsRubrics" => ExamsRubricsButton,
            "UploadBatch" => UploadBatchButton,
            "Assignments" => AssignmentsButton,
            "TeacherDashboard" => TeacherDashboardButton,
            "ReviewScores" => ReviewScoresButton,
            "Reports" => ReportsButton,
            _ => ReviewScoresButton
        };

        foreach (var button in new[]
        {
            DashboardButton,
            UsersButton,
            ExamsRubricsButton,
            UploadBatchButton,
            AssignmentsButton,
            TeacherDashboardButton,
            ReviewScoresButton,
            ReportsButton
        })
        {
            var isActive = ReferenceEquals(button, activeButton);
            button.Background = isActive
                ? ActiveNavigationBackground
                : InactiveNavigationBackground;
            button.Foreground = isActive
                ? ActiveNavigationForeground
                : InactiveNavigationForeground;
        }
    }

    private async void RefreshExams_Click(object sender, RoutedEventArgs e) =>
        await LoadExamsAsync();

    private async Task LoadExamsAsync()
    {
        if (!IsAdmin)
        {
            return;
        }

        await ExecuteExamRubricAsync(async () =>
        {
            await LoadExamsCoreAsync(_selectedExam?.ExamId);
            ShowExamRubricMessage(string.Format(T("Exams.LoadedFormat"), _exams.Count));
        });
    }

    private async Task LoadExamsCoreAsync(Guid? preferredExamId = null)
    {
        var exams = await _httpClient.GetFromJsonAsync<List<ExamSummaryResponse>>(
            "exams/") ?? [];

        _suppressExamSelection = true;
        try
        {
            _exams.Clear();
            foreach (var exam in exams)
            {
                _exams.Add(exam);
            }

            var selected = preferredExamId.HasValue
                ? _exams.FirstOrDefault(x => x.ExamId == preferredExamId.Value)
                : _exams.FirstOrDefault();
            ExamListGrid.SelectedItem = selected;
            _selectedExam = selected;
        }
        finally
        {
            _suppressExamSelection = false;
        }

        if (_selectedExam is not null)
        {
            await LoadSelectedRubricCoreAsync();
        }
        else
        {
            _rubricCriteria.Clear();
            UpdateRubricHeader();
        }
    }

    private void BrowseExamFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = T("Exams.File"),
            Filter = "Exam documents (*.docx;*.pdf)|*.docx;*.pdf|Word documents (*.docx)|*.docx|PDF documents (*.pdf)|*.pdf",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            ExamFilePathTextBox.Text = dialog.FileName;
        }
    }

    private async void ImportExam_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ExamCodeTextBox.Text) ||
            string.IsNullOrWhiteSpace(ExamNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(ExamSubjectTextBox.Text) ||
            string.IsNullOrWhiteSpace(ExamSemesterTextBox.Text) ||
            string.IsNullOrWhiteSpace(ExamFilePathTextBox.Text) ||
            !File.Exists(ExamFilePathTextBox.Text))
        {
            ShowExamRubricMessage(T("Exams.ImportRequired"), isError: true);
            return;
        }

        await ExecuteExamRubricAsync(async () =>
        {
            await using var fileStream = File.OpenRead(ExamFilePathTextBox.Text);
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(ExamCodeTextBox.Text.Trim()), "code");
            form.Add(new StringContent(ExamNameTextBox.Text.Trim()), "name");
            form.Add(new StringContent(ExamSubjectTextBox.Text.Trim()), "subjectCode");
            form.Add(new StringContent(ExamSemesterTextBox.Text.Trim()), "semester");
            form.Add(new StringContent(CurrentUserId.ToString()), "createdByAdminId");

            using var fileContent = new StreamContent(fileStream);
            var extension = Path.GetExtension(ExamFilePathTextBox.Text);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                    ? "application/pdf"
                    : "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            form.Add(fileContent, "file", Path.GetFileName(ExamFilePathTextBox.Text));

            var response = await _httpClient.PostAsync("exams/import", form);
            await EnsureSuccessAsync(response);
            var importedExam = await response.Content.ReadFromJsonAsync<ExamSummaryResponse>()
                ?? throw new InvalidOperationException(T("Login.EmptyResponse"));

            ExamCodeTextBox.Clear();
            ExamNameTextBox.Clear();
            ExamFilePathTextBox.Clear();
            await LoadExamsCoreAsync(importedExam.ExamId);
            ShowExamRubricMessage(T("Exams.Imported"));
        });
    }

    private async void ExamList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressExamSelection ||
            ExamListGrid.SelectedItem is not ExamSummaryResponse exam)
        {
            return;
        }

        _selectedExam = exam;
        await ExecuteExamRubricAsync(LoadSelectedRubricCoreAsync);
    }

    private async Task LoadSelectedRubricCoreAsync()
    {
        if (_selectedExam is null)
        {
            return;
        }

        var rubric = await _httpClient.GetFromJsonAsync<ExamRubricResponse>(
            $"exams/{_selectedExam.ExamId}/rubric")
            ?? throw new InvalidOperationException(T("Login.EmptyResponse"));
        RenderRubric(rubric);
    }

    private void AddRubricCriterion_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedExam is null)
        {
            ShowExamRubricMessage(T("Exams.SelectFirst"), isError: true);
            return;
        }

        var row = new RubricCriterionEditorRow
        {
            DisplayOrder = _rubricCriteria.Count + 1,
            MaxScore = 1m
        };
        _rubricCriteria.Add(row);
        RubricCriteriaGrid.SelectedItem = row;
        RubricCriteriaGrid.ScrollIntoView(row);
        UpdateRubricHeader();
    }

    private void RemoveRubricCriterion_Click(object sender, RoutedEventArgs e)
    {
        if (RubricCriteriaGrid.SelectedItem is not RubricCriterionEditorRow row)
        {
            return;
        }

        _rubricCriteria.Remove(row);
        NormalizeRubricOrder();
        UpdateRubricHeader();
    }

    private async void SaveRubric_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteExamRubricAsync(async () =>
        {
            await SaveRubricCoreAsync();
            ShowExamRubricMessage(T("Exams.Saved"));
        });
    }

    private async void PublishRubric_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteExamRubricAsync(async () =>
        {
            await SaveRubricCoreAsync();
            var response = await _httpClient.PostAsync(
                $"exams/{_selectedExam!.ExamId}/rubric/publish",
                null);
            await EnsureSuccessAsync(response);
            var rubric = await response.Content.ReadFromJsonAsync<ExamRubricResponse>()
                ?? throw new InvalidOperationException(T("Login.EmptyResponse"));
            RenderRubric(rubric);
            UpdateSelectedExamSummary(rubric);
            ShowExamRubricMessage(T("Exams.Published"));
        });
    }

    private async Task SaveRubricCoreAsync()
    {
        if (_selectedExam is null)
        {
            throw new InvalidOperationException(T("Exams.SelectFirst"));
        }

        RubricCriteriaGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        RubricCriteriaGrid.CommitEdit(DataGridEditingUnit.Row, true);
        NormalizeRubricOrder();

        if (_rubricCriteria.Count == 0 || _rubricCriteria.Any(x =>
            string.IsNullOrWhiteSpace(x.Name) ||
            string.IsNullOrWhiteSpace(x.Description) ||
            string.IsNullOrWhiteSpace(x.AiInstructions) ||
            x.MaxScore <= 0))
        {
            throw new InvalidOperationException(T("Exams.CriterionRequired"));
        }

        var request = new SaveExamRubricRequest(
            _rubricCriteria.Select(x => new UpsertRubricCriterionRequest(
                x.CriterionId,
                x.Name,
                x.Description,
                x.AiInstructions,
                x.MaxScore,
                x.DisplayOrder)).ToArray());
        var response = await _httpClient.PutAsJsonAsync(
            $"exams/{_selectedExam.ExamId}/rubric",
            request);
        await EnsureSuccessAsync(response);
        var rubric = await response.Content.ReadFromJsonAsync<ExamRubricResponse>()
            ?? throw new InvalidOperationException(T("Login.EmptyResponse"));
        RenderRubric(rubric);
        UpdateSelectedExamSummary(rubric);
    }

    private void RubricCriteriaGrid_CellEditEnding(
        object sender,
        DataGridCellEditEndingEventArgs e) =>
        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => UpdateRubricHeader()));

    private void RenderRubric(ExamRubricResponse rubric)
    {
        _rubricCriteria.Clear();
        foreach (var criterion in rubric.Criteria.OrderBy(x => x.DisplayOrder))
        {
            _rubricCriteria.Add(new RubricCriterionEditorRow
            {
                CriterionId = criterion.CriterionId,
                Name = criterion.Name,
                Description = criterion.Description,
                AiInstructions = criterion.AiInstructions,
                MaxScore = criterion.MaxScore,
                DisplayOrder = criterion.DisplayOrder
            });
        }

        UpdateRubricHeader(rubric.Status);
    }

    private void UpdateSelectedExamSummary(ExamRubricResponse rubric)
    {
        if (_selectedExam is null)
        {
            return;
        }

        var index = _exams.IndexOf(_selectedExam);
        var updated = _selectedExam with
        {
            RubricStatus = rubric.Status,
            CriterionCount = rubric.Criteria.Count,
            TotalScore = rubric.TotalScore,
            PublishedAtUtc = rubric.PublishedAtUtc
        };

        _suppressExamSelection = true;
        try
        {
            if (index >= 0)
            {
                _exams[index] = updated;
            }
            _selectedExam = updated;
            ExamListGrid.SelectedItem = updated;
        }
        finally
        {
            _suppressExamSelection = false;
        }
        UpdateRubricHeader(rubric.Status);
    }

    private void NormalizeRubricOrder()
    {
        for (var index = 0; index < _rubricCriteria.Count; index++)
        {
            _rubricCriteria[index].DisplayOrder = index + 1;
        }
        RubricCriteriaGrid.Items.Refresh();
    }

    private void UpdateRubricHeader(RubricStatus? status = null)
    {
        var total = _rubricCriteria.Sum(x => x.MaxScore);
        RubricTotalTextBlock.Text = string.Format(T("Exams.TotalFormat"), total);

        if (_selectedExam is null)
        {
            SelectedExamTextBlock.Text = T("Exams.SelectHint");
            return;
        }

        var currentStatus = status ?? _selectedExam.RubricStatus;
        var statusText = currentStatus == RubricStatus.Published
            ? T("Exams.StatusPublished")
            : T("Exams.StatusDraft");
        SelectedExamTextBlock.Text = string.Format(
            T("Exams.SelectedFormat"),
            _selectedExam.Code,
            _selectedExam.Name,
            statusText);
    }

    private async Task ExecuteExamRubricAsync(Func<Task> action)
    {
        try
        {
            ExamsRubricsView.IsEnabled = false;
            await action();
        }
        catch (Exception exception)
        {
            ShowExamRubricMessage(exception.Message, isError: true);
        }
        finally
        {
            ExamsRubricsView.IsEnabled = true;
        }
    }

    private void ShowExamRubricMessage(string message, bool isError = false)
    {
        ExamRubricMessageTextBlock.Text = message;
        ExamRubricMessageTextBlock.Foreground = isError
            ? Brushes.Firebrick
            : Brushes.DimGray;
    }

    private async void RefreshUsers_Click(object sender, RoutedEventArgs e) =>
        await LoadUsersAsync();

    private async Task LoadUsersAsync()
    {
        if (!IsAdmin)
        {
            return;
        }

        await ExecuteUserManagementAsync(async () =>
        {
            var users = await _httpClient.GetFromJsonAsync<List<UserAccountResponse>>(
                "users/") ?? [];

            _users.Clear();
            foreach (var user in users)
            {
                _users.Add(user);
            }

            ShowUserManagementMessage(string.Format(T("Users.LoadedFormat"), _users.Count));
        });
    }

    private async void CreateUser_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewUserFullNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(NewUserEmailTextBox.Text) ||
            NewUserPasswordBox.Password.Length < 8)
        {
            ShowUserManagementMessage(T("Users.Required"), isError: true);
            return;
        }

        var role = GetSelectedNewUserRole();
        if (role == UserRole.Teacher &&
            string.IsNullOrWhiteSpace(NewUserSubjectTextBox.Text))
        {
            ShowUserManagementMessage(T("Users.SubjectRequired"), isError: true);
            return;
        }

        await ExecuteUserManagementAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(
                "users/",
                new CreateUserRequest(
                    NewUserEmailTextBox.Text.Trim(),
                    NewUserFullNameTextBox.Text.Trim(),
                    NewUserPasswordBox.Password,
                    role,
                    role == UserRole.Teacher
                        ? NewUserSubjectTextBox.Text.Trim().ToUpperInvariant()
                        : null));
            await EnsureSuccessAsync(response);

            NewUserFullNameTextBox.Clear();
            NewUserEmailTextBox.Clear();
            NewUserPasswordBox.Clear();
            NewUserSubjectTextBox.Clear();
            NewUserRoleComboBox.SelectedIndex = 0;
            await LoadUsersCoreAsync();
            ShowUserManagementMessage(T("Users.Created"));
        });
    }

    private async void ToggleUserStatus_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not UserAccountResponse selectedUser)
        {
            ShowUserManagementMessage(T("Users.SelectUser"), isError: true);
            return;
        }

        if (selectedUser.UserId == CurrentUserId && selectedUser.IsActive)
        {
            ShowUserManagementMessage(T("Users.CannotDisableSelf"), isError: true);
            return;
        }

        await ExecuteUserManagementAsync(async () =>
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"users/{selectedUser.UserId}/status",
                new SetUserStatusRequest(!selectedUser.IsActive));
            await EnsureSuccessAsync(response);
            await LoadUsersCoreAsync();
            ShowUserManagementMessage(T("Users.StatusUpdated"));
        });
    }

    private async void ResetUserPassword_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not UserAccountResponse selectedUser)
        {
            ShowUserManagementMessage(T("Users.SelectUser"), isError: true);
            return;
        }

        if (ResetUserPasswordBox.Password.Length < 8)
        {
            ShowUserManagementMessage(T("Users.PasswordRequired"), isError: true);
            return;
        }

        await ExecuteUserManagementAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"users/{selectedUser.UserId}/reset-password",
                new ResetUserPasswordRequest(ResetUserPasswordBox.Password));
            await EnsureSuccessAsync(response);
            ResetUserPasswordBox.Clear();
            ShowUserManagementMessage(T("Users.PasswordReset"));
        });
    }

    private void NewUserRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NewUserSubjectLabel is null || NewUserSubjectTextBox is null)
        {
            return;
        }

        var visibility = GetSelectedNewUserRole() == UserRole.Teacher
            ? Visibility.Visible
            : Visibility.Collapsed;
        NewUserSubjectLabel.Visibility = visibility;
        NewUserSubjectTextBox.Visibility = visibility;
    }

    private UserRole GetSelectedNewUserRole()
    {
        if (NewUserRoleComboBox.SelectedItem is ComboBoxItem { Tag: string role } &&
            Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsedRole))
        {
            return parsedRole;
        }

        return UserRole.Teacher;
    }

    private async Task LoadUsersCoreAsync()
    {
        var users = await _httpClient.GetFromJsonAsync<List<UserAccountResponse>>(
            "users/") ?? [];
        _users.Clear();
        foreach (var user in users)
        {
            _users.Add(user);
        }
    }

    private async Task ExecuteUserManagementAsync(Func<Task> action)
    {
        try
        {
            UserManagementView.IsEnabled = false;
            await action();
        }
        catch (Exception exception)
        {
            ShowUserManagementMessage(exception.Message, isError: true);
        }
        finally
        {
            UserManagementView.IsEnabled = true;
        }
    }

    private void ShowUserManagementMessage(string message, bool isError = false)
    {
        UserManagementMessageTextBlock.Text = message;
        UserManagementMessageTextBlock.Foreground = isError
            ? Brushes.Firebrick
            : Brushes.DimGray;
    }

    private async void SaveApiKey_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PersonalApiKeyPasswordBox.Password))
        {
            ShowMessage(T("Credential.Required"), isError: true);
            return;
        }

        await ExecuteAsync(async () =>
        {
            var request = new SaveAiCredentialRequest(
                "Gemini",
                PersonalApiKeyPasswordBox.Password,
                AllowSystemFallbackCheckBox.IsChecked == true);
            var response = await _httpClient.PutAsJsonAsync(
                $"grading/credentials/{CurrentUserId}",
                request);
            await EnsureSuccessAsync(response);

            var status = await response.Content
                .ReadFromJsonAsync<AiCredentialStatusResponse>()
                ?? throw new InvalidOperationException(T("Login.EmptyResponse"));

            PersonalApiKeyPasswordBox.Clear();
            RenderCredentialStatus(status);
            ShowMessage(T("Credential.SavedMessage"));
        });
    }

    private async void TestApiKey_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsync(
                $"grading/credentials/{CurrentUserId}/test?provider=Gemini",
                null);
            await EnsureSuccessAsync(response);

            var result = await response.Content
                .ReadFromJsonAsync<AiCredentialValidationResponse>()
                ?? throw new InvalidOperationException(T("Login.EmptyResponse"));

            ShowMessage(
                result.IsValid
                    ? T("Credential.Valid")
                    : result.Message,
                isError: !result.IsValid);
            await LoadCredentialStatusAsync();
        });
    }

    private async void DeleteApiKey_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync(async () =>
        {
            var response = await _httpClient.DeleteAsync(
                $"grading/credentials/{CurrentUserId}?provider=Gemini");
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                await EnsureSuccessAsync(response);
            }

            PersonalApiKeyPasswordBox.Clear();
            await LoadCredentialStatusAsync();
            ShowMessage(T("Credential.Deleted"));
        });
    }

    private async void LoadScore_Click(object sender, RoutedEventArgs e)
    {
        if (!Guid.TryParse(SubmissionIdTextBox.Text, out var submissionId) ||
            submissionId == Guid.Empty)
        {
            ShowMessage(T("Review.InvalidSubmissionId"), isError: true);
            return;
        }

        await ExecuteAsync(async () =>
        {
            var response = await _httpClient.GetAsync(
                $"scores/submissions/{submissionId}");
            await EnsureSuccessAsync(response);

            _currentScore = await response.Content
                .ReadFromJsonAsync<ScoreComparisonResponse>()
                ?? throw new InvalidOperationException(T("Login.EmptyResponse"));

            RenderScore(_currentScore);
            ShowMessage(T("Review.Loaded"));
        });
    }

    private async void SaveTeacherGrade_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScore is null)
        {
            ShowMessage(T("Review.LoadFirst"), isError: true);
            return;
        }

        CriteriaGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        CriteriaGrid.CommitEdit(DataGridEditingUnit.Row, true);

        await ExecuteAsync(async () =>
        {
            var teacherScore = _criteria.Sum(x => x.TeacherScore);
            var request = new SubmitTeacherGradeRequest(
                CurrentUserId,
                teacherScore,
                TeacherFeedbackTextBox.Text,
                _criteria.Select(x => new TeacherCriterionGradeRequest(
                    x.CriterionId,
                    x.TeacherScore,
                    x.TeacherFeedback)).ToArray());

            var response = await _httpClient.PutAsJsonAsync(
                $"scores/submissions/{_currentScore.SubmissionId}/teacher-grade",
                request);
            await EnsureSuccessAsync(response);

            _currentScore = await response.Content
                .ReadFromJsonAsync<ScoreComparisonResponse>()
                ?? throw new InvalidOperationException(T("Login.EmptyResponse"));

            RenderScore(_currentScore);
            ShowMessage(T("Review.SaveSuccess"));
        });
    }

    private async void FinalizeScore_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScore is null)
        {
            ShowMessage(T("Review.FinalizeFirst"), isError: true);
            return;
        }

        await ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"scores/submissions/{_currentScore.SubmissionId}/finalize",
                new FinalizeScoreRequest(CurrentUserId));
            await EnsureSuccessAsync(response);

            _currentScore = await response.Content
                .ReadFromJsonAsync<ScoreComparisonResponse>()
                ?? throw new InvalidOperationException(T("Login.EmptyResponse"));

            RenderScore(_currentScore);
            ShowMessage(T("Review.FinalizeSuccess"));
        });
    }

    private void RenderScore(ScoreComparisonResponse score)
    {
        _criteria.Clear();
        foreach (var criterion in score.Criteria)
        {
            _criteria.Add(new CriterionReviewRow
            {
                CriterionId = criterion.CriterionId,
                CriterionName = criterion.CriterionName,
                MaxScore = criterion.MaxScore,
                AiScore = criterion.AiScore,
                TeacherScore = criterion.TeacherScore ?? criterion.AiScore,
                TeacherFeedback = criterion.TeacherFeedback ?? string.Empty
            });
        }

        AiScoreTextBlock.Text = $"{score.AiScore:0.##} / {score.MaxScore:0.##}";
        AiCredentialSourceTextBlock.Text = $"{T("Score.ApiPrefix")}: {score.AiCredentialSource}";
        TeacherScoreTextBlock.Text = score.TeacherScore.HasValue
            ? $"{score.TeacherScore:0.##} / {score.MaxScore:0.##}"
            : T("Score.NotGraded");
        DifferenceTextBlock.Text = score.Difference.HasValue
            ? $"{score.Difference:+0.##;-0.##;0}"
            : T("Common.Dash");
        FinalScoreTextBlock.Text = score.FinalScore.HasValue
            ? $"{score.FinalScore:0.##} / {score.MaxScore:0.##}"
            : T("Score.NotFinalized");
        AiFeedbackTextBox.Text = score.AiFeedback;
        TeacherFeedbackTextBox.Text = score.TeacherFeedback ?? string.Empty;
        StatusTextBlock.Text = score.Status.ToString();
    }

    private async Task LoadCredentialStatusAsync()
    {
        try
        {
            var status = await _httpClient.GetFromJsonAsync<AiCredentialStatusResponse>(
                $"grading/credentials/{CurrentUserId}?provider=Gemini");
            if (status is not null)
            {
                RenderCredentialStatus(status);
            }
        }
        catch (Exception exception)
        {
            ApiKeyStatusTextBlock.Text = T("Credential.BackendUnavailable");
            ShowMessage(exception.Message, isError: true);
        }
    }

    private void RenderCredentialStatus(AiCredentialStatusResponse status)
    {
        ApiKeyStatusTextBlock.Text = status.HasCredential
            ? string.Format(T("Credential.SavedFormat"), status.MaskedApiKey)
            : T("Credential.SystemKey");
        AllowSystemFallbackCheckBox.IsChecked = status.HasCredential
            ? status.AllowSystemFallback
            : true;
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        try
        {
            IsEnabled = false;
            await action();
        }
        catch (Exception exception)
        {
            ShowMessage(exception.Message, isError: true);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"API error {(int)response.StatusCode}: {error}");
    }

    private void ShowMessage(string message, bool isError = false)
    {
        MessageTextBlock.Text = message;
        MessageTextBlock.Foreground = isError
            ? System.Windows.Media.Brushes.Firebrick
            : System.Windows.Media.Brushes.DimGray;
    }

    private void ResetReviewScreen()
    {
        _criteria.Clear();
        _currentScore = null;
        SubmissionIdTextBox.Text = "00000000-0000-0000-0000-000000000000";
        PersonalApiKeyPasswordBox.Clear();
        AiScoreTextBlock.Text = T("Common.Dash");
        AiCredentialSourceTextBlock.Text = $"{T("Score.ApiPrefix")}: {T("Common.Dash")}";
        TeacherScoreTextBlock.Text = T("Common.Dash");
        DifferenceTextBlock.Text = T("Common.Dash");
        FinalScoreTextBlock.Text = T("Common.Dash");
        AiFeedbackTextBox.Clear();
        TeacherFeedbackTextBox.Clear();
        StatusTextBlock.Text = T("Status.NotLoaded");
        ApiKeyStatusTextBlock.Text = T("Credential.Checking");
        ShowMessage(T("Message.EnterSubmission"));
    }

    private readonly record struct LocalizedText(string Vi, string En);

    private sealed class CriterionReviewRow
    {
        public Guid CriterionId { get; init; }
        public string CriterionName { get; init; } = string.Empty;
        public decimal MaxScore { get; init; }
        public decimal AiScore { get; init; }
        public decimal TeacherScore { get; set; }
        public string TeacherFeedback { get; set; } = string.Empty;
    }

    private sealed class RubricCriterionEditorRow
    {
        public Guid? CriterionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AiInstructions { get; set; } = string.Empty;
        public decimal MaxScore { get; set; }
        public int DisplayOrder { get; set; }
    }
}
