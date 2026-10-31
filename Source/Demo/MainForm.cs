using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using PortfolioSamples.Configuration;
using PortfolioSamples.Conversion;
using PortfolioSamples.Logging;
using PortfolioSamples.Mes;

namespace PortfolioSamples.Demo
{
    public partial class MainForm : Form
    {
        private readonly string _workspace;
        private readonly string _settingsPath;
        private readonly SafeLog _log;
        private readonly SampleSettings _settings;

        public MainForm()
        {
            InitializeComponent();
            _workspace = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PortfolioSamples");
            _settingsPath = Path.Combine(_workspace, "Settings", "settings.local.json");
            SettingsLoadResult loaded = SettingsStore.Load(_settingsPath);
            _settings = loaded.Settings;
            _log = new SafeLog(Path.Combine(_workspace, "Logs"), _settings.MaximumLogKilobytes);
            _log.WriteCode("START");
            txtOutputDirectory.Text = _settings.OutputDirectory;
            ShowResult(loaded.Notice);
        }

        private void btnMes_Click(object sender, EventArgs e)
        {
            var machine = new UploadStateMachine();
            machine.MoveTo(UploadState.MesQuerying);
            machine.MoveTo(UploadState.VisionMatching);
            machine.MoveTo(UploadState.UploadPending);
            machine.MoveTo(UploadState.Uploading);
            machine.MoveTo(UploadState.Indeterminate);
            machine.MoveTo(UploadState.ReconciliationWait);
            var breaker = new CircuitBreaker(new CircuitBreakerOptions { Enabled = true,
                FailureThreshold = _settings.FailureThreshold, OpenSeconds = _settings.OpenSeconds }, _log, () => DateTime.UtcNow);
            for (int i = 0; i < _settings.FailureThreshold; i++)
                breaker.ReportResult("DEMO", MesOperationResult.Fail(-1, "샘플", MesFailureCategory.Transport, true));
            ShowResult("MES 모의 시험: 결과 불명확 → 결과 재확인 대기. 통신 실패 누적 후 회로 차단 상태: " + breaker.State);
            _log.WriteCode("MES_DEMO");
        }

        private void btnConversion_Click(object sender, EventArgs e)
        {
            RetryDependency dependency;
            RetryPolicy.TryClassify(new FormatException(), out dependency);
            ShowResult("변환 모의 시험: 형식 오류는 입력 변경 후 재시도 대상입니다. 분류: " + dependency);
            _log.WriteCode("CONVERSION_DEMO");
        }

        private void btnCurrency_Click(object sender, EventArgs e)
        {
            var mapper = _settings.CreateCurrencyMapper();
            CurrencyMapping first = _settings.CurrencyMappings[0];
            mapper.ValidateApiRows(new[] { new Dictionary<string, object> { { "cur_unit", first.Code } } });
            ShowResult("환율 모의 시험: 설정의 샘플 통화 매핑 검증 완료. 실제 API·사내 DB 접속은 수행하지 않습니다.");
            _log.WriteCode("CURRENCY_DEMO");
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string previous = _settings.OutputDirectory;
            _settings.OutputDirectory = txtOutputDirectory.Text;
            string notice;
            if (!SettingsStore.TrySave(_settingsPath, _settings, out notice))
                _settings.OutputDirectory = previous;
            else
                _log.WriteCode("SETTINGS_SAVED");
            ShowResult(notice);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                string directory = _settings.ResolveOutputDirectory(_workspace);
                Directory.CreateDirectory(directory);
                // 고유 파일명으로 결과를 생성합니다. 기존 파일을 덮어쓰거나 자동 삭제하지 않습니다.
                string path = Path.Combine(directory, "sample-" + Guid.NewGuid().ToString("N") + ".txt");
                File.WriteAllText(path, "공개 샘플 실행 결과입니다. 실제 운영 데이터가 아닙니다.", new System.Text.UTF8Encoding(true));
                _log.WriteCode("SAMPLE_EXPORTED");
                ShowResult("로컬 샘플 결과를 저장했습니다. 기존 파일은 유지합니다.");
            }
            catch (UnauthorizedAccessException) { ShowResult("결과 저장 권한을 확인하세요."); _log.WriteCode("EXPORT_ACCESS_DENIED"); }
            catch (IOException) { ShowResult("결과 저장에 실패했습니다. 디스크와 파일 잠금을 확인하세요."); _log.WriteCode("EXPORT_IO_ERROR"); }
            catch (System.Runtime.Serialization.SerializationException) { ShowResult("출력 폴더 설정을 확인하세요."); }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _log.WriteCode("STOP");
            base.OnFormClosed(e);
        }

        private void ShowResult(string message)
        {
            txtLog.AppendText(message + Environment.NewLine);
            lbStatus.Text = _log != null && _log.HasWriteFailure ? "샘플 실행 가능 · 로컬 로그 기록 권한 확인 필요" : "샘플 전용 · 외부 서버 접속 없음";
        }
    }
}
