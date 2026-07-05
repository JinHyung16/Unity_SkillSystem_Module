#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GoogleSheetDataLoader.Editor
{
    /// <summary>미사용 데이터 제거 치트. 연결된 구글 스프레드시트의 탭 목록을 받아와,
    /// Resources/GoogleSheetData 안에 있지만 시트에는 없는 JSON(고아 파일)을 삭제한다.
    /// (탭 이름을 바꾸거나 지웠을 때 로컬에 남는 stale JSON 정리용. DB는 항상 시트가 SSOT.)</summary>
    public static class StaleDataCleaner
    {
        private const string PrefKeyUrl = "GoogleSheetDataLoader.Url";
        private const string PrefKeyClientId = "GoogleSheetDataLoader.OAuth.ClientId";
        private const string PrefKeyClientSecret = "GoogleSheetDataLoader.OAuth.ClientSecret";
        private const string DataFolder = "Assets/Resources/GoogleSheetData";

        [MenuItem("Tools/미사용 DB JSON 제거 (시트에 없는 것)")]
        public static async void Clean()
        {
            try
            {
                string url = EditorPrefs.GetString(PrefKeyUrl, string.Empty);
                string clientId = EditorPrefs.GetString(PrefKeyClientId, string.Empty);
                string clientSecret = EditorPrefs.GetString(PrefKeyClientSecret, string.Empty);

                if (string.IsNullOrWhiteSpace(url)
                    || string.IsNullOrWhiteSpace(clientId)
                    || string.IsNullOrWhiteSpace(clientSecret))
                {
                    EditorUtility.DisplayDialog("미사용 JSON 제거",
                        "구글 연동 정보(URL/ClientId/Secret)가 없습니다.\n'Tools/Google Sheet Data Loader'에서 먼저 연동하세요.", "확인");
                    return;
                }

                string spreadsheetId = SheetJsonConverter.ExtractSpreadsheetId(url.Trim());
                if (string.IsNullOrEmpty(spreadsheetId))
                {
                    EditorUtility.DisplayDialog("미사용 JSON 제거", "URL에서 스프레드시트 ID를 추출하지 못했습니다.", "확인");
                    return;
                }

                if (Directory.Exists(DataFolder) == false)
                {
                    EditorUtility.DisplayDialog("미사용 JSON 제거", "데이터 폴더가 없습니다:\n" + DataFolder, "확인");
                    return;
                }

                EditorUtility.DisplayProgressBar("미사용 JSON 제거", "구글 시트 탭 목록 조회 중...", 0.4f);
                string token = await OAuth2Authenticator.EnsureAccessTokenAsync(clientId, clientSecret);
                List<SheetMeta> sheets = await GoogleSheetsApi.ListSheetsAsync(spreadsheetId, token);
                EditorUtility.ClearProgressBar();

                HashSet<string> liveNames = new HashSet<string>(StringComparer.Ordinal);
                if (sheets != null)
                {
                    for (int i = 0; i < sheets.Count; i++)
                    {
                        SheetMeta s = sheets[i];
                        if (s == null || string.IsNullOrEmpty(s.Title))
                            continue;
                        liveNames.Add(Sanitize(s.Title.Trim()));
                    }
                }

                if (liveNames.Count == 0)
                {
                    EditorUtility.DisplayDialog("미사용 JSON 제거", "시트 탭을 하나도 못 받아왔습니다. 안전을 위해 중단합니다.", "확인");
                    return;
                }

                List<string> stale = new List<string>();
                foreach (string path in Directory.GetFiles(DataFolder, "*.json"))
                {
                    string baseName = Path.GetFileNameWithoutExtension(path);
                    if (string.IsNullOrEmpty(baseName))
                        continue;
                    if (liveNames.Contains(baseName))
                        continue;
                    stale.Add(path.Replace('\\', '/'));
                }

                if (stale.Count == 0)
                {
                    EditorUtility.DisplayDialog("미사용 JSON 제거",
                        $"시트 탭 {liveNames.Count}개 기준 — 제거할 미사용 JSON이 없습니다.", "확인");
                    return;
                }

                StringBuilder list = new StringBuilder();
                for (int i = 0; i < stale.Count; i++)
                {
                    list.Append(" - ").Append(Path.GetFileName(stale[i])).Append('\n');
                }

                bool ok = EditorUtility.DisplayDialog("미사용 JSON 제거",
                    $"시트에 없는 JSON {stale.Count}개를 삭제합니다:\n\n{list}\n진행할까요?", "삭제", "취소");
                if (ok == false)
                    return;

                int deleted = 0;
                for (int i = 0; i < stale.Count; i++)
                {
                    string path = stale[i];
                    if (AssetDatabase.DeleteAsset(path))
                    {
                        deleted++;
                        continue;
                    }
                    // 폴백: 에셋 삭제 실패 시 파일+meta 직접 삭제
                    try
                    {
                        File.Delete(path);
                        string meta = path + ".meta";
                        if (File.Exists(meta))
                            File.Delete(meta);
                        deleted++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[StaleDataCleaner] '{Path.GetFileName(path)}' 삭제 실패: {e.Message}");
                    }
                }

                AssetDatabase.Refresh();
                Debug.Log($"[StaleDataCleaner] 미사용 JSON {deleted}/{stale.Count}개 삭제 완료 (시트 탭 {liveNames.Count}개 기준)");
                EditorUtility.DisplayDialog("미사용 JSON 제거", $"{deleted}개 삭제 완료.", "확인");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[StaleDataCleaner] 실패: {e.Message}");
                EditorUtility.DisplayDialog("미사용 JSON 제거", "실패: " + e.Message, "확인");
            }
        }

        private static string Sanitize(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
            return sb.ToString();
        }
    }
}
#endif
