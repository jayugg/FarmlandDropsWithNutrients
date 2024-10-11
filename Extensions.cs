using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace FarmlandDropsWithNutrients;

public static class Extensions
{
    #region treeAttributes

    public static TreeAttribute MergeWithFarmlandAttributes(this TreeAttribute farmlandAttributes, TreeAttribute farmlandAttributes2, int movedQuantity, int sinkSlotStackSize)
    {
        var mergedAttributes = new TreeAttribute();
        var mergedN = WeightedAverage(farmlandAttributes.GetFloat("n"), farmlandAttributes2.GetFloat("n"),movedQuantity, sinkSlotStackSize);
        var mergedP = WeightedAverage(farmlandAttributes.GetFloat("p"), farmlandAttributes2.GetFloat("p"),movedQuantity, sinkSlotStackSize);
        var mergedK = WeightedAverage(farmlandAttributes.GetFloat("k"), farmlandAttributes2.GetFloat("k"),movedQuantity, sinkSlotStackSize);
        var mergedSlowN = WeightedAverage(farmlandAttributes.GetFloat("slowN"), farmlandAttributes2.GetFloat("slowN"),movedQuantity, sinkSlotStackSize);
        var mergedSlowP = WeightedAverage(farmlandAttributes.GetFloat("slowP"), farmlandAttributes2.GetFloat("slowP"),movedQuantity, sinkSlotStackSize);
        var mergedSlowK = WeightedAverage(farmlandAttributes.GetFloat("slowK"), farmlandAttributes2.GetFloat("slowK"),movedQuantity, sinkSlotStackSize);
        var originalN = WeightedAverage(farmlandAttributes.GetInt("originalFertilityN"), farmlandAttributes2.GetInt("originalFertilityN"),movedQuantity, sinkSlotStackSize);
        var originalP = WeightedAverage(farmlandAttributes.GetInt("originalFertilityP"), farmlandAttributes2.GetInt("originalFertilityP"),movedQuantity, sinkSlotStackSize);
        var originalK = WeightedAverage(farmlandAttributes.GetInt("originalFertilityK"), farmlandAttributes2.GetInt("originalFertilityK"),movedQuantity, sinkSlotStackSize);
        var mergedMoistureLevel = WeightedAverage(farmlandAttributes.GetFloat("moistureLevel"), farmlandAttributes2.GetFloat("moistureLevel"),movedQuantity, sinkSlotStackSize);
        mergedAttributes.SetFloat("n", mergedN);
        mergedAttributes.SetFloat("p", mergedP);
        mergedAttributes.SetFloat("k", mergedK);
        mergedAttributes.SetFloat("slowN", mergedSlowN);
        mergedAttributes.SetFloat("slowP", mergedSlowP);
        mergedAttributes.SetFloat("slowK", mergedSlowK);
        mergedAttributes.SetInt("originalFertilityN", (int) originalN);
        mergedAttributes.SetInt("originalFertilityP", (int) originalP);
        mergedAttributes.SetInt("originalFertilityK", (int) originalK);
        mergedAttributes.SetFloat("moistureLevel", mergedMoistureLevel);
        var permaBoosts1 = farmlandAttributes.GetStringArray("permaBoosts") ?? Array.Empty<string>();
        var permaBoosts2 = farmlandAttributes2.GetStringArray("permaBoosts") ?? Array.Empty<string>();
        var permaBoosts = permaBoosts1.Concat(permaBoosts2).ToArray();
        mergedAttributes.SetStringArray("permaBoosts", permaBoosts);
        var fertilizerOverlayStrength1 = farmlandAttributes.GetAttribute("fertilizerOverlayStrength") as TreeAttribute ?? new TreeAttribute();
        var fertilizerOverlayStrength2 = farmlandAttributes2.GetAttribute("fertilizerOverlayStrength") as TreeAttribute ?? new TreeAttribute();
        var mergedFertilizerOverlayStrengthTree = MergeFertilizerOverlayStrength(fertilizerOverlayStrength1, fertilizerOverlayStrength2, movedQuantity, sinkSlotStackSize);
        mergedAttributes["fertilizerOverlayStrength"] = mergedFertilizerOverlayStrengthTree;
        return mergedAttributes;
    }
    
    public static float WeightedAverage(float a, float b, float weightA, float weightB)
    {
        return (a * weightA + b * weightB) / (weightA + weightB);
    }
    
