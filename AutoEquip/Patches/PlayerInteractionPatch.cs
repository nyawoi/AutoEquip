using HarmonyLib;
using UnityEngine;

namespace AetharNet.Mods.ZumbiBlocks2.AutoEquip.Patches;

[HarmonyPatch(typeof(PlayerInteraction))]
public static class PlayerInteractionPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerInteraction.Interact))]
    public static bool AutoEquipServer(PlayerInteraction __instance, InteractableObject interactableObj)
    {
        // If the client is interacting with an object, run the original method
        if (MultiplayerController.instance.IsClient()) return true;
        
        // If the player cannot interact, or the object being interacted with does not exist, don't do anything
        if (!__instance.playerMain.CanInteract() || interactableObj == null) return false;
        
        // If the object being interacted with is not loot, run the original method
        if (interactableObj.GetInteractableID() != InteractableObject.ID.Loot) return true;
        
        // If the object is not DroppedLoot, don't do anything
        // This should not happen, as the object will always be DroppedLoot
        // This is here just in case anything happens in the future
        if (interactableObj is not DroppedLoot droppedLoot) return false;
        
        // Retrieve the appropriate inventory slot for the item
        var equipmentSlot = GetEquipmentSlot(droppedLoot.item);
        
        // If the item does not belong in any currently available slots, run the original method
        if (equipmentSlot == PlayerInventory.EquippedID.COUNT) return true;

        // Retrieve the current item in the assigned slot
        var currentItem = __instance.playerMain.inventory.GetEquipment((int)equipmentSlot);
        
        // Does the user wish to force-equip the item?
        var forceEquipItem = Input.GetKey(KeyCode.LeftShift);

        // If an item already exists in the slot, and the player does not wish to force-equip, run the original method
        if (!currentItem.GetDBItem().IsNone && !forceEquipItem) return true;
        
        // If the player wishes to force-equip the item,
        if (forceEquipItem)
        {
            // and the items are not of the same type
            if (currentItem.GetDBItem().itemID == droppedLoot.item.GetDBItem().itemID) return true;
            
            // find open space in the inventory to move the item
            var placeForCurrentItem = __instance.playerMain.inventory.FindPlaceFor(currentItem.id, currentItem.stackCount, true, false);
            
            // If there is no more space in the inventory,
            if (placeForCurrentItem == null)
            {
                // drop the currently equipped item
                __instance.playerMain.inventory.DropLoot(currentItem);
            }
            else
            {
                // otherwise, move the item into the inventory
                ItemContainer.PutLootIntoPlace(currentItem, placeForCurrentItem, __instance.playerMain.inventory.storage);
            }
        }
        
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
        
        // Retrieve the appropriate inventory slot for the item
        var equipmentSlot = GetEquipmentSlot(item);

        // If the item does not belong in any currently available slots, run the original method
        if (equipmentSlot == PlayerInventory.EquippedID.COUNT) return true;

        // Retrieve the current item in the assigned slot
        var currentItem = __instance.playerMain.inventory.GetEquipment((int)equipmentSlot);
        
        // Does the user wish to force-equip the item?
        var forceEquipItem = Input.GetKey(KeyCode.LeftShift);

        // If an item already exists in the slot, and the player does not wish to force-equip, run the original method
        if (!currentItem.GetDBItem().IsNone && !forceEquipItem) return true;
        
        // If the player wishes to force-equip the item,
        if (forceEquipItem)
        {
            // and the items are not of the same type
            if (currentItem.GetDBItem().itemID == item.GetDBItem().itemID) return true;
            
            // find open space in the inventory to move the item
            var placeForCurrentItem = __instance.playerMain.inventory.FindPlaceFor(currentItem.id, currentItem.stackCount, true, false);
            
            // If there is no more space in the inventory,
            if (placeForCurrentItem == null)
            {
                // drop the currently equipped item
                __instance.playerMain.inventory.DropLoot(currentItem);
            }
            else
            {
                // otherwise, move the item into the inventory
                ItemContainer.PutLootIntoPlace(currentItem, placeForCurrentItem, __instance.playerMain.inventory.storage);
            }
        }
        
        // Equip the item into its appropriate slot
        __instance.playerMain.inventory.SetEquipment(item, (int)equipmentSlot);
        
        // Do not call the original method; we've equipped the item and have no need to store it in inventory
        return false;
    }

    private static PlayerInventory.EquippedID GetEquipmentSlot(InventoryItem item)
    {
        // Get inventory slot for item based on its subtype
        // Return EquippedID.COUNT if unable to find appropriate slot
        var equipmentSlot = item.GetDBItem().GetSubType() switch
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
