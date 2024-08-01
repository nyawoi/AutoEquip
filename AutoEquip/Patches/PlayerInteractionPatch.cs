using HarmonyLib;
using UnityEngine;

namespace AetharNet.Mods.ZumbiBlocks2.AutoEquip.Patches;

[HarmonyPatch(typeof(PlayerInteraction))]
public static class PlayerInteractionPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("InteractWithLoot")]
    public static bool AutoEquipServer(PlayerInteraction __instance, InteractableObject interactableObj)
    {
        // If the client is interacting with an object, run the original method
        if (MultiplayerController.instance.IsClient()) return true;
        
        // Cast the InteractableObject instance to DroppedLoot
        var droppedLoot = (DroppedLoot)interactableObj;
        
        // If the handler ran into an issue, run the original method
        if (!SuccessfullyHandledAutoEquip(droppedLoot.item, __instance.playerMain.inventory, out var equipmentSlot)) return true;
        
        // Retrieve the piece of loot, removing it from the world
        var inventoryItem = LootController.instance.PullLoot(droppedLoot);
        
        // If it turns out there was no loot, don't do anything
        // This might occur in situations where two clients attempt to pick up the same piece of loot at the same time
        if (inventoryItem == null) return false;
        
        // Equip the item into its appropriate slot
        __instance.playerMain.inventory.SetEquipment(inventoryItem, (int)equipmentSlot);
        
        // Do not call the original method; we've equipped the item and have no need to store it in inventory
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerInteraction.GotLootFromServer))]
    public static bool AutoEquipClient(PlayerInteraction __instance, InventoryItem.ID itemID, int numericvalue)
    {
        // Create an instance of the item for use in the inventory
        var item = new InventoryItem(itemID);
        item.SetGenericNumericValue(numericvalue);
        
        // If the handler ran into an issue, run the original method
        if (!SuccessfullyHandledAutoEquip(item, __instance.playerMain.inventory, out var equipmentSlot)) return true;
        
        // Equip the item into its appropriate slot
        __instance.playerMain.inventory.SetEquipment(item, (int)equipmentSlot);
        
        // Do not call the original method; we've equipped the item and have no need to store it in inventory
        return false;
    }

    private static bool SuccessfullyHandledAutoEquip(InventoryItem item, PlayerInventory inventory, out PlayerInventory.EquippedID equipmentSlot)
    {
        // Retrieve the appropriate inventory slot for the item
        equipmentSlot = GetEquipmentSlot(item);

        // If the item does not belong in any currently available slots, cannot equip
        if (equipmentSlot == PlayerInventory.EquippedID.COUNT) return false;

        // Retrieve the current item in the assigned slot
        var currentItem = inventory.GetEquipment((int)equipmentSlot);
        
        // Does the user wish to force-equip the item?
        var forceEquipItem = Input.GetKey(KeyCode.LeftShift);

        // If an item already exists in the slot, and the player does not wish to force-equip, cannot equip
        if (!currentItem.IsNone && !forceEquipItem) return false;
        
        // If the player wishes to force-equip the item,
        if (forceEquipItem)
        {
            // and the items are not of the same type
            if (currentItem.id == item.id) return false;
            
            // find open space in the inventory to move the item
            var placeForCurrentItem = inventory.FindPlaceFor(currentItem.id, currentItem.stackCount, true, false);
            
            // If there is no more space in the inventory,
            if (placeForCurrentItem == null)
            {
                // drop the currently equipped item
                inventory.DropLoot(currentItem);
            }
            else
            {
                // otherwise, move the item into the inventory
                ItemContainer.PutLootIntoPlace(currentItem, placeForCurrentItem, inventory.storage);
            }
        }

        // The item can be equipped without issues
        return true;
    }

    private static PlayerInventory.EquippedID GetEquipmentSlot(InventoryItem item)
    {
        // Get inventory slot for item based on its subtype
        // Return EquippedID.COUNT if unable to find appropriate slot
        var equipmentSlot = item.GetDataBaseItem().GetSubType() switch
        {
            DatabaseItem.SubType.PrimaryGun => PlayerInventory.EquippedID.PrimaryGun,
            DatabaseItem.SubType.SecondaryGun => PlayerInventory.EquippedID.SecondaryGun,
            DatabaseItem.SubType.Melee => PlayerInventory.EquippedID.Melee,
            DatabaseItem.SubType.Throwable => PlayerInventory.EquippedID.Throwable,
            DatabaseItem.SubType.Healing => PlayerInventory.EquippedID.Healing,
            DatabaseItem.SubType.Food => PlayerInventory.EquippedID.Consumable,
            _ => PlayerInventory.EquippedID.COUNT
        };

        return equipmentSlot;
    }
}
