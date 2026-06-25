using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.TeacherDesktop;

public partial class MainWindow : Window
{
    private static readonly Guid DemoTeacherId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5000/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly ObservableCollection<CriterionReviewRow> _criteria = [];
    private ScoreComparisonResponse? _currentScore;

    public MainWindow()
    {
        InitializeComponent();
        CriteriaGrid.ItemsSource = _criteria;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadCredentialStatusAsync();
    }

    private async void SaveApiKey_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PersonalApiKeyPasswordBox.Password))
        {
            ShowMessage("Nhập Gemini API key trước khi lưu.", isError: true);
            return;
        }

        await ExecuteAsync(async () =>
        {
            var request = new SaveAiCredentialRequest(
                "Gemini",
                PersonalApiKeyPasswordBox.Password,
                AllowSystemFallbackCheckBox.IsChecked == true);
            var response = await _httpClient.PutAsJsonAsync(
                $"grading/credentials/{DemoTeacherId}",
                request);
            await EnsureSuccessAsync(response);

            var status = await response.Content
                .ReadFromJsonAsync<AiCredentialStatusResponse>()
                ?? throw new InvalidOperationException("API trả về dữ liệu rỗng.");

            PersonalApiKeyPasswordBox.Clear();
            RenderCredentialStatus(status);
            ShowMessage("Đã mã hóa và lưu API key cá nhân trên backend.");
        });
    }

    private async void TestApiKey_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsync(
                $"grading/credentials/{DemoTeacherId}/test?provider=Gemini",
                null);
            await EnsureSuccessAsync(response);

            var result = await response.Content
                .ReadFromJsonAsync<AiCredentialValidationResponse>()
                ?? throw new InvalidOperationException("API trả về dữ liệu rỗng.");

            ShowMessage(
                result.IsValid
                    ? "Gemini API key cá nhân hoạt động."
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
                $"grading/credentials/{DemoTeacherId}?provider=Gemini");
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                await EnsureSuccessAsync(response);
            }

            PersonalApiKeyPasswordBox.Clear();
            await LoadCredentialStatusAsync();
            ShowMessage("Đã xóa API key cá nhân. Hệ thống sẽ dùng key chung.");
        });
    }

    private async void LoadScore_Click(object sender, RoutedEventArgs e)
    {
        if (!Guid.TryParse(SubmissionIdTextBox.Text, out var submissionId) ||
            submissionId == Guid.Empty)
        {
            ShowMessage("Submission ID không hợp lệ.", isError: true);
            return;
        }

        await ExecuteAsync(async () =>
        {
            var response = await _httpClient.GetAsync(
                $"scores/submissions/{submissionId}");
            await EnsureSuccessAsync(response);

            _currentScore = await response.Content
                .ReadFromJsonAsync<ScoreComparisonResponse>()
                ?? throw new InvalidOperationException("API trả về dữ liệu rỗng.");

            RenderScore(_currentScore);
            ShowMessage("Đã tải điểm AI. Teacher có thể chỉnh từng tiêu chí.");
        });
    }

    private async void SaveTeacherGrade_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScore is null)
        {
            ShowMessage("Hãy tải kết quả AI trước.", isError: true);
            return;
        }

        CriteriaGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        CriteriaGrid.CommitEdit(DataGridEditingUnit.Row, true);

        await ExecuteAsync(async () =>
        {
            var teacherScore = _criteria.Sum(x => x.TeacherScore);
            var request = new SubmitTeacherGradeRequest(
                DemoTeacherId,
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
                ?? throw new InvalidOperationException("API trả về dữ liệu rỗng.");

            RenderScore(_currentScore);
            ShowMessage("Đã lưu điểm Teacher. Điểm này chưa chính thức cho tới khi finalize.");
        });
    }

    private async void FinalizeScore_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScore is null)
        {
            ShowMessage("Hãy tải và chấm lại bài trước.", isError: true);
            return;
        }

        await ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"scores/submissions/{_currentScore.SubmissionId}/finalize",
                new FinalizeScoreRequest(DemoTeacherId));
            await EnsureSuccessAsync(response);

            _currentScore = await response.Content
                .ReadFromJsonAsync<ScoreComparisonResponse>()
                ?? throw new InvalidOperationException("API trả về dữ liệu rỗng.");

            RenderScore(_currentScore);
            ShowMessage("Đã finalize. Teacher Score là điểm chính thức.");
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
        AiCredentialSourceTextBlock.Text = $"API: {score.AiCredentialSource}";
        TeacherScoreTextBlock.Text = score.TeacherScore.HasValue
            ? $"{score.TeacherScore:0.##} / {score.MaxScore:0.##}"
            : "Chưa chấm";
        DifferenceTextBlock.Text = score.Difference.HasValue
            ? $"{score.Difference:+0.##;-0.##;0}"
            : "—";
        FinalScoreTextBlock.Text = score.FinalScore.HasValue
            ? $"{score.FinalScore:0.##} / {score.MaxScore:0.##}"
            : "Chưa finalize";
        AiFeedbackTextBox.Text = score.AiFeedback;
        TeacherFeedbackTextBox.Text = score.TeacherFeedback ?? string.Empty;
        StatusTextBlock.Text = score.Status.ToString();
    }

    private async Task LoadCredentialStatusAsync()
    {
        try
        {
            var status = await _httpClient.GetFromJsonAsync<AiCredentialStatusResponse>(
                $"grading/credentials/{DemoTeacherId}?provider=Gemini");
            if (status is not null)
            {
                RenderCredentialStatus(status);
            }
        }
        catch (Exception exception)
        {
            ApiKeyStatusTextBlock.Text = "Không kết nối được backend";
            ShowMessage(exception.Message, isError: true);
        }
    }

    private void RenderCredentialStatus(AiCredentialStatusResponse status)
    {
        ApiKeyStatusTextBlock.Text = status.HasCredential
            ? $"Đã lưu: {status.MaskedApiKey}"
            : "Đang dùng API key hệ thống";
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
