using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

#region OpenfortStaticData
public static class OpenfortStaticData
{
    public static string publishableKey = "pk_test_1ea91913-8d8b-5ca0-8b0f-3e1a5a97d970";
}
#endregion

#region Static PlayerData
public static class StaticPlayerData
{
    public static string playerID = "";

    public static string currencyAmount = "";

    public static string DisplayName = "";

    public static string EquipedWeapon = "";
    
    public static string OFplayer = "";
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