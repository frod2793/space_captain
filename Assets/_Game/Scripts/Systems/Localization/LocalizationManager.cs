using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SpaceCaptain.Systems.Localization
{
    public class LocalizationManager
    {
        private Dictionary<string, Dictionary<LanguageType, string>> m_translationTable;
        private LanguageType m_currentLanguage = LanguageType.Korean;

        public event Action<LanguageType> OnLanguageChanged;

        public LanguageType CurrentLanguage => m_currentLanguage;

        public LocalizationManager()
        {
            m_translationTable = new Dictionary<string, Dictionary<LanguageType, string>>();
        }

        public async UniTask LoadTranslationsAsync(string resourcePath, CancellationToken cancellationToken = default)
        {
            TextAsset csvAsset = await Resources.LoadAsync<TextAsset>(resourcePath).WithCancellation(cancellationToken) as TextAsset;

            if (csvAsset == null)
            {
                Debug.LogError($"[LocalizationManager] 번역 파일 로드 실패: {resourcePath}");
                return;
            }

            ParseCsv(csvAsset.text);
            Debug.Log($"[LocalizationManager] 번역 파일 로드 성공: {resourcePath}");
        }

        public void ChangeLanguage(LanguageType newLanguage)
        {
            if (m_currentLanguage == newLanguage)
            {
                return;
            }

            m_currentLanguage = newLanguage;
            OnLanguageChanged?.Invoke(m_currentLanguage);
        }

        public string GetTranslation(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (m_translationTable.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(m_currentLanguage, out string text))
                {
                    return text;
                }
            }

            Debug.LogWarning($"[LocalizationManager] 번역 키를 찾을 수 없음: {key}");
            return key;
        }

        private void ParseCsv(string csvText)
        {
            m_translationTable.Clear();

            using (StringReader reader = new StringReader(csvText))
            {
                string headerLine = reader.ReadLine();
                if (string.IsNullOrEmpty(headerLine))
                {
                    return;
                }

                // Header mapping: Key, Korean, English, ...
                string[] headers = headerLine.Split(',');
                Dictionary<int, LanguageType> columnMap = new Dictionary<int, LanguageType>();

                for (int i = 1; i < headers.Length; i++)
                {
                    if (Enum.TryParse(headers[i].Trim(), out LanguageType lang))
                    {
                        columnMap[i] = lang;
                    }
                }

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] values = line.Split(',');
                    if (values.Length < 1)
                    {
                        continue;
                    }

                    string key = values[0].Trim();
                    var languageDict = new Dictionary<LanguageType, string>();

                    for (int i = 1; i < values.Length; i++)
                    {
                        if (columnMap.TryGetValue(i, out LanguageType lang))
                        {
                            languageDict[lang] = values[i].Trim().Replace("\\n", "\n");
                        }
                    }

                    m_translationTable[key] = languageDict;
                }
            }
        }
    }
}
