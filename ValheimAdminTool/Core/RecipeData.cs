using System;
using System.Collections.Generic;

namespace ValheimAdminTool.Core
{
    // Items a player naturally finds in each biome, taken from the Valheim wiki
    // (Template:MaterialsNav, Materials, Trophies, and each biome page) and matched
    // to the game's own item names. Vendor goods come from the live Trader scan instead.
    public static class RecipeData
    {
        private static readonly Dictionary<RecipeBiome, string[]> s_biomeItems = new Dictionary<RecipeBiome, string[]>
        {
            {
                RecipeBiome.Meadows, new[]
                {
                    "wood", "stone", "flint", "resin", "feathers", "finewood", "dandelion",
                    "deerhide", "deer_meat", "deer_meat_cooked", "honey", "queenbee", "leatherscraps",
                    "boar_meat", "boar_meat_cooked", "necktail", "necktailgrilled", "mushroomcommon",
                    "raspberries", "beechseeds", "birchseeds", "oakseeds",
                    "trophy_deer", "trophy_boar", "trophy_neck", "trophy_eikthyr", "hardantler",
                    "amber", "amberpearl", "ruby", "silvernecklace", "coins",
                    "animal_fish1", "animal_fish2"
                }
            },
            {
                RecipeBiome.BlackForest, new[]
                {
                    "copperore", "tinore", "copper", "tin", "bronze", "bronzenails", "roundlog",
                    "pinecone", "fircone", "bjornhide", "bjornpaw", "bjorn_meat", "bjorn_meat_cooked",
                    "blueberries", "carrot", "carrotseeds", "ectoplasm", "greydwarfeye", "surtlingcore",
                    "thistle", "trollhide", "mushroomyellow", "bonefragments", "coal", "pukeberries",
                    "ancientseed", "cryptkey",
                    "trophy_greydwarf", "trophy_greydwarfbrute", "trophy_greydwarfshaman", "trophy_skeleton",
                    "trophy_skeletonpoison", "trophy_ghost", "trophy_troll", "trophy_bjorn",
                    "trophy_skeleton_hildir", "trophy_elder",
                    "animal_fish5"
                }
            },
            {
                RecipeBiome.Swamp, new[]
                {
                    "ironscrap", "iron", "ironore", "elderbark", "bloodbag", "chain", "entrails", "guck",
                    "ironnails", "ooze", "root", "sharpeningstone", "turnip", "turnipseeds",
                    "witheredbone", "writhanroots", "wishbone",
                    "trophy_abomination", "trophy_blob", "trophy_draugr", "trophy_draugrelite", "trophy_leech",
                    "trophy_surtling", "trophy_wraith", "trophy_writhan", "trophy_bonemass",
                    "animal_fish6"
                }
            },
            {
                RecipeBiome.Mountains, new[]
                {
                    "silverore", "silver", "obsidian", "crystal", "wolfclaw", "wolfhairbundle", "freezegland",
                    "onion", "onionseeds", "jutered", "wolffang", "wolfpelt", "wolf_meat", "wolf_meat_cooked",
                    "dragonegg", "dragontear",
                    "trophy_wolf", "trophy_hatchling", "trophy_fenring", "trophy_sgolem", "trophy_cultist",
                    "trophy_ulv", "trophy_blob_frost", "trophy_cultist_hildir", "trophy_dragonqueen",
                    "animal_fish4"
                }
            },
            {
                RecipeBiome.Ocean, new[]
                {
                    "chitin", "serpentscale", "serpentmeat", "serpentmeatcooked", "trophy_serpent",
                    "animal_fish3", "animal_fish8", "animal_fish12"
                }
            },
            {
                RecipeBiome.Plains, new[]
                {
                    "blackmetalscrap", "blackmetal", "barley", "barleyflour", "cloudberries", "flax",
                    "linenthread", "loxpelt", "loxmeat", "loxmeat_cooked", "needle", "tar", "goblintotem",
                    "yagluththing",
                    "trophy_deathsquito", "trophy_goblin", "trophy_goblinbrute", "trophy_goblinshaman",
                    "trophy_growth", "trophy_lox", "trophy_bjorn_undead", "trophy_shamanbro", "trophy_brutebro",
                    "trophy_goblinking",
                    "animal_fish7"
                }
            },
            {
                RecipeBiome.Mistlands, new[]
                {
                    "copperscrap", "blackmarble", "yggdrasilwood", "bilebag", "blackcore", "bloodclot",
                    "juteblue", "carapace", "dvergrneedle", "hook", "jotunpuffs", "magecap", "mandible",
                    "mechanicalspring", "eitr", "royaljelly", "sap", "scalehide", "softtissue", "wisp",
                    "hare_meat", "hare_meat_cooked", "bug_meat", "bug_meat_cooked", "dvergrkeyfragment",
                    "lantern", "seekerqueen_drop",
                    "trophy_seeker", "trophy_seeker_brute", "trophy_gjall", "trophy_tick", "trophy_hare",
                    "trophy_dvergr", "trophy_seekerqueen",
                    "animal_fish9"
                }
            },
            {
                RecipeBiome.Ashlands, new[]
                {
                    "flametalore", "flametal", "grausten", "blackwood", "gemstone_red", "gemstone_blue",
                    "gemstone_green", "askbladder", "askhide", "asksvincarrionneck", "asksvincarrionpelvic",
                    "asksvincarrionribcage", "asksvincarrionskull", "asksvin_meat", "asksvin_meat_cooked",
                    "asksvin_egg", "bonemawtooth", "bonemawmeat", "bonemawmeat_cooked", "bonemawscale",
                    "celestialfeather", "ceramicplate", "charcoalresin", "charredbone", "charredcogwheel",
                    "charredskull", "fiddleheadfern", "moltencore", "morgenheart", "morgensinew",
                    "pot_shard_green", "proustitepowder", "bronzescrap", "shieldcore", "smokepuff",
                    "sulfurstone", "vineberry", "vineberryseeds", "volture_meat", "volture_meat_cooked",
                    "voltureegg", "bellfragment", "bell", "fader_drop",
                    "trophy_asksvin", "trophy_bonemaw", "trophy_fallenvalkyrie", "trophy_charredarcher",
                    "trophy_charredmage", "trophy_charredmelee", "trophy_morgen", "trophy_volture",
                    "trophy_blob_lava", "trophy_fader",
                    "animal_fish11"
                }
            },
            {
                RecipeBiome.DeepNorth, new[]
                {
                    "goldore", "gold", "frostwood", "fircone_big", "ice", "snowball", "frostcore", "glowworm",
                    "frozenfuel", "lingonberries", "kale", "kaleseeds", "oat", "oatseeds", "oatflour",
                    "poteitr", "poteitrseeds", "sealhide", "blubber", "blubber_cooked", "moose_meat",
                    "moose_meat_cooked", "moosehide", "moosesinew", "elakinghairbundle", "moleclaws",
                    "barkabranch", "leatherstraps", "orbfrostfire", "orbthunderblood", "crownjewel",
                    "ooze_mork", "faderember", "nornthread", "memorialcoal", "hatefulblood", "frozenking_drop",
                    "ancientcoin", "ancientgemstone_black", "ancientgemstone_green", "ancientgemstone_orange",
                    "ancientgemstone_purple",
                    "trophy_barka", "trophy_elaking", "trophy_mole", "trophy_jotunwitch", "trophy_jotunwarrior",
                    "trophy_moose", "trophy_blob_morkhalla", "trophy_seal",
                    "animal_fish10"
                }
            }
        };

