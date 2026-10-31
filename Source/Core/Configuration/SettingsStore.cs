using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace PortfolioSamples.Configuration
{
    public sealed class SettingsLoadResult
    {
        public SampleSettings Settings { get; private set; }
        public string Notice { get; private set; }
        public SettingsLoadResult(SampleSettings settings, string notice) { Settings = settings; Notice = notice; }
    }

    public static class SettingsStore
    {
        private const long MaximumSettingsBytes = 65536;

        // 초기 → 입력 파일 확인 → 검증 → 사용. 오류 시 원본을 덮어쓰지 않고 메모리 기본값으로 복구합니다.
        public static SettingsLoadResult Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return Defaults("설정 파일 없음: 기본값으로 실행합니다.");
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > MaximumSettingsBytes) return Defaults("설정 파일 크기 초과: 기본값으로 실행합니다.");
                    var settings = new DataContractJsonSerializer(typeof(SampleSettings)).ReadObject(stream) as SampleSettings;
                    if (settings == null) return Defaults("설정 내용 없음: 기본값으로 실행합니다.");
                    settings.Validate();
                    return new SettingsLoadResult(settings, "설정을 불러왔습니다.");
                }
            }
            catch (UnauthorizedAccessException) { return Defaults("설정 읽기 권한 없음: 기본값으로 실행합니다."); }
            catch (IOException) { return Defaults("설정 읽기 실패: 기본값으로 실행합니다."); }
            catch (SerializationException) { return Defaults("설정 형식 오류: 기본값으로 실행합니다."); }
            catch (ArgumentException) { return Defaults("설정값 오류: 기본값으로 실행합니다."); }
        }

        private static SettingsLoadResult Defaults(string notice)
        {
            return new SettingsLoadResult(SampleSettings.CreateDefault(), notice);
        }

        public static bool TrySave(string path, SampleSettings settings, out string notice)
        {
            // [주의] 실제 비밀번호·API 키는 이 파일에 넣지 않습니다. 공개 샘플은 외부 접속 자체를 포함하지 않습니다.
            try
            {
                settings.Validate();
                string directory = Path.GetDirectoryName(Path.GetFullPath(path));
                Directory.CreateDirectory(directory);
                // 직렬화·검증이 끝난 바이트를 저장하므로 직렬화 실패로 기존 파일을 비우지 않습니다.
                using (var buffer = new MemoryStream())
                {
                    new DataContractJsonSerializer(typeof(SampleSettings)).WriteObject(buffer, settings);
                    File.WriteAllBytes(path, buffer.ToArray());
                }
                notice = "설정을 저장했습니다.";
                return true;
            }
            catch (UnauthorizedAccessException) { notice = "설정 저장 권한을 확인하세요."; }
            catch (IOException) { notice = "설정 저장에 실패했습니다. 잠금 상태와 디스크를 확인하세요."; }
            catch (SerializationException) { notice = "설정 형식을 확인하세요."; }
            catch (ArgumentException) { notice = "설정값을 확인하세요."; }
            return false;
        }
    }
}
