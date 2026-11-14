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

    // Player info structure for matchmaking results
    public struct PlayerInfo
    {
        public int slot;
        public bool isHuman;
        public string puzzleTag;
        public bool isLocal;
        
        public PlayerInfo(int s, bool human, string tag, bool local)
        {
            slot = s;
            isHuman = human;
            puzzleTag = tag;
            isLocal = local;
        }
    }

    // Matchmaking result struct containing all slot information
    public struct MatchmakingResult
    {
        public List<int> humanSlots;      // Slots occupied by human players
        public List<int> aiSlots;         // Slots filled by AI bots
        public List<PlayerInfo> players;  // Full player info for each slot
        public string roomId;             // Matchmaking room identifier
        
        public MatchmakingResult(int totalSlots)
        {
            humanSlots = new List<int>();
            aiSlots = new List<int>();
            players = new List<PlayerInfo>(new PlayerInfo[totalSlots]);
            roomId = "";
        }
    }

    /// <summary>
    /// Creates a 4-player FFA lobby. Searches for 3 other players, fills with bots if not found.
    /// </summary>
    public MatchmakingResult CreateFFALobby(string localPuzzleTag = null)
    {
        const int totalSlots = 4;
        var result = new MatchmakingResult(totalSlots);
        
        // Initialize player info for each slot
        for (int i = 0; i < totalSlots; i++)
        {
            result.players[i] = new PlayerInfo(i, false, "", false);
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
            for (int i = 0; i < totalSlots; i++)
            {
                bool isLocal = (i == 0);  // Local player is slot 0
                
                if (isLocal)
                {
                    // Local player
                    result.players[i] = new PlayerInfo(i, true, localPuzzleTag ?? "LocalPlayer", true);
                    result.humanSlots.Add(i);
                    foundPlayers++;
                }
                else if (additionalPlayers > 0 && Random.value > 0.5f)
                {
                    // Found another human player
                    string tag = $"Player{i + 1}";
                    result.players[i] = new PlayerInfo(i, true, tag, false);
                    result.humanSlots.Add(i);
                    result.aiSlots.Add(i);
                    foundPlayers++;
                    additionalPlayers--;
                }
                else
                {
                    // Fill with AI bot
                    string botTag = GenerateBotPuzzleTag();
                    result.players[i] = new PlayerInfo(i, false, botTag, false);
                    result.aiSlots.Add(i);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Generates a randomized puzzle tag for AI bots.
    /// Format examples: "Alpha-Omega-42", "Quantum-Vortex-7", "Neutron-Prime-9"
    /// </summary>
    public string GenerateBotPuzzleTag()
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

        string prefix = prefixes[Random.Range(0, prefixes.Length)];
        string suffix = suffixes[Random.Range(0, suffixes.Length)];
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

        RefreshStatus(humanSlots?.Count ?? 0, aiSlots?.Count ?? 0);
    }

    /// <summary>
    /// Creates the loading screen from a matchmaking result.
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
            displayName = "AI Bot";
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
                textObj.GetComponent<TextMeshProUGUI>().text = isLocal ? "YOU" : (isHuman ? $"Player {slot + 1}" : "AI Bot");
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
    /// Creates a player card from player info (used for proper puzzleTag display).
    /// </summary>
    public void CreatePlayerCard(PlayerInfo info)
    {
        if (playerCardPrefab == null || cardContainer == null) return;

        var cardObj = Instantiate(playerCardPrefab, cardContainer);
        var card = cardObj.GetComponent<PlayerCard>();

        // Create player with puzzleTag
        var player = new Player(info.slot, info.puzzleTag, info.isLocal);
        player.isBot = !info.isHuman;

        // Set card visuals
        if (card != null)
        {
            var loadout = new CosmeticLoadout(); // Uses defaults
            card.Bind(player, loadout);
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