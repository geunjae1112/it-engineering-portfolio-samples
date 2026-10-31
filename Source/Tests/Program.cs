using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using PortfolioSamples.Configuration;
using PortfolioSamples.Conversion;
using PortfolioSamples.Logging;
using PortfolioSamples.Mes;

namespace PortfolioSamples.Tests
{
    internal static class Program
    {
        private static int _passed;
        private static int _failed;
        private static string _testDirectory;

        [STAThread]
        private static int Main()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "PortfolioSamplesTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
            Run("정상 업로드 상태 전이", () => {
                var state = new UploadStateMachine();
                foreach (var next in new[] {UploadState.MesQuerying, UploadState.VisionMatching, UploadState.UploadPending, UploadState.Uploading, UploadState.Success}) state.MoveTo(next);
                Equal(UploadState.Success, state.Current);
            });
            Run("검증 단계를 건너뛰는 업로드 차단", () => Throws<InvalidOperationException>(() => new UploadStateMachine().MoveTo(UploadState.Uploading)));
            Run("응답 불명확 상태와 재확인 대기 분리", () => {
                var state = new UploadStateMachine(); state.MoveTo(UploadState.Indeterminate); state.MoveTo(UploadState.ReconciliationWait);
                Equal(UploadState.ReconciliationWait, state.Current);
                Equal(MesResultKind.Indeterminate, MesOperationResult.Classify(MesOperationResult.Indeterminate(-408, "SAMPLE")));
            });
            Run("업무 거절은 회로 장애로 집계하지 않음", () => {
                var breaker = Breaker(() => DateTime.UtcNow);
                for (int i=0;i<5;i++) breaker.ReportResult("DEMO", MesOperationResult.Fail(1,"SAMPLE", MesFailureCategory.Business,false));
                Equal(0, breaker.FailureCount); Equal(CircuitBreakerState.Closed, breaker.State);
            });
            Run("통신 장애 임계치 도달 시 회로 차단", () => {
                var breaker = Breaker(() => DateTime.UtcNow); FailTwice(breaker);
                Equal(CircuitBreakerState.Open, breaker.State); DateTime? after; Equal(false, breaker.TryEnter(out after)); Equal(true, after.HasValue);
            });
            Run("차단 만료 후 동시 복구 시험 한 건만 허용", () => {
                DateTime now = new DateTime(2020,1,1,0,0,0,DateTimeKind.Utc); var breaker = Breaker(() => now); FailTwice(breaker); now = now.AddSeconds(30);
                int allowed=0; Parallel.For(0,16,i=> { DateTime? after; if (breaker.TryEnter(out after)) Interlocked.Increment(ref allowed); }); Equal(1,allowed);
                breaker.ReportSuccess(); Equal(CircuitBreakerState.Closed, breaker.State);
            });
            Run("복구 시험 실패 시 다시 차단", () => {
                DateTime now=DateTime.UtcNow; var breaker=Breaker(()=>now); FailTwice(breaker); now=now.AddSeconds(30); DateTime? after;
                Equal(true,breaker.TryEnter(out after)); breaker.ReportResult("DEMO",MesOperationResult.Fail(-1,"SAMPLE",MesFailureCategory.Timeout,true)); Equal(CircuitBreakerState.Open,breaker.State);
            });
            Run("복구 시험 취소는 성공·통신 실패와 구분", () => {
                DateTime now=DateTime.UtcNow; var breaker=Breaker(()=>now); FailTwice(breaker); now=now.AddSeconds(30); DateTime? after;
                breaker.TryEnter(out after); breaker.ReleaseProbeWithoutResult("DEMO"); Equal(true,breaker.TryEnter(out after));
            });
            Run("잘못된 회로 임계치 거부", () => Throws<ArgumentOutOfRangeException>(()=>new CircuitBreaker(new CircuitBreakerOptions {Enabled=true, FailureThreshold=0,OpenSeconds=30},new MemoryLog(),()=>DateTime.UtcNow)));
            Run("설정 오류는 설정 변경 후 재시도", ()=>Classify(new ConversionException("CFG001",false),RetryDependency.Settings));
            Run("무결성 오류는 출력 확인 후 재시도", ()=>Classify(new ConversionException("INTEGRITY001",false),RetryDependency.Output));
            Run("형식 오류는 입력 변경 후 재시도", ()=>Classify(new FormatException(),RetryDependency.Input));
            Run("파일 없음은 시간 재시도로 분류", ()=>Classify(new FileNotFoundException(),RetryDependency.Time));
            Run("권한 오류는 시간 재시도로 분류", ()=>Classify(new UnauthorizedAccessException(),RetryDependency.Time));
            Run("취소는 재시도로 삼키지 않음", ()=>Throws<OperationCanceledException>(()=>{RetryDependency dependency; RetryPolicy.TryClassify(new OperationCanceledException(),out dependency);}));
            Run("일반 디스크 오류를 파일 잠금으로 오판하지 않음", ()=>Equal(false,RetryPolicy.IsFileLocked(new IOException())));
            Run("공유 위반 오류만 파일 잠금으로 분류", ()=>Equal(true,RetryPolicy.IsFileLocked(new LockedFileException())));
            Run("분류하지 않은 런타임 오류 보존", ()=>{RetryDependency dependency; Equal(false,RetryPolicy.TryClassify(new InvalidOperationException(),out dependency));});
            Run("통화 코드 정규화", ()=>Equal("JPY",PortfolioSamples.Currency.CurrencyCodeMapper.NormalizeCurrencyCode(" jpy(100) ")));
            Run("CNH 별칭 정규화", ()=>Equal("CNY",PortfolioSamples.Currency.CurrencyCodeMapper.NormalizeCurrencyCode("CNH")));
            Run("사내 순번 대신 설정 매핑 사용", ()=>Equal("USD_DEMO",SampleSettings.CreateDefault().CreateCurrencyMapper().ResolveDatabaseCurrencyNumber("usd")));
            Run("미등록 통화 차단", ()=>Throws<InvalidOperationException>(()=>SampleSettings.CreateDefault().CreateCurrencyMapper().ResolveDatabaseCurrencyNumber("ZZZ")));
            Run("정규화 이후 중복 통화 차단", ()=>Throws<InvalidOperationException>(()=>SampleSettings.CreateDefault().CreateCurrencyMapper().ValidateApiRows(new[]{Row("CNY"),Row("CNH")})));
            Run("빈 API 결과 차단", ()=>Throws<InvalidOperationException>(()=>SampleSettings.CreateDefault().CreateCurrencyMapper().ValidateApiRows(null)));
            Run("DB 명령문 형태 매핑 차단", ()=>Throws<ArgumentException>(()=>new PortfolioSamples.Currency.CurrencyCodeMapper(new Dictionary<string,string>{{"USD","x;DROP"}})));
            Run("설정 파일 없음 기본값 복구", ()=>Equal(3,SettingsStore.Load(Path.Combine(_testDirectory,"missing.json")).Settings.FailureThreshold));
            Run("깨진 설정 기본값 복구·원문 유지", ()=>{
                string path=Path.Combine(_testDirectory,"broken.json"); File.WriteAllText(path,"{bad"); Equal(3,SettingsStore.Load(path).Settings.FailureThreshold); Equal("{bad",File.ReadAllText(path));
            });
            Run("설정 경로 탈출 차단", ()=>{var settings=SampleSettings.CreateDefault();settings.OutputDirectory="../outside";Throws<SerializationException>(()=>settings.ResolveOutputDirectory(_testDirectory));});
            Run("설정 절대 경로 차단", ()=>{var settings=SampleSettings.CreateDefault();settings.OutputDirectory=Path.GetPathRoot(_testDirectory);Throws<SerializationException>(()=>settings.Validate());});
            Run("설정 저장·다시 불러오기", ()=>{
                string path=Path.Combine(_testDirectory,"roundtrip.json");var settings=SampleSettings.CreateDefault(); settings.FailureThreshold=7;string notice;
                Equal(true,SettingsStore.TrySave(path,settings,out notice));Equal(7,SettingsStore.Load(path).Settings.FailureThreshold);
            });
            Run("잠긴 설정 저장 실패 후 원문 유지", ()=>{
                string path=Path.Combine(_testDirectory,"locked.json");File.WriteAllText(path,"unchanged");string notice;
                using(var file=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) Equal(false,SettingsStore.TrySave(path,SampleSettings.CreateDefault(),out notice));
                Equal("unchanged",File.ReadAllText(path));
            });
            Run("읽기 전용 설정 권한 오류 복구", ()=>{
                string path=Path.Combine(_testDirectory,"readonly.json");File.WriteAllText(path,"unchanged");File.SetAttributes(path,FileAttributes.ReadOnly);string notice;
                try { Equal(false,SettingsStore.TrySave(path,SampleSettings.CreateDefault(),out notice)); Equal("unchanged",File.ReadAllText(path)); }
                finally { File.SetAttributes(path,FileAttributes.Normal); }
            });
            Run("로그 문자열에서 원문을 제외", ()=>{
                string dir=Path.Combine(_testDirectory,"logs");var log=new SafeLog(dir,4);log.Info("PRIVATE_SAMPLE_SHOULD_NOT_BE_WRITTEN");
                string text=File.ReadAllText(Directory.GetFiles(dir)[0]);Equal(false,text.Contains("PRIVATE_SAMPLE"));Equal(true,text.Contains("+09:00"));
            });
            Run("로그 크기 제한과 자동 삭제 없음", ()=>{
                string dir=Path.Combine(_testDirectory,"capped-logs");var log=new SafeLog(dir,4);for(int i=0;i<500;i++)log.WriteCode("TEST");
                string path=Directory.GetFiles(dir)[0];Equal(true,new FileInfo(path).Length<=4096);Equal(1,Directory.GetFiles(dir).Length);
            });
            Run("로그 I/O 오류 후 프로세스 계속 실행", ()=>{
                string path=Path.Combine(_testDirectory,"not-directory");File.WriteAllText(path,"sample");var log=new SafeLog(path,4);log.WriteCode("TEST");Equal(true,log.HasWriteFailure);
            });
            Console.WriteLine("결과: " + _passed + "개 통과, " + _failed + "개 실패");
            Console.WriteLine("실제 장비·DB·외부 API를 사용하지 않은 공개 샘플 시험입니다.");
            return _failed == 0 ? 0 : 1;
        }

