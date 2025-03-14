using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FarmlandDropsWithNutrients;

public class KeepNutrientsBehavior : BlockBehavior
{
    public KeepNutrientsBehavior(Block block) : base(block)
    {
    }

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack,
        ref EnumHandling handling)
    {
        world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position, byItemStack);
        if (!world.BlockAccessor.GetBlockEntity(blockSel.Position).IsAcceptableFarmland(out var blockEntity)) return true;
        var farmlandAttributes = ((TreeAttribute)byItemStack.Attributes).GetAttribute("farmlandAttributes") as TreeAttribute;
        this.FromTreeAttributes(farmlandAttributes, blockEntity, world);
        ((BlockEntity)blockEntity).MarkDirty();
        handling = EnumHandling.PreventDefault;
        return true;
    }

    public override ItemStack[] GetDrops(
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer,
        ref float dropChanceMultiplier,
        ref EnumHandling handling)
    {
        if (byPlayer != null && byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative ||
            !world.BlockAccessor.GetBlockEntity(pos).IsAcceptableFarmland(out var blockEntity))
        {
            return Array.Empty<ItemStack>();
        }
        ((BlockEntity) blockEntity).MarkDirty();
        handling = EnumHandling.PreventDefault;
        // Drop soil block if nutrients are default and moisture is 0
        var slowReleaseNutrients = blockEntity.GetField<float[]>("slowReleaseNutrients");
        if (!blockEntity.Nutrients.Zip(blockEntity.GetOriginalFertility(), (n, o) => Math.Abs(n - o) < 0.001).Any(b => b)
            && !slowReleaseNutrients.Any(n => n > 0.001))
        {
            return new[]
            {
                new ItemStack(world.GetBlock(new AssetLocation("game", "soil-" + block.FirstCodePart(2) + "-none")))
            };
        }
        
        var treeAttributes = new TreeAttribute();
        var farmlandAttributes = new TreeAttribute();
        this.ToTreeAttributes(farmlandAttributes, blockEntity);
        treeAttributes.SetAttribute("farmlandAttributes", farmlandAttributes);
        var farmBlockCode = block.CodeWithVariant("state", "dry");
        return new[]
        {
            new ItemStack(world.GetBlock(farmBlockCode), 1) { Attributes = treeAttributes }
        };
    }
    
    public void ToTreeAttributes(TreeAttribute tree, IFarmlandBlockEntity be)
    {
        var slowReleaseNutrients = be.GetField<float[]>("slowReleaseNutrients");
        var permaBoosts = be.GetField<string[]>("permaBoosts");
        var fertilizerOverlayStrength = be.GetField<Dictionary<string, float>>("fertilizerOverlayStrength");
        var originalFertility = be.GetOriginalFertility();
        tree.SetFloat("n", be.Nutrients[0]);
        tree.SetFloat("p", be.Nutrients[1]);
        tree.SetFloat("k", be.Nutrients[2]);
        if (slowReleaseNutrients != null)
        {
            tree.SetFloat("slowN", slowReleaseNutrients[0]);
            tree.SetFloat("slowP", slowReleaseNutrients[1]);
            tree.SetFloat("slowK", slowReleaseNutrients[2]);
        }
        tree.SetFloat("moistureLevel", be.MoistureLevel);
        tree.SetInt("originalFertilityN", originalFertility[0]);
        tree.SetInt("originalFertilityP", originalFertility[1]);
        tree.SetInt("originalFertilityK", originalFertility[2]);

        if (permaBoosts != null)
        {
            tree.SetStringArray("permaBoosts", permaBoosts.ToArray());
        }
        if (fertilizerOverlayStrength == null)
            return;
        var treeAttribute = new TreeAttribute();
        tree["fertilizerOverlayStrength"] = treeAttribute;
        foreach (KeyValuePair<string, float> keyValuePair in fertilizerOverlayStrength)
            treeAttribute.SetFloat(keyValuePair.Key, keyValuePair.Value);
    }
    public void FromTreeAttributes(TreeAttribute tree, IFarmlandBlockEntity be, IWorldAccessor worldForResolve)
    {
        be.Nutrients[0] = tree.GetFloat("n");
        be.Nutrients[1] = tree.GetFloat("p");
        be.Nutrients[2] = tree.GetFloat("k");

        if (tree.HasAttribute("slowN") && tree.HasAttribute("slowP") && tree.HasAttribute("slowK"))
        {
            be.SetField("slowReleaseNutrients", new float[3]
            {
                tree.GetFloat("slowN"),
                tree.GetFloat("slowP"),
                tree.GetFloat("slowK")
            });
        }
        be.SetOriginalFertility(tree, worldForResolve);
        be.SetField("moistureLevel", tree.GetFloat("moistureLevel"));

        var permaBoosts = tree.GetStringArray("permaBoosts");
        if (permaBoosts != null)
        {
            be.SetField("PermaBoosts", permaBoosts.ToHashSet());
        }
        
        if (!tree.HasAttribute("fertilizerOverlayStrength")) return;
        var dictionary = new Dictionary<string, float>();
        if (tree["fertilizerOverlayStrength"] is TreeAttribute fertilizerOverlayStrength)
        {
            foreach (var key in fertilizerOverlayStrength.Keys)
            {
                dictionary[key] = fertilizerOverlayStrength.GetFloat(key);
            }
        }
        be.SetField("fertilizerOverlayStrength", dictionary);
    }

    public static TreeAttribute DefaultFarmlandAttributes(BlockFarmland block)
    {
        var tree = new TreeAttribute();
        string key = block.LastCodePart();
        tree.SetFloat("n", (int) BlockEntityFarmland.Fertilities[key]);
        tree.SetFloat("p", (int) BlockEntityFarmland.Fertilities[key]);
        tree.SetFloat("k", (int) BlockEntityFarmland.Fertilities[key]);
        tree.SetFloat("slowN", 0);
        tree.SetFloat("slowP", 0);
        tree.SetFloat("slowK", 0);
        tree.SetInt("originalFertilityN", (int) BlockEntityFarmland.Fertilities[key]);
        tree.SetInt("originalFertilityP", (int) BlockEntityFarmland.Fertilities[key]);
        tree.SetInt("originalFertilityK", (int) BlockEntityFarmland.Fertilities[key]);
        return tree;
    }
}