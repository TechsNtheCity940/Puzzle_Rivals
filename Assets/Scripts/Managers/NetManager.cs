using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine.Networking;

/// <summary>
/// Matchmaking result containing lobby information.
/// The lobby is always filled to totalSlots:
/// - human slots are real players found online
/// - the remainder are AI bots
/// </summary>
public class MatchmakingResult
{
    public bool online;
    public int totalSlots;
    public int realPlayers;
    public int botCount;
    public string roomId = "";
    public List<int> humanSlots = new List<int>();
    public List<int> aiSlots = new List<int>();
    
    // Player info for each slot
    // Index corresponds to slot index
    public List<PlayerInfo> players = new List<PlayerInfo>();
    
    // Legacy field - kept for compatibility
    public List<string> playerNames = new List<string>();
    
    public MatchmakingResult()
    {
        players = new List<PlayerInfo>();
    }
}

// PlayerInfo for matchmaking results (mirrors LoadingScreen.PlayerInfo)
[System.Serializable]
public class PlayerInfo
{
    public int slot;
    public bool isHuman;
    public string puzzleTag;
    public bool isLocal;
    
    public PlayerInfo() {}
    
    public PlayerInfo(int s, bool human, string tag, bool local)
    {
        slot = s;
        isHuman = human;
        puzzleTag = tag;
        isLocal = local;
    }
}

/// <summary>
/// Client matchmaking using Unity Multiplayer Center SDK.
/// Searches for real players; if the backend is unreachable or returns no
/// opponents within the timeout, the lobby is filled with AI bots so the match
/// can always start. WebGL-compatible using coroutines.
/// </summary>
public class NetManager : MonoBehaviour
{
    public static NetManager Instance { get; private set; }

    [Header("Supabase Configuration")]
    [Tooltip("Supabase URL from environment variable (VITE_SUPABASE_URL)")]
    public string supabaseUrl = "";
    [Tooltip("Supabase API key from environment variable (VITE_SUPABASE_PUBLISHABLE_KEY)")]
    public string supabaseApiKey = "";

    [Header("Matchmaking Settings")]
    public float defaultSearchTimeout = 6f;
    public int maxRetryAttempts = 3;
    
    [Header("Quick Match Settings")]
    [Tooltip("When true, skip live player search and fill lobby with bots immediately (for debugging/testing)")]
    public bool quickMatchNoLivePlayers = false;

    [Header("State")]
    public bool isConnected = false;
    public string roomId = "";

    public event Action<MatchmakingResult> OnMatchReady;

