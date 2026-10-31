using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using PortfolioSamples.Currency;

namespace PortfolioSamples.Configuration
{
    [DataContract]
    public sealed class CurrencyMapping
    {
        [DataMember] public string Code { get; set; }
        [DataMember] public string Value { get; set; }
    }

    /// <summary>공개 가능한 실행 설정만 저장합니다. 인증정보를 저장하는 모델이 아닙니다.</summary>
    [DataContract]
    public sealed class SampleSettings
    {
        [DataMember] public string OutputDirectory { get; set; }
        [DataMember] public int FailureThreshold { get; set; }
        [DataMember] public int OpenSeconds { get; set; }
        [DataMember] public int MaximumLogKilobytes { get; set; }
        [DataMember] public List<CurrencyMapping> CurrencyMappings { get; set; }

        public static SampleSettings CreateDefault()
        {
            return new SampleSettings {
                OutputDirectory = "Results", FailureThreshold = 3, OpenSeconds = 30,
                MaximumLogKilobytes = 256,
                CurrencyMappings = new List<CurrencyMapping> {
                    new CurrencyMapping { Code = "USD", Value = "USD_DEMO" },
                    new CurrencyMapping { Code = "JPY", Value = "JPY_DEMO" },
                    new CurrencyMapping { Code = "CNY", Value = "CNY_DEMO" }
                }
            };
        }

        // 파일을 읽은 뒤에도 검증합니다. 잘못된 값으로 외부 경로를 쓰는 문제를 방지합니다.
        public void Validate()
        {
            if (String.IsNullOrWhiteSpace(OutputDirectory) || OutputDirectory.Length > 40 ||
                !Regex.IsMatch(OutputDirectory, "^[A-Za-z0-9_-]+$") ||
                FailureThreshold < 1 || FailureThreshold > 100 || OpenSeconds < 1 || OpenSeconds > 3600 ||
                MaximumLogKilobytes < 4 || MaximumLogKilobytes > 1024 ||
                CurrencyMappings == null || CurrencyMappings.Count < 1 || CurrencyMappings.Count > 100)
                throw new SerializationException("설정값의 범위를 확인하세요.");
            CreateCurrencyMapper();
        }

        public CurrencyCodeMapper CreateCurrencyMapper()
        {
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (CurrencyMappings == null) throw new SerializationException("통화 매핑이 필요합니다.");
            foreach (CurrencyMapping item in CurrencyMappings)
            {
                if (item == null || String.IsNullOrWhiteSpace(item.Code) || mapping.ContainsKey(item.Code))
                    throw new SerializationException("통화 매핑이 중복되거나 비어 있습니다.");
                mapping.Add(item.Code, item.Value);
            }
            return new CurrencyCodeMapper(mapping);
        }

        public string ResolveOutputDirectory(string workspace)
        {
            Validate();
            string root = Path.GetFullPath(workspace).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string target = Path.GetFullPath(Path.Combine(root, OutputDirectory));
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("샘플 작업 폴더 밖으로 저장할 수 없습니다.");
            return target;
        }
    }
}
