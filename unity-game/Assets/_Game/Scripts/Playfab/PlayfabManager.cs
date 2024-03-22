using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

#region GAME_CONSTANTS
public static class GameConstants
{
    // TODO we should fetch this from server
    public const int GameChainId = 4337; // BEAM
    public const string GameChainIdHex = "0x10F1"; // BEAM
}
#endregion

#region OpenfortStaticData
public static class OFStaticData
{
    public const string PublishableKey = "***REMOVED***";

    // PlayFab Keys
    public const string OFplayerKey = "OFplayer";
    public const string OFaddressKey = "address";
    public const string OFownerAddressKey = "ownerAddress";
    public const string Web3AuthCompletedOnceKey = "Web3AuthCompletedOnce";

    // PlayFab Key Values
    public static string OFplayerValue = "";
    public static string OFownerAddressValue = "";
}
#endregion

#region PlayerPrefs Static Data
public static class PPStaticData
{
    public const string RememberMeKey = "RememberMe";
    public const string CustomIdKey = "CustomID";
    public const string AppleSubjectIdKey = "AppleSubjectId";
    public const string GooglePlayGamesPlayerIdKey = "GooglePlayGamesPlayerId";
    public const string LastPlayerKey = "LastPlayer";
}
#endregion

#region Static PlayerData
public static class StaticPlayerData
{
    public static string playerID = "";

    public static string currencyAmount = "";

    public static string DisplayName = "";

    public static string EquipedWeapon = "";
}
#endregion

#region playerData
public class ProtectedPlayerData
{
    public string WeaponEquipedID;
    public string Score;
}

public class ExchangeOffer
{
    public decimal InputCurrencyAmount { get; set; }
    public decimal Id { get; set; }
    public List<decimal> OutputCollectionItemIds { get; set; }
}

public struct CollectionItem
{
    public int ID;
    public int count;
}

public class LeaderBoardPlayerData
{
    public string playerName;
    public int Score;

}

#endregion