    private MatchmakingResult _lastResult;
    private bool _unityServicesInitialized = false;
    private bool _matchmakingTimedOut = false;
    private bool _mpCenterFailed = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        await InitializeUnityServices();
    }

    /// <summary>
    /// Initialize Unity Services including Multiplayer Center
    /// </summary>
    private async System.Threading.Tasks.Task InitializeUnityServices()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }
            
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // Load configuration from environment or PlayerPrefs
            LoadConfiguration();
            
            _unityServicesInitialized = true;
            Debug.Log("[NetManager] Unity Services initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetManager] Failed to initialize Unity Services: {e.Message}");
            _unityServicesInitialized = false;
        }
    }

    /// <summary>
    /// Load configuration from environment variables or PlayerPrefs
    /// </summary>
    private void LoadConfiguration()
    {
        // Try to load from environment first, then fall back to PlayerPrefs
        supabaseUrl = System.Environment.GetEnvironmentVariable("VITE_SUPABASE_URL") ??
                      PlayerPrefs.GetString("supabase_url", "https://mp.tesana.ai");
        supabaseApiKey = System.Environment.GetEnvironmentVariable("VITE_SUPABASE_PUBLISHABLE_KEY") ??
                         PlayerPrefs.GetString("supabase_api_key", "");
    }

    /// <summary>
    /// Searches for real players to fill `totalSlots` (localSlot is always the
    /// local human). Fills any unfilled slots with AI bots. Invokes onComplete
    /// with the resolved roster. Never fails — always returns a full lobby.
    /// </summary>
    public IEnumerator FindMatch(int totalSlots, int localSlot, float timeout, Action<MatchmakingResult> onComplete)
    {
        totalSlots = Mathf.Max(1, totalSlots);
        localSlot = Mathf.Clamp(localSlot, 0, totalSlots - 1);
        timeout = Mathf.Max(0.5f, timeout);

        var result = new MatchmakingResult { totalSlots = totalSlots };
        var humans = new List<int> { localSlot }; // the local player is always real
        bool online = false;

        // Initialize player info for each slot
        string localPuzzleTag = GetLocalPlayerName();
        for (int i = 0; i < totalSlots; i++)
        {
            bool isHuman = i == localSlot;
            string tag = isHuman ? localPuzzleTag : "";
            var playerInfo = new PlayerInfo(i, isHuman, tag, isHuman);
            result.players.Add(playerInfo);
            result.playerNames.Add(tag);
        }

        // QUICK MATCH MODE: Skip live player search entirely, fill with bots
        if (quickMatchNoLivePlayers)
        {
            Debug.Log("[Matchmaking] Quick Match mode: skipping live player search, filling with bots");
            online = false;
            
            // Generate bot puzzle tags for all non-local slots
            for (int slot = 0; slot < totalSlots; slot++)
            {
                if (slot != localSlot)
                {
                    GenerateBotPuzzleTag(result, slot);
                }
            }
        }
        else
        {
            Debug.Log($"[Matchmaking] Searching for real players ({totalSlots} slots, {timeout:0.0}s timeout)...");

            // Reset timeout flags
            _matchmakingTimedOut = false;
            _mpCenterFailed = false;

            // Use HTTP fallback with timeout - skip MP Center since packages may not be available
            Debug.Log("[Matchmaking] Attempting HTTP fallback matchmaking...");
            yield return FindMatchViaHttpFallback(totalSlots, timeout, result, ref online, humans, localSlot);
            
            // CRITICAL FIX: Ensure matchmaking is finalized even after HTTP failure
            if (!online && _matchmakingTimedOut)
            {
                Debug.Log("[Matchmaking] HTTP fallback timeout, forcing bot fill");
                _mpCenterFailed = true;
            }

            // If still not online after all attempts, fill with bots
            if (!online)
            {
                Debug.Log("[Matchmaking] No real players found, using AI bots only");
                for (int slot = 0; slot < totalSlots; slot++)
                {
                    if (!humans.Contains(slot) && string.IsNullOrEmpty(result.playerNames[slot]))
                    {
                        GenerateBotPuzzleTag(result, slot);
                    }
                }
            }
        }

        // Fill AI slots list and update player info for non-human slots
        for (int slot = 0; slot < totalSlots; slot++)
        {
            if (humans.Contains(slot))
            {
                result.humanSlots.Add(slot);
                if (slot < result.players.Count)
                    result.players[slot].isHuman = true;
            }
            else
            {
                result.aiSlots.Add(slot);
                if (slot < result.players.Count)
                    result.players[slot].isHuman = false;
            }
        }

        // Finalize result
        result.online = online;
        result.realPlayers = humans.Count;
        result.botCount = totalSlots - humans.Count;
        result.roomId = (online ? "ROOM_" : "LOCAL_") + UnityEngine.Random.Range(1000, 9999);

        Debug.Log($"[Matchmaking] Lobby ready ({result.roomId}): {result.realPlayers} real, {result.botCount} bots.");
        OnMatchReady?.Invoke(result);
        
        // CRITICAL FIX: Always invoke the callback to prevent hanging
        onComplete?.Invoke(result);
    }

    public IEnumerator FindMatch(int totalSlots, int localSlot, Action<MatchmakingResult> onComplete)
        => FindMatch(totalSlots, localSlot, defaultSearchTimeout, onComplete);

    /// <summary>
    /// Get the local player's puzzle tag/name
    /// </summary>
    private string GetLocalPlayerName()
    {
        var profile = PlayerProfile.Instance?.data;
        if (profile != null)
        {
            return string.IsNullOrEmpty(profile.username) ? "RIVAL" : profile.username;
        }
        return "RIVAL";
    }

    /// <summary>
    /// Legacy HTTP-based matchmaking fallback with timeout
    /// </summary>
    private IEnumerator FindMatchViaHttpFallback(int totalSlots, float timeout, MatchmakingResult result, 
        ref bool online, List<int> humans, int localSlot)
    {
        string serverUrl = string.IsNullOrEmpty(supabaseUrl) ? "https://mp.tesana.ai" : supabaseUrl;
        
        float startTime = Time.time;
        float effectiveTimeout = Mathf.Max(1f, Mathf.Min(timeout, 8f));
        
        UnityWebRequest req = null;
        bool requestFaulted = false;
        
        try
        {
            req = UnityWebRequest.Get(serverUrl + "/matchmake?slots=" + totalSlots);
            req.timeout = Mathf.CeilToInt(effectiveTimeout);
            
            if (!string.IsNullOrEmpty(supabaseApiKey))
            {
                req.SetRequestHeader("apikey", supabaseApiKey);
            }
        }
        catch (Exception e)
        {
            requestFaulted = true;
            Debug.LogWarning("[Matchmaking] Request setup failed: " + e.Message);
        }

        if (!requestFaulted && req != null)
        {
            Debug.Log("[Matchmaking] Sending HTTP request to " + serverUrl + "/matchmake?slots=" + totalSlots);
            
            yield return req.SendWebRequest();

            bool requestFailed = false;
            
            if (Time.time - startTime > effectiveTimeout)
            {
                Debug.Log("[Matchmaking] HTTP request timed out after " + effectiveTimeout + "s");
                requestFailed = true;
                _matchmakingTimedOut = true;
            }
            else if (req.result == UnityWebRequest.Result.Success)
            {
                string responseText = req.downloadHandler?.text ?? "";
                bool validResponse = !string.IsNullOrEmpty(responseText) && 
                                     (responseText.Contains("\"players\"") || responseText.StartsWith("{"));
                
                if (validResponse)
                {
                    online = true;
                    TryParseRemoteHumans(responseText, result.totalSlots, localSlot, humans);
                    Debug.Log("[Matchmaking] Backend reachable. Real players found: " + humans.Count);
                }
                else
                {
                    Debug.Log("[Matchmaking] Backend unreachable (invalid response). Falling back to AI bots.");
                    requestFailed = true;
                    _mpCenterFailed = true;
                }
                if (req != null) req.Dispose();
            }
            else
            {
                Debug.Log("[Matchmaking] HTTP request failed (" + req.result + "): " + req.error);
                Debug.Log("[Matchmaking] Backend unreachable. Falling back to AI bots.");
                requestFailed = true;
                _mpCenterFailed = true;
                if (req != null) req.Dispose();
            }
        }
        else
        {
            Debug.Log("[Matchmaking] HTTP request could not be initiated. Falling back to AI bots.");
            _mpCenterFailed = true;
        }
        
        if (!online)
        {
            for (int slot = 0; slot < totalSlots; slot++)
            {
                if (!humans.Contains(slot) && string.IsNullOrEmpty(result.playerNames[slot]))
                {
                    GenerateBotPuzzleTag(result, slot);
                }
            }
        }
        
        Debug.Log("[Matchmaking] HTTP fallback completed, online=" + online + ", mpCenterFailed=" + _mpCenterFailed);
    }

    /// <summary>
    /// Generates a bot puzzle tag for display purposes.
    /// Format examples: "Alpha-Omega-42", "Quantum-Vortex-7", "Neutron-Prime-9"
    /// </summary>
    private void GenerateBotPuzzleTag(MatchmakingResult result, int slot)
    {
        string[] prefixes = { 
            "Alpha", "Beta", "Gamma", "Delta", "Epsilon",
            "Zeta", "Eta", "Theta", "Iota", "Kappa",
            "Lambda", "Mu", "Nu", "Xi", "Omicron",
            "Pi", "Rho", "Sigma", "Tau", "Upsilon",
            "Phi", "Chi", "Psi", "Omega"
        };
        
        string[] suffixes = { 
            "Prime", "Vortex", "Quantum", "Neutron", "Photon",
            "Electron", "Proton", "Ion", "Cyclone", "Tempest",
            "Storm", "Flare", "Burst", "Nova", "Supernova",
            "Anomaly", "Pulsar", "Nebula", "Galaxy", "Universe"
        };

        string prefix = prefixes[slot % prefixes.Length];
        string suffix = suffixes[(slot * 3) % suffixes.Length];
        int number = UnityEngine.Random.Range(1, 100);
        
        string botTag = $"{prefix}-{suffix}-{number}";
        result.playerNames[slot] = botTag;
        result.players[slot].puzzleTag = botTag;
        result.players[slot].isHuman = false;
        Debug.Log($"[Matchmaking] Generated bot puzzle tag for slot {slot}: {botTag}");
    }

    public bool IsAi(int slot)
    {
        return _lastResult != null && _lastResult.aiSlots.Contains(slot);
    }

    /// <summary>
    /// Integration point for a real backend response. Expected to append remote
    /// human slot indices (other than localSlot) to `humans`. 
    /// </summary>
    private void TryParseRemoteHumans(string responseBody, int totalSlots, int localSlot, List<int> humans)
    {
        if (string.IsNullOrEmpty(responseBody)) return;
        
        try
        {
            var json = JsonUtility.FromJson<MatchmakingResponse>(responseBody);
            if (json?.players != null)
            {
                foreach (var player in json.players)
                {
                    if (player.slot >= 0 && player.slot < totalSlots && player.slot != localSlot && !humans.Contains(player.slot))
                    {
                        humans.Add(player.slot);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Matchmaking] Failed to parse response: {e.Message}");
        }
    }

    /// <summary>
    /// Clean up resources on destroy
    /// </summary>
    private void OnDestroy()
    {
        // No event handlers to clean up in simplified version
    }
}

/// <summary>
/// Helper class for parsing matchmaking responses
/// </summary>
[Serializable]
public class MatchmakingResponse
{
    public BackendPlayerInfo[] players;
}

/// <summary>
/// Player info for backend matchmaking responses
/// </summary>
[Serializable]
public class BackendPlayerInfo
{
    public int slot;
    public string playerId;
}