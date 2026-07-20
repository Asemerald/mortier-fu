using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using MortierFu.Shared;

namespace MortierFu.Analytics
{
    public static class AnalyticsNetwork
    {
        public static async UniTask<bool> SendFormWithRedirectHandling(string url, WWWForm form, string label)
        {
            using (UnityWebRequest www = UnityWebRequest.Post(url, form))
            {
                www.redirectLimit = 0;

                try
                {
                    await www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        Logs.Log($"Successfully sent data for {label} to Google Sheets");
                        return true;
                    }

                    Logs.LogError($"Error sending data to Google Sheets: {www.error}");
                    return false;
                }
                catch (System.Exception)
                {
                    string redirectUrl = www.GetResponseHeader("Location");
                    if (string.IsNullOrEmpty(redirectUrl))
                    {
                        Logs.LogError($"Should redirected but can't find url");
                        return false;
                    }

                    using (UnityWebRequest redirected = UnityWebRequest.Get(redirectUrl))
                    {
                        await redirected.SendWebRequest();

                        if (redirected.result != UnityWebRequest.Result.Success)
                        {
                            Logs.LogError($"Error (redirected): {redirected.error}");
                            return false;
                        }

                        Logs.Log($"Successfully sent data for {label} to Google Sheets (redirected). Response: {redirected.downloadHandler.text}");
                        return true;
                    }
                }
            }
        }
    }
}
