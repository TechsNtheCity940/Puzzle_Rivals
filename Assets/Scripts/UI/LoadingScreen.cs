using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Loading screen that displays active player information during matchmaking.
/// Shows each player's puzzletag (name) and player card (placeholder or resolved).
/// Supports 4-player FFA format with human players and AI bot filling.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [Header("UI References")]
    public Transform cardContainer;           // Parent object to instantiate player cards under
    public GameObject playerCardPrefab;       // Prefab for the player card display
    public TextMeshProUGUI statusText;        // Main status message ("SEARCHING FOR PLAYERS...")
    public TextMeshProUGUI subText;           // Sub status ("2 real players, 2 AI bots")
    public Image background;                  // Optional background overlay

    [Header("Bot PuzzleTag Generation")]
    public bool autoFillBots = true;

    [Header("Card Layout")]
    public Vector2 cardSpacing = new Vector2(0, -40);
    public int cardWidth = 200;
    public Color humanCardColor = new Color(0f, 0.9f, 1f);
    public Color botCardColor = new Color(0.5f, 0.5f, 0.5f);

    private List<GameObject> _activeCards = new List<GameObject>();

    /// <summary>
    /// Creates a 4-player FFA lobby. Searches for 3 other players, fills with bots if not found.
    /// Uses NetManager's MatchmakingResult for consistency with matchmaking flow.
    /// </summary>
    public MatchmakingResult CreateFFALobby(string localPuzzleTag = null)
    {
        const int totalSlots = 4;
        var result = new MatchmakingResult { totalSlots = totalSlots };
        
        // Initialize player info for each slot
        string localTag = localPuzzleTag ?? "LocalPlayer";
        for (int i = 0; i < totalSlots; i++)
        {
            bool isHuman = i == 0;  // Local player is slot 0
            string tag = isHuman ? localTag : "";
            var playerInfo = new PlayerInfo(i, isHuman, tag, isHuman);
            result.players.Add(playerInfo);
            result.playerNames.Add(tag);
        }

        // Try to find other players (simulated - in real implementation this would
        // query a matchmaking service)
        int foundPlayers = 0;
        
        // Simulate finding other players with different success rates
        // In production, this would call actual matchmaking API
        if (autoFillBots)
        {
            // Random chance to find 0-3 additional players (simulating real matchmaking)
            int additionalPlayers = Random.Range(0, 4);
            
            // Mark which slots have humans vs bots
            for (int i = 1; i < totalSlots; i++)  // Start from 1, slot 0 is local
            {
                if (additionalPlayers > 0 && Random.value > 0.5f)
                {
                    // Found another human player
                    string tag = $"Player{i + 1}";
                    result.players[i] = new PlayerInfo(i, true, tag, false);
                    result.playerNames[i] = tag;
                    result.humanSlots.Add(i);
                    result.realPlayers++;
                    foundPlayers++;
                    additionalPlayers--;
                }
                else
                {
                    // Fill with AI bot
                    string botTag = GenerateBotPuzzleTag(i);
                    result.players[i] = new PlayerInfo(i, false, botTag, false);
                    result.playerNames[i] = botTag;
                    result.aiSlots.Add(i);
                }
            }
            
            // Slot 0 (local player)
            result.humanSlots.Add(0);
        }

        result.online = false;  // Local lobby is offline
        result.botCount = totalSlots - foundPlayers - 1;  // -1 for local player
        result.roomId = "LOCAL_" + Random.Range(1000, 9999);

        return result;
    }

    /// <summary>
    /// Generates a randomized puzzle tag for AI bots.
    /// Format examples: "Alpha-Omega-42", "Quantum-Vortex-7", "Neutron-Prime-9"
    /// </summary>
    /// <param name="slot">Slot index to generate tag for</param>
    /// <returns>A randomized puzzle tag string</returns>
    public string GenerateBotPuzzleTag(int slot)
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
        int number = Random.Range(1, 100);
        
        return $"{prefix}-{suffix}-{number}";
    }

    /// <summary>
    /// Creates and displays the loading screen with all player slot information.
    /// </summary>
    public void Show(int totalSlots, List<int> humanSlots, List<int> aiSlots, int localSlot)
    {
        gameObject.SetActive(true);
        ClearCards();

        // Create a card for each slot
        for (int i = 0; i < totalSlots; i++)
        {
            bool isHuman = humanSlots != null && humanSlots.Contains(i);
            bool isLocal = i == localSlot;
            CreatePlayerCard(i, isHuman, isLocal);
        }

        RefreshStatus(humanSlots?.Count ?? 0, aiSlots?.Count ?? totalSlots - (humanSlots?.Count ?? 0));
    }

    /// <summary>
    /// Creates the loading screen from a matchmaking result.
    /// Uses NetManager.MatchmakingResult for consistency.
    /// </summary>
    public void ShowFromMatchmakingResult(MatchmakingResult result, int localSlot = 0)
    {
        gameObject.SetActive(true);
        ClearCards();

        foreach (var playerInfo in result.players)
        {
            CreatePlayerCard(playerInfo);
        }

        RefreshStatus(result.humanSlots.Count, result.aiSlots.Count);
    }

    public void RefreshStatus(int realPlayerCount, int botCount)
    {
        if (statusText != null)
        {
            statusText.text = "SEARCHING FOR PLAYERS...";
            statusText.color = realPlayerCount > 0 ? Color.cyan : Color.yellow;
        }

        if (subText != null)
        {
            subText.text = $"{realPlayerCount} real{(realPlayerCount == 1 ? "" : "s")}  -  {botCount} AI bot{(botCount == 1 ? "" : "s")}";
        }
    }

    /// <summary>
    /// Refreshes status with a custom message during search (used for timeout indication)
    /// </summary>
    public void RefreshStatusWithTimeout(int realPlayerCount, int botCount, string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = realPlayerCount > 0 ? Color.cyan : Color.yellow;
        }

        if (subText != null)
        {
            subText.text = $"{realPlayerCount} real{(realPlayerCount == 1 ? "" : "s")}  -  {botCount} AI bot{(botCount == 1 ? "" : "s")}";
        }
    }

    public void ShowMatchFound(int realPlayerCount, int botCount)
    {
        if (statusText != null)
        {
            statusText.text = "MATCH FOUND";
            statusText.color = Color.green;
        }

        if (subText != null)
        {
            subText.text = $"{realPlayerCount} real{(realPlayerCount == 1 ? "" : "s")}  -  {botCount} AI bot{(botCount == 1 ? "" : "s")}";
        }
    }

    public void Hide()
    {
        ClearCards();
        gameObject.SetActive(false);
    }

    private void ClearCards()
    {
        foreach (var card in _activeCards)
        {
            if (card != null) Destroy(card);
        }
        _activeCards.Clear();
    }

    private void CreatePlayerCard(int slot, bool isHuman, bool isLocal)
    {
        if (playerCardPrefab == null || cardContainer == null) return;

        var cardObj = Instantiate(playerCardPrefab, cardContainer);
        var card = cardObj.GetComponent<PlayerCard>();

        // Determine display name
        string displayName;
        if (isLocal)
        {
            displayName = "YOU";
        }
        else if (isHuman)
        {
            displayName = $"Player {slot + 1}";
        }
        else
        {
            displayName = GenerateBotPuzzleTag(slot);
        }

        // Create placeholder player for card binding
        var player = new Player(slot, displayName, isLocal);
        player.isBot = !isHuman;

        // Set card visuals
        if (card != null)
        {
            var loadout = new CosmeticLoadout(); // Uses defaults
            card.Bind(player, loadout);
        }
        else
        {
            var textObj = cardObj.transform.Find("NameText");
            if (textObj != null && textObj.GetComponent<TextMeshProUGUI>() != null)
            {
                textObj.GetComponent<TextMeshProUGUI>().text = isLocal ? "YOU" : (isHuman ? $"Player {slot + 1}" : displayName);
            }
        }

        // Position the card
        var rt = cardObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = new Vector2(0, -slot * cardSpacing.y);
        }

        _activeCards.Add(cardObj);
    }

    /// <summary>
    /// Creates the loading screen from a matchmaking result.
    /// Uses player info to display proper puzzleTag names.
    /// Uses BindForLobby for lobby-style display.
    /// </summary>
    public void CreatePlayerCard(PlayerInfo info)
    {
        if (playerCardPrefab == null || cardContainer == null) return;

        var cardObj = Instantiate(playerCardPrefab, cardContainer);
        var card = cardObj.GetComponent<PlayerCard>();

        // Create player with puzzleTag
        var player = new Player(info.slot, info.puzzleTag, info.isLocal);
        player.isBot = !info.isHuman;

        // Create PlayerCardData for lobby-style binding
        var defaultRank = new PlayerRank(0, 1, 0, 100); // Level 1, no completed seasons
        var cardData = new PlayerCardData(player, defaultRank, !info.isHuman);
        cardData.puzzleTag = info.puzzleTag;
        cardData.displayName = info.isLocal ? info.puzzleTag + " (YOU)" : info.puzzleTag;

        // Set card visuals using BindForLobby
        if (card != null)
        {
            var defaultLoadout = new CosmeticLoadout(); // Uses defaults
            card.BindForLobby(cardData, defaultLoadout);
        }

        // Position the card
        var rt = cardObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = new Vector2(0, -info.slot * cardSpacing.y);
        }

        _activeCards.Add(cardObj);
    }

    public void AddSlotCard(int slot, bool isHuman, bool isLocal)
    {
        if (playerCardPrefab == null || cardContainer == null) return;

        var cardObj = Instantiate(playerCardPrefab, cardContainer);
        var rt = cardObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = new Vector2(0, -slot * cardSpacing.y);
        }

        _activeCards.Add(cardObj);
    }

    /// <summary>
    /// Sets display names for all slots (called after matchmaking completes).
    /// Used to show puzzleTags from the matchmaking result.
    /// </summary>
    public void SetPlayerNames(List<string> playerNames)
    {
        ClearCards();
        int totalSlots = playerNames != null ? playerNames.Count : 0;
        
        for (int i = 0; i < totalSlots; i++)
        {
            bool isHuman = i == 0; // First slot is always local player (human)
            CreatePlayerCard(i, isHuman, i == 0, playerNames[i]);
        }
    }

    /// <summary>
    /// Creates a player card with a custom display name (puzzleTag).
    /// Used for displaying matchmaking results with proper player names.
    /// Uses BindForLobby for lobby-style display.
    /// </summary>
    private void CreatePlayerCard(int slot, bool isHuman, bool isLocal, string displayName)
    {
        CreatePlayerCard(new PlayerInfo(slot, isHuman, displayName, isLocal));
    }

    private void Awake()
    {
        if (background == null)
        {
            background = GetComponent<Image>();
        }
    }

    private void OnDestroy()
    {
        ClearCards();
    }
}