        private static Dictionary<string,object> Row(string code) {return new Dictionary<string,object>{{"cur_unit",code}};}
        private static CircuitBreaker Breaker(Func<DateTime> clock) {return new CircuitBreaker(new CircuitBreakerOptions {Enabled=true,FailureThreshold=2,OpenSeconds=30},new MemoryLog(),clock);}
        private static void FailTwice(CircuitBreaker breaker) {for(int i=0;i<2;i++)breaker.ReportResult("DEMO",MesOperationResult.Fail(-1,"SAMPLE",MesFailureCategory.Transport,true));}
        private static void Classify(Exception error,RetryDependency expected) {RetryDependency actual;Equal(true,RetryPolicy.TryClassify(error,out actual));Equal(expected,actual);}
        private static void Run(string name,Action action) {try{action();_passed++;Console.WriteLine("통과: "+name);}catch(Exception){_failed++;Console.WriteLine("실패: "+name);}}
        private static void Equal<T>(T expected,T actual) {if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new InvalidOperationException("기대 결과 불일치");}
        private static void Throws<T>(Action action) where T:Exception {try{action();}catch(T){return;}throw new InvalidOperationException("예상 예외 없음");}
        private sealed class MemoryLog:ILoggerService
        {
            public readonly List<string> Entries = new List<string>();
            public void Info(string message) { Entries.Add("INFO"); }
            public void Warn(string message) { Entries.Add("WARN"); }
        }
        private sealed class LockedFileException:IOException {public LockedFileException(){HResult=unchecked((int)0x80070020);}}
    }
}
