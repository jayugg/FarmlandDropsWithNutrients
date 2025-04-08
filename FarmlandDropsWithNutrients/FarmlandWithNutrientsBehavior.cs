using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace FarmlandDropsWithNutrients;

public class FarmlandWithNutrientsBehavior : CollectibleBehavior
{
    public FarmlandWithNutrientsBehavior(CollectibleObject collObj) : base(collObj)
    {
    }
    
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        if (HasDefaultNutrients(inSlot.Itemstack)) return;
        var farmlandAttributes = inSlot.Itemstack.Attributes.GetTreeAttribute("farmlandAttributes");
        dsc.AppendLine(Lang.Get("farmland-nutrientlevels", Math.Round(farmlandAttributes.GetFloat("n"), 1), Math.Round(farmlandAttributes.GetFloat("p"), 1), Math.Round(farmlandAttributes.GetFloat("k"), 1)));
        var slowN = (float) Math.Round(farmlandAttributes.GetFloat("slowN"), 1);
        var slowP = (float) Math.Round(farmlandAttributes.GetFloat("slowP"), 1);
        var slowK = (float) Math.Round(farmlandAttributes.GetFloat("slowK"), 1);
        if (slowN > 0.0 || slowP > 0.0 || slowK > 0.0)
        {
            var values = new List<string>();
            if (slowN > 0.0)
                values.Add(Lang.Get("+{0}% N", slowN));
            if (slowP > 0.0)
                values.Add(Lang.Get("+{0}% P", slowP));
            if (slowK > 0.0)
                values.Add(Lang.Get("+{0}% K", slowK));
            dsc.AppendLine(Lang.Get("farmland-activefertilizer", string.Join(", ", values)));
        }
        var moisture = (float) Math.Round(farmlandAttributes.GetFloat("moistureLevel") * 100.0, 0);
        var moistureColor = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int) Math.Min(99f, moisture)]);
        dsc.AppendLine(Lang.Get("farmland-moisture", moistureColor, moisture));
    }

    private static bool HasDefaultNutrients(ItemStack stack)
    {
        var farmlandAttributes = stack.Attributes.GetTreeAttribute("farmlandAttributes");
        return farmlandAttributes == null;
    }
}