using System;
using System.Collections.Generic;
using UnityEngine;

// The 17 puzzle categories used by the ranked arena. Score Attack remains a
// separate arena mode and is intentionally not part of this ranked rotation.
public enum RankedArenaPuzzleType
{
    GridLogic,        // CollapseGridOverlay
    PatternRecall,    // MirrorMazeOverlay
    PipePulse,        // PipePulseOverlay
    ComboRush,        // ComboRushOverlay
    RuleBreaker,      // RuleBreakerOverlay
    LinkLock,         // LinkLockOverlay
    MazeRush,         // MazeRushOverlay
    CircuitScramble,  // CircuitScrambleOverlay
    ButtonMash,       // ButtonMashOverlay
    TargetShoot,      // TargetShootOverlay
    GridShift,        // GridShiftOverlay
    CardMatch,        // CardMatchOverlay
    BinaryBurst,      // BinaryBurstOverlay
    SignalSync,       // SignalSyncOverlay
    HexPulse,         // HexPulseOverlay
    NeonNudge,        // NeonNudgeOverlay
    RuneTrace         // RuneTraceOverlay
}

// Static catalogue describing the available puzzle types and how each maps to
// its overlay prefab (by name fragment, matching the manager's lookup).
public static class RankedArena
{
    public static readonly List<RankedArenaPuzzleType> PuzzleTypes = new List<RankedArenaPuzzleType>
    {
        RankedArenaPuzzleType.GridLogic,
        RankedArenaPuzzleType.PatternRecall,
        RankedArenaPuzzleType.PipePulse,
        RankedArenaPuzzleType.ComboRush,
        RankedArenaPuzzleType.RuleBreaker,
        RankedArenaPuzzleType.LinkLock,
        RankedArenaPuzzleType.MazeRush,
        RankedArenaPuzzleType.CircuitScramble,
        RankedArenaPuzzleType.ButtonMash,
        RankedArenaPuzzleType.TargetShoot,
        RankedArenaPuzzleType.GridShift,
        RankedArenaPuzzleType.CardMatch,
        RankedArenaPuzzleType.BinaryBurst,
        RankedArenaPuzzleType.SignalSync,
        RankedArenaPuzzleType.HexPulse,
        RankedArenaPuzzleType.NeonNudge,
        RankedArenaPuzzleType.RuneTrace
    };

    // Fragment found in the overlay prefab name for each type.
    public static string PrefabNameFragment(RankedArenaPuzzleType type)
    {
        switch (type)
        {
            case RankedArenaPuzzleType.GridLogic: return "Collapse";
            case RankedArenaPuzzleType.PatternRecall: return "Mirror";
            case RankedArenaPuzzleType.PipePulse: return "Pipe";
            case RankedArenaPuzzleType.ComboRush: return "Combo";
            case RankedArenaPuzzleType.RuleBreaker: return "Rule";
            case RankedArenaPuzzleType.LinkLock: return "Link";
            case RankedArenaPuzzleType.MazeRush: return "MazeRush";
            case RankedArenaPuzzleType.CircuitScramble: return "Circuit";
            case RankedArenaPuzzleType.ButtonMash: return "Button";
            case RankedArenaPuzzleType.TargetShoot: return "Target";
            case RankedArenaPuzzleType.GridShift: return "GridShift";
            case RankedArenaPuzzleType.CardMatch: return "Card";
            case RankedArenaPuzzleType.BinaryBurst: return "Binary";
            case RankedArenaPuzzleType.SignalSync: return "Signal";
            case RankedArenaPuzzleType.HexPulse: return "Hex";
            case RankedArenaPuzzleType.NeonNudge: return "Nudge";
            case RankedArenaPuzzleType.RuneTrace: return "Rune";
            default: return "";
        }
    }
}

