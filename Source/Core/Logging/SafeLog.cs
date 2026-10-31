using System;
using System.Globalization;
using System.IO;
using System.Text;
using PortfolioSamples.Mes;

namespace PortfolioSamples.Logging
{
    /// <summary>샘플의 상태 코드만 KST 날짜별로 기록합니다. 경로·예외 원문·입력값은 기록하지 않습니다.</summary>
    public sealed class SafeLog : ILoggerService
    {
        private readonly string _directory;
        private readonly long _maximumBytes;
        private readonly object _sync = new object();
        public bool HasWriteFailure { get; private set; }

        public SafeLog(string directory, int maximumKilobytes)
        {
            if (maximumKilobytes < 4 || maximumKilobytes > 1024) throw new ArgumentOutOfRangeException("maximumKilobytes");
            _directory = directory;
            _maximumBytes = maximumKilobytes * 1024L;
        }

        public void Info(string message) { WriteCode("INFO"); }
        public void Warn(string message) { WriteCode("WARN"); }

        // ILoggerService에서 전달한 문자열도 그대로 저장하지 않습니다. 외부 응답·업무값의 재노출을 막습니다.
        public void WriteCode(string code)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(code ?? "", "^[A-Z0-9_]{1,32}$"))
                throw new ArgumentException("고정 로그 코드를 사용하세요.", "code");
            lock (_sync)
            {
                try
                {
                    Directory.CreateDirectory(_directory);
                    DateTimeOffset now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9));
                    string path = Path.Combine(_directory, now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log");
                    byte[] line = Encoding.UTF8.GetBytes(now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture) + " " + code + Environment.NewLine);
                    using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                    {
                        if (stream.Length + line.Length > _maximumBytes) return;
                        stream.Seek(0, SeekOrigin.End);
                        stream.Write(line, 0, line.Length);
                    }
                }
                catch (UnauthorizedAccessException) { HasWriteFailure = true; }
                catch (IOException) { HasWriteFailure = true; }
                catch (System.Security.SecurityException) { HasWriteFailure = true; }
            }
        }
    }
}
