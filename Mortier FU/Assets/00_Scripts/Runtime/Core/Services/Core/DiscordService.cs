using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using MortierFu;
using Discord.Sdk;
using MortierFu.Shared;
using UnityEngine.UI;

public class DiscordService : IGameService
{
    public bool IsInitialized { get; set; }
    private ulong _clientId = 1434840712305967125;

    private Client _client;
    private string _codeVerifier;
    
    [Serializable]
    public class SavedDiscordAuth
    {
        public string accessToken;
        public string refreshToken;
        public long expiresAt;
    }

    private void SaveAuth(SavedDiscordAuth data)
    {
        PlayerPrefs.SetString("discord_auth", JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private SavedDiscordAuth LoadAuth()
    {
        if (!PlayerPrefs.HasKey("discord_auth")) return null;
        return JsonUtility.FromJson<SavedDiscordAuth>(PlayerPrefs.GetString("discord_auth"));
    }


    public UniTask OnInitialize()
    {
        _client = new Client();
        _client.AddLogCallback(OnLog, LoggingSeverity.Error);
        _client.SetStatusChangedCallback(OnStatusChanged);

        var saved = LoadAuth();
        if (saved != null)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // ✅ Token valide ? On l'utilise directement
            if (now < saved.expiresAt)
            {
                Debug.Log("[DiscordService] Using saved access token.");
                _client.UpdateToken(AuthorizationTokenType.Bearer, saved.accessToken, (_) => _client.Connect());
                return UniTask.CompletedTask;
            }

            // ✅ Token expiré ? On rafraîchit
            Debug.Log("[DiscordService] Refreshing token...");
            RefreshToken(saved.refreshToken);

            return UniTask.CompletedTask;
        }

        // ✅ Première fois → OAuth flow classique
        StartOAuthFlow();
        return UniTask.CompletedTask;
    }
    
    private void RefreshToken(string refreshToken)
    {
        _client.RefreshToken(
            _clientId,
            refreshToken,
            (result, newToken, newRefresh, tokenType, expiresIn, scope) =>
            {
                if (!result.Successful())
                {
                    Debug.LogError("[DiscordService] Failed to refresh token, restarting OAuth.");
                    StartOAuthFlow();
                    return;
                }

                Debug.Log("[DiscordService] Token refreshed!");

                var saved = new SavedDiscordAuth
                {
                    accessToken = newToken,
                    refreshToken = newRefresh,
                    expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn
                };

                SaveAuth(saved);

                _client.UpdateToken(AuthorizationTokenType.Bearer, newToken, (_) => _client.Connect());
            }
        );
    }



    private void OnLog(string message, LoggingSeverity severity)
    {
        Debug.Log($"Log: {severity} - {message}");
    }

    private void OnStatusChanged(Client.Status status, Client.Error error, int errorCode)
    {
        Logs.Log($"[DiscordService] Status changed: {status}, Error: {error}, Code: {errorCode}");
        if(error != Client.Error.None)
        {
            Logs.LogError($"[DiscordService] Error occurred: {error}, Code: {errorCode}");
            return;
        }

        if (status == Client.Status.Ready)
        {
            ClientReady();
        }
    }

    private void ClientReady()
    {
            Debug.Log($"Friend Count: {_client.GetRelationships().Count()}");

            ActivityButton button = new ActivityButton();
            button.SetLabel("Jsp quoi mettre la");
            button.SetUrl("https://www.github.com/Asemerald/mortier-fu");
            Activity activity = new Activity();
            activity.SetType(ActivityTypes.Competing);
            activity.SetState("In Competitive Match");
            activity.SetDetails("Rank: Diamond II");
            activity.AddButton(button);
            _client.UpdateRichPresence(activity, (ClientResult result) => {
                if (result.Successful()) {
                    Debug.Log("Rich presence updated!");
                } else {
                    Debug.LogError("Failed to update rich presence");
                }
            });
    }

    private void StartOAuthFlow() {
        var authorizationVerifier = _client.CreateAuthorizationCodeVerifier();
        _codeVerifier = authorizationVerifier.Verifier();
        
        var args = new AuthorizationArgs();
        args.SetClientId(_clientId);
        args.SetScopes(Client.GetDefaultPresenceScopes());
        args.SetCodeChallenge(authorizationVerifier.Challenge());
        _client.Authorize(args, OnAuthorizeResult);
    }

    private void OnAuthorizeResult(ClientResult result, string code, string redirectUri) {
        Debug.Log($"Authorization result: [{result.Error()}] [{code}] [{redirectUri}]");
        if (!result.Successful()) {
            return;
        }
        GetTokenFromCode(code, redirectUri);
    }

    private void GetTokenFromCode(string code, string redirectUri)
    {
        _client.GetToken(_clientId, code, _codeVerifier, redirectUri,
            (result, token, refreshToken, tokenType, expiresIn, scope) =>
            {
                if (!result.Successful())
                {
                    Logs.LogError("[DiscordService] Failed to get token: " + result.Error());
                    return;
                }

                var saved = new SavedDiscordAuth
                {
                    accessToken = token,
                    refreshToken = refreshToken,
                    expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn
                };

                SaveAuth(saved);
                OnReceivedToken(token);
            }
        );
    }


    private void OnReceivedToken(string token) {
        Debug.Log("Token received: " + token);
        _client.UpdateToken(AuthorizationTokenType.Bearer, token, (ClientResult result) => { _client.Connect(); });
    }
    
    
    

    public void Dispose()
    {
        _client.Dispose();
    }
}
