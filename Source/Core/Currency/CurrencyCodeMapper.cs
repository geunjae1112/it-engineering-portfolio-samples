using System;
using System.Collections.Generic;
using System.Globalization;

namespace PortfolioSamples.Currency
{
    /// <summary>
    /// API 통화코드와 운영 DB CurrencyCd 매핑을 한곳에서 관리합니다.
    /// [주의] 미등록 통화를 임의 순번으로 저장하면 기존 통화와 충돌할 수 있으므로 즉시 차단합니다.
    /// </summary>
    public sealed class CurrencyCodeMapper
    {
        private readonly Dictionary<string, string> _databaseNumberByCode;

        // 사내 DB 순번은 공개하지 않습니다. 공개 예제의 매핑을 설정에서 주입합니다.
        public CurrencyCodeMapper(IDictionary<string, string> mapping)
        {
            if (mapping == null || mapping.Count == 0) throw new ArgumentException("통화 매핑이 필요합니다.", "mapping");
            _databaseNumberByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> item in mapping)
            {
                string key = NormalizeCurrencyCode(item.Key);
                if (key.Length != 3 || !System.Text.RegularExpressions.Regex.IsMatch(key, "^[A-Z]{3}$") ||
                    String.IsNullOrWhiteSpace(item.Value) || item.Value.Length > 32 ||
                    !System.Text.RegularExpressions.Regex.IsMatch(item.Value, "^[A-Za-z0-9_-]+$") ||
                    _databaseNumberByCode.ContainsKey(key))
                    throw new ArgumentException("통화 매핑 형식을 확인하세요.", "mapping");
                _databaseNumberByCode.Add(key, item.Value);
            }
        }

        public static string NormalizeCurrencyCode(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                return string.Empty;
            }

            string normalized = currencyCode.Trim().ToUpperInvariant();
            int bracketIndex = normalized.IndexOf('(');
            if (bracketIndex > 0)
            {
                normalized = normalized.Substring(0, bracketIndex);
            }

            return string.Equals(normalized, "CNH", StringComparison.OrdinalIgnoreCase)
                ? "CNY"
                : normalized;
        }

        public string ResolveDatabaseCurrencyNumber(string currencyCode)
        {
            string normalizedCode = NormalizeCurrencyCode(currencyCode);
            string databaseCurrencyNumber;
            if (_databaseNumberByCode.TryGetValue(normalizedCode, out databaseCurrencyNumber))
            {
                return databaseCurrencyNumber;
            }

            throw new InvalidOperationException(
                "CUR001" + " - 등록되지 않은 통화코드입니다.");
        }

        public void ValidateApiRows(IEnumerable<Dictionary<string, object>> apiRows)
        {
            if (apiRows == null)
            {
                throw new InvalidOperationException("CUR001" + " - API 통화 데이터가 없습니다.");
            }

            HashSet<string> seenCurrencyCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, object> apiRow in apiRows)
            {
                string currencyCode = NormalizeCurrencyCode(GetRowValue(apiRow, "cur_unit"));
                if (string.IsNullOrWhiteSpace(currencyCode) ||
                    !_databaseNumberByCode.ContainsKey(currencyCode))
                {
                    throw new InvalidOperationException(
                        "CUR001" + " - 등록되지 않은 통화코드입니다.");
                }

                if (!seenCurrencyCodes.Add(currencyCode))
                {
                    throw new InvalidOperationException(
                        "CUR002" + " - 중복 통화코드입니다.");
                }
            }
        }

        public static string GetRowValue(Dictionary<string, object> row, string key)
        {
            object value;
            return row != null && row.TryGetValue(key, out value)
                ? Convert.ToString(value, CultureInfo.InvariantCulture)
                : string.Empty;
        }
    }
}