// Converts an integer to Roman numerals (1-100 range)
public static class RomanNumerals
{
    public static string ToRoman(int number)
    {
        if (number < 1 || number > 100) return number.ToString();
        
        string[] numerals = {
            "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
            "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX",
            "XXI", "XXII", "XXIII", "XXIV", "XXV", "XXVI", "XXVII", "XXVIII", "XXIX", "XXX",
            "XXXI", "XXXII", "XXXIII", "XXXIV", "XXXV", "XXXVI", "XXXVII", "XXXVIII", "XXXIX", "XL",
            "XLI", "XLII", "XLIII", "XLIV", "XLV", "XLVI", "XLVII", "XLVIII", "XLIX", "L",
            "LI", "LII", "LIII", "LIV", "LV", "LVI", "LVII", "LVIII", "LIX", "LX",
            "LXI", "LXII", "LXIII", "LXIV", "LXV", "LXVI", "LXVII", "LXVIII", "LXIX", "LXX",
            "LXXI", "LXXII", "LXXIII", "LXXIV", "LXXV", "LXXVI", "LXXVII", "LXXVIII", "LXXIX", "LXXX",
            "LXXXI", "LXXXII", "LXXXIII", "LXXXIV", "LXXXV", "LXXXVI", "LXXXVII", "LXXXVIII", "LXXXIX", "XC",
            "XCI", "XCII", "XCIII", "XCIV", "XCV", "XCVI", "XCVII", "XCVIII", "XCIX", "C"
        };
        return numerals[number];
    }
}

// Player rank data stored on the player profile. The Roman numeral represents
// completed seasons (only updates when full season pass is finished), and
// level represents current season pass tier.
[Serializable]
public struct PlayerRank
{
    public int romanNumeral;  // Completed seasons (Roman numeral representation)
    public int level;         // Current season pass level
    public int exp;           // Current EXP toward next level
    public int expToNext;     // EXP needed to reach next level

    public string GetDisplayString()
    {
        return $"{RomanNumerals.ToRoman(romanNumeral)}-{level}";
    }

    public PlayerRank(int roman, int lvl, int currentExp, int requiredExp)
    {
        romanNumeral = roman;
        level = lvl;
        exp = currentExp;
        expToNext = requiredExp;
    }
}

// Light-weight competitor model. In the overlay-based design players are
// represented by data + a player card (no world-space avatar), so Player is a
// plain serializable class rather than a MonoBehaviour.
[Serializable]
public class Player
{
    public int playerId;
    public string puzzleTag;      // Player's custom name (displayed on nameplate)
    public string playerName;     // Legacy field, set equal to puzzleTag
    public bool isLocal;
    public bool isBot;          // true = AI bot filling a lobby slot; false = real human
    public bool isEliminated;
    public bool hasBomb;
    public int rankPoints;
    public int puzzlesSolved;
    public Color color = Color.white;
    public PlayerRank playerRank;  // Season pass progress and rank

    public Player() { }

    public Player(int id, string name, bool local)
    {
        playerId = id;
        puzzleTag = name;
        playerName = name;
        isLocal = local;
    }

    // Convenience constructor for testing
    public Player(int id, string puzzleTagName, bool local, PlayerRank rank)
    {
        playerId = id;
        puzzleTag = puzzleTagName;
        playerName = puzzleTagName;
        isLocal = local;
        playerRank = rank;
    }
}

// Data container for player card display. Separates display data from the
// game-ready Player model, allowing for clean UI binding.
[Serializable]
public class PlayerCardData
{
    // Core identity
    public string puzzleTag;
    public PlayerRank rank;
    public bool isLocal;
    public bool isBot;

    // Cosmetics
    public Sprite background;       // Full card background image
    public Sprite frame;            // Frame around the card
    public Sprite avatar;           // Character avatar
    public Sprite emblem;           // Corner emblem
    public Sprite banner;           // Bottom banner strip
    public Sprite nameplate;        // Nameplate background

    // Display helpers
    public string displayName;      // What to show in name text (puzzleTag + "(YOU)" for local)
    public Color cardColor = Color.white;

    public PlayerCardData() { }

    public PlayerCardData(Player player, PlayerRank rank, bool isBot = false)
    {
        puzzleTag = player.puzzleTag;
        this.rank = rank;
        this.isLocal = player.isLocal;
        this.isBot = isBot;
        displayName = player.isLocal ? $"{puzzleTag} (YOU)" : puzzleTag;
    }

    public string GetRankDisplay()
    {
        return rank.GetDisplayString();
    }
}