    public static Dictionary<string, float> MergeDictionaries(Dictionary<string, float> dict1, Dictionary<string, float> dict2, int mergedQuantity, int sinkStackSize)
    {
        var sumDict = new Dictionary<string, float>();
        
        // Get all unique keys from both dictionaries
        var allKeys = new HashSet<string>(dict1.Keys);
        allKeys.UnionWith(dict2.Keys);

        // Initialize sumDict with keys from both dictionaries
        foreach (var key in allKeys)
        {
            dict1.TryGetValue(key, out var value1);
            dict2.TryGetValue(key, out var value2);
            sumDict[key] = value1 * sinkStackSize + value2 * mergedQuantity;
        }
        // Calculate weighted average
        var avgDict = new Dictionary<string, float>();
        foreach (var kvp in sumDict)
        {
            avgDict[kvp.Key] = sumDict[kvp.Key] / (mergedQuantity + sinkStackSize);
        }
        return avgDict;
    }

    public static TreeAttribute MergeFertilizerOverlayStrength(TreeAttribute attr1, TreeAttribute attr2, int mergedQuantity, int sinkStackSize)
    {
        var mergedDict = MergeDictionaries(
            attr1.ToDictionary(k => k.Key, k => attr1.GetFloat(k.Key)),
            attr2.ToDictionary(k => k.Key, k => attr2.GetFloat(k.Key)),
            sinkStackSize,
            mergedQuantity
        );
        var mergedTree = new TreeAttribute();
        foreach (var kvp in mergedDict)
        {
            mergedTree.SetFloat(kvp.Key, kvp.Value);
        }
        return mergedTree;
    }
    
    public static bool IsZero(this TreeAttribute farmlandAttributes)
    {
        return farmlandAttributes == null ||
               (farmlandAttributes.GetFloat("n") == 0 &&
                farmlandAttributes.GetFloat("p") == 0 &&
                farmlandAttributes.GetFloat("k") == 0 &&
                farmlandAttributes.GetFloat("slowN") == 0 &&
                farmlandAttributes.GetFloat("slowP") == 0 &&
                farmlandAttributes.GetFloat("slowK") == 0 &&
                farmlandAttributes.GetFloat("moistureLevel") == 0 &&
                farmlandAttributes.GetFloat("originalFertilityN") == 0 &&
                farmlandAttributes.GetFloat("originalFertilityP") == 0 &&
                farmlandAttributes.GetFloat("originalFertilityK") == 0);
    }

    #endregion

    #region farmland

    public static bool IsAcceptableFarmland(this BlockEntity be, out IFarmlandBlockEntity farmBe)
    {
        farmBe = be as IFarmlandBlockEntity;
        return be.IsAcceptableFarmland();
    }
    
    public static bool IsAcceptableFarmland(this BlockEntity be)
    {
        return be is IFarmlandBlockEntity && be.Block.Code.Path.Contains("farmland");
    }
    
    public static bool IsAcceptableFarmland(this CollectibleObject block, out BlockFarmland farmBlock)
    {
        farmBlock = block as BlockFarmland;
        return block.IsAcceptableFarmland();
    }
    
    public static bool IsAcceptableFarmland(this CollectibleObject block)
    {
        return block is BlockFarmland && block.Code.Path.Contains("farmland");
    }
    
    public static int[] GetOriginalFertility(this IFarmlandBlockEntity be)
    {
        var farmlandAttributes = new TreeAttribute();
        ((BlockEntity) be).ToTreeAttributes(farmlandAttributes);
        int[] originalFertility = new int[3];
        originalFertility[0] = farmlandAttributes.GetInt("originalFertilityN");
        originalFertility[1] = farmlandAttributes.GetInt("originalFertilityP");
        originalFertility[2] = farmlandAttributes.GetInt("originalFertilityK");
        return originalFertility;
    }
    
    public static void SetOriginalFertility(this IFarmlandBlockEntity be, TreeAttribute tree, IWorldAccessor worldForResolve)
    {
        var farmlandAttributes = new TreeAttribute();
        ((BlockEntity)be).ToTreeAttributes(farmlandAttributes);
        farmlandAttributes.SetInt("originalFertilityN", tree.GetInt("originalFertilityN"));
        farmlandAttributes.SetInt("originalFertilityP", tree.GetInt("originalFertilityP"));
        farmlandAttributes.SetInt("originalFertilityK", tree.GetInt("originalFertilityK"));
        ((BlockEntity)be).FromTreeAttributes(farmlandAttributes, worldForResolve);
    }

    #endregion
    
    public static void AddBehavior<T>(this CollectibleObject collectible) where T : CollectibleBehavior
    {
        var behavior = (T) Activator.CreateInstance(typeof(T), collectible);
        collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
    }
    
    public static void AddBehavior<T>(this Block block) where T : BlockBehavior
    {
        var behavior = (T) Activator.CreateInstance(typeof(T), block);
        block.BlockBehaviors = block.BlockBehaviors.Append(behavior);
    }
}