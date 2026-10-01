using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace OverdraftMod;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    internal const int CreditLimit = 100;
    private static RunState? _currentRun;

    public static void Initialize()
    {
        RunManager.Instance.RunStarted += run => _currentRun = run;
        new Harmony("tracy.sts2.overdraft").PatchAll(typeof(ModEntry).Assembly);
        Log.Info("OverdraftMod: shop balance floor -100, repay by earning gold, collect at act end.");
    }

    internal static bool IsEligible(Player player) =>
        ReferenceEquals(player.RunState, _currentRun) &&
        _currentRun?.Players.Count == 1 &&
        RunManager.Instance.NetService?.Type == NetGameType.Singleplayer &&
        player.RunState.CurrentRoom is MerchantRoom;

    internal static bool CanAfford(Player player, int cost) =>
        cost >= 0 && (long)player.Gold - cost >= -CreditLimit;

    internal static Task? CollectOverdueBalance()
    {
        if (_currentRun?.Players.Count != 1 ||
            RunManager.Instance.NetService?.Type != NetGameType.Singleplayer)
        {
            return null;
        }

        var player = _currentRun.Players[0];
        if (player.Gold >= 0 || player.Creature.IsDead)
        {
            return null;
        }

        Log.Info($"OverdraftMod: act ended with {player.Gold} gold; collecting debt.");
        return CreatureCmd.Kill(player.Creature, force: true);
    }
}

[HarmonyPatch(typeof(MerchantEntry), "get_EnoughGold")]
internal static class MerchantAffordabilityPatch
{
    private static readonly FieldInfo PlayerField = AccessTools.Field(typeof(MerchantEntry), "_player");

    private static void Postfix(MerchantEntry __instance, ref bool __result)
    {
        if (__result)
        {
            return;
        }

        var player = (Player)PlayerField.GetValue(__instance)!;
        if (ModEntry.IsEligible(player))
        {
            __result = ModEntry.CanAfford(player, __instance.Cost);
        }
    }
}

[HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.LoseGold))]
internal static class ShopPaymentPatch
{
    private static bool Prefix(decimal amount, Player player, GoldLossType goldLossType, ref Task __result)
    {
        var isShopPayment = goldLossType == GoldLossType.Spent && ModEntry.IsEligible(player);
        if (!isShopPayment)
        {
            if (player.Gold >= 0)
            {
                return true;
            }

            __result = Task.CompletedTask;
            return false;
        }

        var balanceAfterPurchase = (long)player.Gold - (int)amount;
        if (balanceAfterPurchase >= 0)
        {
            return true;
        }
        if (balanceAfterPurchase < -ModEntry.CreditLimit)
        {
            Log.Warn("OverdraftMod: blocked a shop payment beyond the -100 gold limit.");
            __result = Task.CompletedTask;
            return false;
        }

        SfxCmd.Play(PlayerCmd.goldSmallSfx);
        var history = player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId);
        if (history != null)
        {
            history.GoldSpent += (int)amount;
        }
        player.Gold = (int)balanceAfterPurchase;
        __result = Task.CompletedTask;
        return false;
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
internal static class NextActDebtPatch
{
    private static bool Prefix(ref Task __result)
    {
        var collection = ModEntry.CollectOverdueBalance();
        if (collection == null)
        {
            return true;
        }
        __result = collection;
        return false;
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.WinRun))]
internal static class FinalActDebtPatch
{
    private static bool Prefix(ref Task __result)
    {
        var collection = ModEntry.CollectOverdueBalance();
        if (collection == null)
        {
            return true;
        }
        __result = collection;
        return false;
    }
}

[HarmonyPatch(typeof(NTopBarGold), "OnFocus")]
internal static class GoldTooltipPatch
{
    private static bool Prefix(NTopBarGold __instance)
    {
        var loc = LocManager.Instance;
        if (loc == null || RunManager.Instance.NetService?.Type != NetGameType.Singleplayer)
        {
            return true;
        }

        var chinese = loc.Language == "zhs";
        loc.GetTable("static_hover_tips").MergeWith(new Dictionary<string, string>
        {
            ["OVERDRAFT_MOD.title"] = chinese ? "金币透支" : "Gold Overdraft",
            ["OVERDRAFT_MOD.description"] = chinese
                ? "商店购买可将金币降至 -100。获得金币会自动偿还欠款；本幕结束时金币仍为负数，你将死亡。"
                : "Shop purchases may lower gold to -100. Gold earned repays the debt. If gold is still negative at the end of the act, you die."
        });

        var tip = new HoverTip(
            new LocString("static_hover_tips", "OVERDRAFT_MOD.title"),
            new LocString("static_hover_tips", "OVERDRAFT_MOD.description"));
        NHoverTipSet.CreateAndShow(__instance, tip)?
            .SetGlobalPosition(__instance.GlobalPosition + new Vector2(0f, __instance.Size.Y + 20f));
        return false;
    }
}
