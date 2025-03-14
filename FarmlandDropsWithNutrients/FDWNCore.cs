using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace FarmlandDropsWithNutrients;

[HarmonyPatch]
public class FDWNCore : ModSystem
{
    public static ILogger Logger;
    public Harmony HarmonyInstance;
        
    public override void Start(ICoreAPI api)
    {
        Logger = Mod.Logger;
        base.Start(api);
        
        if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
            HarmonyInstance = new Harmony(Mod.Info.ModID);
            HarmonyInstance.PatchAll();
        }
        
        api.RegisterBlockBehaviorClass("KeepNutrientsBehavior", typeof(KeepNutrientsBehavior));
        api.RegisterCollectibleBehaviorClass("FarmlandWithNutrients", typeof(FarmlandWithNutrientsBehavior));
        GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append("farmlandAttributes");
    }
        
    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        if (!api.Side.IsServer()) return;
            
        foreach (var collectible in api.World.Collectibles.Where(c => c?.Code != null))
        {
            if (!collectible.IsAcceptableFarmland()) continue;
            Logger.Warning($"Adding behavior to {collectible.Code}");
            collectible.AddBehavior<FarmlandWithNutrientsBehavior>();
        }
            
        foreach (var block in api.World.Blocks.Where(b => b?.Code != null))
        {
            if (!block.IsAcceptableFarmland()) continue;
            Logger.Warning($"Adding behavior to {block.Code}");
            block.AddBehavior<KeepNutrientsBehavior>();
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.TryMergeStacks))]
    public static bool TryMergeStacksPrefix(CollectibleObject __instance, ItemStackMergeOperation op)
    {
        if (!op.SinkSlot.Itemstack.Collectible.IsAcceptableFarmland(out var block)) return true;
        if (!block.HasBehavior<KeepNutrientsBehavior>()) return true;
        op.MovableQuantity = __instance.GetMergableQuantity(op.SinkSlot.Itemstack, op.SourceSlot.Itemstack, op.CurrentPriority);
        if (op.MovableQuantity == 0) return false;
        if (!op.SinkSlot.CanTakeFrom(op.SourceSlot, op.CurrentPriority)) return false;
        op.MovedQuantity = GameMath.Min(op.SinkSlot.GetRemainingSlotSpace(op.SourceSlot.Itemstack), op.MovableQuantity, op.RequestedQuantity);
        var sourceFarmAttributes = op.SourceSlot.Itemstack.Attributes["farmlandAttributes"] as TreeAttribute;
        var sinkFarmAttributes = op.SinkSlot.Itemstack.Attributes["farmlandAttributes"] as TreeAttribute;
        //if (sinkFarmAttributes == null && sourceFarmAttributes == null) return true;
        //if (op.CurrentPriority == EnumMergePriority.AutoMerge &&
        //(sinkFarmAttributes == null || sourceFarmAttributes == null)) return true;
        //if (sinkFarmAttributes == null || sourceFarmAttributes == null) FDWNCore.Logger.Warning("One of the attributes is null");
        if (sinkFarmAttributes.IsZero()) sinkFarmAttributes = KeepNutrientsBehavior.DefaultFarmlandAttributes(block);
        if (sourceFarmAttributes.IsZero()) sourceFarmAttributes = KeepNutrientsBehavior.DefaultFarmlandAttributes(block);
        var mergedAttributes = sourceFarmAttributes.MergeWithFarmlandAttributes(sinkFarmAttributes, op.MovedQuantity, op.SinkSlot.StackSize);
        ((TreeAttribute)op.SinkSlot.Itemstack.Attributes).SetAttribute("farmlandAttributes", mergedAttributes);
        return true;
    }
    
    public override void Dispose() {
        HarmonyInstance?.UnpatchAll(Mod.Info.ModID);
    }
}