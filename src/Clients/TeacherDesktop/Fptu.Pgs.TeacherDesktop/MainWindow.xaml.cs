using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fptu.Pgs.Contracts;

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
    private readonly Dictionary<object, string> _localizedObjects = [];
    private readonly Dictionary<DataGridColumn, string> _localizedColumns = [];
    private ScoreComparisonResponse? _currentScore;
    private LoginResponse? _currentUser;
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
            ["Teacher Desktop"] = "App.TeacherDesktop",
            ["Not signed in"] = "User.NotSignedIn",
            ["Language"] = "App.Language",
            ["Dashboard"] = "Nav.Dashboard",
            ["Exams & Rubrics"] = "Nav.ExamsRubrics",
            ["Exams &amp; Rubrics"] = "Nav.ExamsRubrics",
            ["Upload Batch"] = "Nav.UploadBatch",
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
            ["Login"] = "Login.Button"
        };

    private static readonly IReadOnlyDictionary<string, LocalizedText> Texts =
        new Dictionary<string, LocalizedText>(StringComparer.Ordinal)
        {
            ["App.TeacherDesktop"] = new("Ứng dụng giảng viên", "Teacher Desktop"),
            ["App.Language"] = new("Ngôn ngữ", "Language"),
            ["User.NotSignedIn"] = new("Chưa đăng nhập", "Not signed in"),
            ["User.Role"] = new("Vai trò", "Role"),
            ["Nav.Dashboard"] = new("Tổng quan", "Dashboard"),
            ["Nav.ExamsRubrics"] = new("Đề thi & Rubric", "Exams & Rubrics"),
            ["Nav.UploadBatch"] = new("Upload bài thi", "Upload Batch"),
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
        InitializeLocalization();
        LanguageComboBox.SelectedIndex = 0;
        ShowView("ReviewScores");
    }

    private void InitializeLocalization()
    {
        RegisterLocalizedObjects(this);
        RegisterColumn(CriteriaGrid.Columns[0], "Grid.Criterion");
        RegisterColumn(CriteriaGrid.Columns[1], "Grid.Max");
        RegisterColumn(CriteriaGrid.Columns[2], "Grid.AI");
        RegisterColumn(CriteriaGrid.Columns[3], "Grid.Teacher");
        RegisterColumn(CriteriaGrid.Columns[4], "Grid.TeacherFeedback");
        RegisterColumn(RubricCriteriaGrid.Columns[0], "Grid.Criterion");
        RegisterColumn(RubricCriteriaGrid.Columns[1], "Grid.MaxScore");
        RegisterColumn(RubricCriteriaGrid.Columns[2], "Grid.Description");
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
            ShowView(_currentUser.Role == UserRole.Admin.ToString()
                ? "Dashboard"
                : "ReviewScores");
            ResetReviewScreen();
            await LoadCredentialStatusAsync();
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
        CurrentUserTextBlock.Text = _currentUser is null
            ? T("User.NotSignedIn")
            : $"{_currentUser.FullName}\n{_currentUser.Email}\n{T("User.Role")}: {_currentUser.Role}";
    }

    private void ShowLoginMessage(string message, bool isError = true)
    {
        LoginMessageTextBlock.Text = message;
        LoginMessageTextBlock.Foreground = isError
            ? Brushes.Firebrick
            : Brushes.DimGray;
    }

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string viewName })
        {
            ShowView(viewName);
        }
    }

    private void ShowView(string viewName)
    {
        DashboardView.Visibility = viewName == "Dashboard"
            ? Visibility.Visible
            : Visibility.Collapsed;
        ExamsRubricsView.Visibility = viewName == "ExamsRubrics"
            ? Visibility.Visible
            : Visibility.Collapsed;
        UploadBatchView.Visibility = viewName == "UploadBatch"
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
            "ExamsRubrics" => ExamsRubricsButton,
            "UploadBatch" => UploadBatchButton,
            "ReviewScores" => ReviewScoresButton,
            "Reports" => ReportsButton,
            _ => ReviewScoresButton
        };

        foreach (var button in new[]
        {
            DashboardButton,
            ExamsRubricsButton,
            UploadBatchButton,
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
}
