using System.Reflection;
using UnityEngine;
using ValheimTooler.Utils;

namespace ValheimTooler.Core
{
    public static class InventoryCleaner
    {
        public static int ClearLocalPlayer()
        {
            if (Player.m_localPlayer == null)
            {
                return 0;
            }

            return Clear(Player.m_localPlayer.GetInventory());
        }

        public static int ClearLocalPlayerAndOpenContainer()
        {
            return ClearLocalPlayer() + Clear(CheatStatus.GetOpenContainerInventory());
        }

        public static int ClearNearbyDropped(float radius)
        {
            if (Player.m_localPlayer == null)
            {
                return 0;
            }

            ItemDrop[] drops = Object.FindObjectsOfType<ItemDrop>();
            if (drops == null)
            {
                return 0;
            }

            Vector3 origin = Player.m_localPlayer.transform.position;
            int count = 0;
            foreach (ItemDrop drop in drops)
            {
                if (drop == null || drop.m_itemData == null || !drop.m_itemData.m_cheated)
                {
                    continue;
                }

                if (global::Utils.DistanceXZ(origin, drop.transform.position) > radius)
                {
                    continue;
                }

                drop.m_itemData.m_cheated = false;
                PersistDrop(drop);
                count++;
            }

            return count;
        }

        public static int Clear(Inventory inventory)
        {
            if (inventory == null)
            {
                return 0;
            }

            var items = inventory.GetAllItems();
            if (items == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ItemDrop.ItemData item in items)
            {
                if (item != null && item.m_cheated)
                {
                    item.m_cheated = false;
                    count++;
                }
            }

            if (count > 0)
            {
                MethodInfo changed = typeof(Inventory).GetMethod("Changed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (changed != null)
                {
                    changed.Invoke(inventory, null);
                }
            }

            return count;
        }

        private static void PersistDrop(ItemDrop drop)
        {
            ZNetView netView = drop.GetComponent<ZNetView>();
            if (netView != null && netView.IsValid() && !netView.IsOwner())
            {
                netView.ClaimOwnership();
            }

            drop.CallMethod("Save");
        }
    }
}