        private const string DeepNorthMouldPrefix = "$item_mold_";

        private static Dictionary<string, RecipeBiome> s_biomeByName;

        public static IEnumerable<string> ItemsFor(RecipeBiome biome)
        {
            string[] items;
            if (!s_biomeItems.TryGetValue(biome, out items))
            {
                yield break;
            }

            for (int i = 0; i < items.Length; i++)
            {
                yield return SharedName(items[i]);
            }
        }

        public static bool IsBiomeMould(RecipeBiome biome, string sharedName)
        {
            return biome == RecipeBiome.DeepNorth && IsMould(sharedName);
        }

        public static bool TryGetBiome(string sharedName, out RecipeBiome biome)
        {
            biome = RecipeBiome.Special;
            if (string.IsNullOrEmpty(sharedName))
            {
                return false;
            }

            if (IsMould(sharedName))
            {
                biome = RecipeBiome.DeepNorth;
                return true;
            }

            EnsureIndex();
            return s_biomeByName.TryGetValue(sharedName, out biome);
        }

        private static bool IsMould(string sharedName)
        {
            return sharedName != null && sharedName.StartsWith(DeepNorthMouldPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureIndex()
        {
            if (s_biomeByName != null)
            {
                return;
            }

            s_biomeByName = new Dictionary<string, RecipeBiome>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<RecipeBiome, string[]> pair in s_biomeItems)
            {
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    string name = SharedName(pair.Value[i]);
                    RecipeBiome existing;
                    if (!s_biomeByName.TryGetValue(name, out existing) || Rank(pair.Key) < Rank(existing))
                    {
                        s_biomeByName[name] = pair.Key;
                    }
                }
            }
        }

        public static string SharedName(string key)
        {
            if (key.StartsWith("animal_", StringComparison.Ordinal))
            {
                return "$" + key;
            }

            return "$item_" + key;
        }

        public static int Rank(RecipeBiome biome)
        {
            switch (biome)
            {
                case RecipeBiome.Meadows:
                    return 0;
                case RecipeBiome.BlackForest:
                    return 10;
                case RecipeBiome.Swamp:
                    return 20;
                case RecipeBiome.Mountains:
                    return 30;
                case RecipeBiome.Ocean:
                    return 35;
                case RecipeBiome.Plains:
                    return 40;
                case RecipeBiome.Mistlands:
                    return 50;
                case RecipeBiome.Ashlands:
                    return 60;
                case RecipeBiome.DeepNorth:
                    return 70;
                default:
                    return 1000;
            }
        }
    }
}
