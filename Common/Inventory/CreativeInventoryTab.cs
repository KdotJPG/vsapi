﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vintagestory.API.Common
{
    public class CreativeInventoryTab : InventoryGeneric
    {
        public int TabIndex;

        public CreativeInventoryTab(int quantitySlots, string className, string instanceId, ICoreAPI api) : base(quantitySlots, className, instanceId, api)
        {

        }

        public CreativeInventoryTab(int quantitySlots, string invId, ICoreAPI api) : base(quantitySlots, invId, api)
        {

        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            return 0;
        }

        protected override ItemSlot NewSlot(int slotId)
        {
            return new ItemSlotCreative(this);
        }


    }
}
