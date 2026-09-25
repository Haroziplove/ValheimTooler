using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RapidGUI;
using UnityEngine;
using ValheimTooler.Core.Extensions;
using ValheimTooler.Models.Mono;
using ValheimTooler.Utils;

namespace ValheimTooler.Core
{
    public static class MiscHacks
    {
        public static bool s_enableAutopinMap = false;
        public static bool s_autopinVisibleOnly = false;
        private static int s_playerDamageIdx = 0;
        private static string s_damageToDeal = "1";

        private static string s_worldMessageText = "";

        private static string s_chatUsernameText = "";
        private static string s_chatMessageText = "";
        private static bool s_isShoutMessage = false;

        private static List<Player> s_players = null;

        private static float s_updateTimer = 0f;
        private static readonly float s_updateTimerInterval = 1.5f;

        public static void Start()
        {
            return;
        }

        public static void Update()
        {
            if (Time.time >= s_updateTimer)
            {
                s_players = Player.GetAllPlayers();
                if (s_enableAutopinMap)
                {
                    PinNearbyDeposits(s_autopinVisibleOnly);
                }

                s_updateTimer = Time.time + s_updateTimerInterval;
            }

            if (ConfigManager.s_espPlayersShortcut.Value.IsDown())
            {
                ActionToggleESPPlayers(true);
            }
            if (ConfigManager.s_espMonstersShortcut.Value.IsDown())
            {
                ActionToggleESPMonsters(true);
            }
            if (ConfigManager.s_espDroppedItemsShortcut.Value.IsDown())
            {
                ActionToggleESPDroppedItems(true);
            }
            if (ConfigManager.s_espDepositsShortcut.Value.IsDown())
            {
                ActionToggleESPDeposits(true);
            }
            if (ConfigManager.s_espPickablesShortcut.Value.IsDown())
            {
                ActionToggleESPPickables(true);
            }
        }

        public static void DisplayGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical();
                {
                    UI.Controls.BeginSection("$vt_misc_damage_title");
                    {
                        GUILayout.BeginHorizontal();
                        {
                            UI.Controls.FieldLabel("$vt_misc_damage_player");
                            s_playerDamageIdx = RGUI.SelectionPopup(s_playerDamageIdx, s_players?.Select(p => p.GetPlayerName()).ToArray());
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            UI.Controls.FieldLabel("$vt_misc_damage_value");
                            s_damageToDeal = GUILayout.TextField(s_damageToDeal, GUILayout.ExpandWidth(true));
                        }
                        GUILayout.EndHorizontal();

                        if (UI.Controls.ActionButton("$vt_misc_damage_button_player", FeatureMethod.Direct))
                        {

                            if (int.TryParse(s_damageToDeal, out int damage))
                            {
                                s_players[s_playerDamageIdx].VTDamage(damage);
                            }
                        }
                        if (UI.Controls.ActionButton("$vt_misc_damage_button_entities", FeatureMethod.Direct))
                        {
                            DamageAllCharacters();
                        }
                        if (UI.Controls.ActionButton("$vt_misc_damage_button_radius", FeatureMethod.Direct, null, true))
                        {
                            DamageCharactersInRadius(ConfigManager.ActionRadius);
                        }
                        if (UI.Controls.ActionButton("$vt_misc_damage_button_players", FeatureMethod.Direct))
                        {
                            DamageAllOtherPlayers();
                        }
                    }
                    UI.Controls.EndSection();

                    UI.Controls.BeginSection("$vt_misc_map_title");
                    {
                        if (UI.Controls.ActionButton("$vt_misc_clear_deaths", FeatureMethod.Direct))
                        {
                            ClearDeathMarkers();
                        }

                        if (UI.Controls.FeatureButton("$vt_misc_autopin", s_enableAutopinMap, FeatureMethod.Direct, null, true))
                        {
                            s_enableAutopinMap = !s_enableAutopinMap;
                            if (s_enableAutopinMap)
                            {
                                PinNearbyDeposits(s_autopinVisibleOnly);
                            }
                        }

                        if (UI.Controls.FeatureButton("$vt_misc_autopin_visible", s_autopinVisibleOnly, FeatureMethod.Direct, null, true))
                        {
                            s_autopinVisibleOnly = !s_autopinVisibleOnly;
                        }

                        ConfigManager.s_permanentPins.Value = UI.Controls.LabeledToggle("$vt_misc_autopin_permanent", ConfigManager.s_permanentPins.Value);

                        if (UI.Controls.ActionButton("$vt_misc_autopin_clear", FeatureMethod.Direct))
                        {
                            if (Minimap.instance != null)
                            {
                                var pinListCopy = new List<Minimap.PinData>(Minimap.instance.GetFieldValue<List<Minimap.PinData>>("m_pins"));
                                foreach (var pin in pinListCopy)
                                {
                                    if (Regex.IsMatch(pin.m_name, @"^.+\[VT[0-9]{5}]$"))
                                    {
                                        Minimap.instance.RemovePin(pin);
                                    }
                                }
                            }
                        }

                        ConfigManager.s_cheatMinimapIndicators.Value = UI.Controls.LabeledToggle("$vt_misc_cheat_indicators", ConfigManager.s_cheatMinimapIndicators.Value);

                        UI.Controls.Hint("$vt_player_minimap_hint");
                        if (UI.Controls.ActionButton("$vt_player_explore_minimap", FeatureMethod.Direct))
                        {
                            UI.Controls.AskConfirm("$vt_player_explore_minimap", "$vt_player_explore_minimap_confirm", () =>
                            {
                                if (Minimap.instance != null)
                                {
                                    Minimap.instance.VTExploreAll();
                                }
                            });
                        }
                        if (UI.Controls.ActionButton("$vt_player_reset_minimap", FeatureMethod.Direct))
                        {
                            UI.Controls.AskConfirm("$vt_player_reset_minimap", "$vt_player_reset_minimap_confirm", () =>
                            {
                                if (Minimap.instance != null)
                                {
                                    Minimap.instance.VTReset();
                                }
                            });
                        }
                    }
                    UI.Controls.EndSection();

                    UI.Controls.BeginSection("$vt_misc_event_title");
                    {
                        GUILayout.BeginHorizontal();
                        {
                            UI.Controls.FieldLabel("$vt_misc_event_message");
                            s_worldMessageText = GUILayout.TextField(s_worldMessageText, GUILayout.ExpandWidth(true));
                        }
                        GUILayout.EndHorizontal();

                        if (UI.Controls.ActionButton("$vt_misc_event_button", FeatureMethod.Direct))
                        {
                            if (MessageHud.instance != null && !string.IsNullOrEmpty(s_worldMessageText))
                            {
                                MessageHud.instance.MessageAll(MessageHud.MessageType.Center, s_worldMessageText);
                            }
                        }
                    }
                    UI.Controls.EndSection();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                {
                    UI.Controls.BeginSection("$vt_misc_chat_title");
                    {
                        GUILayout.BeginHorizontal();
                        {
                            UI.Controls.FieldLabel("$vt_misc_chat_username");
                            s_chatUsernameText = GUILayout.TextField(s_chatUsernameText, GUILayout.ExpandWidth(true));
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            UI.Controls.FieldLabel("$vt_misc_chat_message");
                            s_chatMessageText = GUILayout.TextField(s_chatMessageText, GUILayout.ExpandWidth(true));
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            s_isShoutMessage = GUILayout.Toggle(s_isShoutMessage, "");
                            UI.Controls.HoverLabel("$vt_misc_chat_shout");
                        }
                        GUILayout.EndHorizontal();

                        if (UI.Controls.ActionButton("$vt_misc_chat_button", FeatureMethod.Direct))
                        {
                            ChatMessage(s_isShoutMessage ? Talker.Type.Shout : Talker.Type.Normal, s_chatUsernameText, s_chatMessageText);
                        }
                    }
                    UI.Controls.EndSection();

                    UI.Controls.BeginSection("$vt_misc_esp_title");
                    {

                        if (UI.Controls.FeatureButton("$vt_misc_player_esp_button", ESP.s_showPlayerESP, FeatureMethod.Direct, ConfigManager.s_espPlayersShortcut.Value))
                        {
                            ActionToggleESPPlayers();
                        }

                        if (UI.Controls.FeatureButton("$vt_misc_monster_esp_button", ESP.s_showMonsterESP, FeatureMethod.Direct, ConfigManager.s_espMonstersShortcut.Value))
                        {
                            ActionToggleESPMonsters();
                        }

                        if (UI.Controls.FeatureButton("$vt_misc_dropped_esp_button", ESP.s_showDroppedESP, FeatureMethod.Direct, ConfigManager.s_espDroppedItemsShortcut.Value))
                        {
                            ActionToggleESPDroppedItems();
                        }

                        if (UI.Controls.FeatureButton("$vt_misc_deposit_esp_button", ESP.s_showDepositESP, FeatureMethod.Direct, ConfigManager.s_espDepositsShortcut.Value))
                        {
                            ActionToggleESPDeposits();
                        }

                        if (UI.Controls.FeatureButton("$vt_misc_pickable_esp_button", ESP.s_showPickableESP, FeatureMethod.Direct, ConfigManager.s_espPickablesShortcut.Value))
                        {
                            ActionToggleESPPickables();
                        }

                        ConfigManager.s_espRadiusEnabled.Value = UI.Controls.LabeledToggle("$vt_misc_radius_enable", ConfigManager.s_espRadiusEnabled.Value, true);
                        ConfigManager.s_comfortEsp.Value = UI.Controls.LabeledToggle("$vt_comfort_esp", ConfigManager.s_comfortEsp.Value);
                        UI.Controls.NoteHoverAction("$vt_misc_radius_enable");
                    }
                    UI.Controls.EndSection();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private static void ActionToggleESPPlayers(bool sendNotification = false)
        {
            ESP.s_showPlayerESP = !ESP.s_showPlayerESP;

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_misc_player_esp_button", ESP.s_showPlayerESP));
            }
        }

        private static void ActionToggleESPMonsters(bool sendNotification = false)
        {
            ESP.s_showMonsterESP = !ESP.s_showMonsterESP;

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_misc_monster_esp_button", ESP.s_showMonsterESP));
            }
        }

        private static void ActionToggleESPDroppedItems(bool sendNotification = false)
        {
            ESP.s_showDroppedESP = !ESP.s_showDroppedESP;

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_misc_dropped_esp_button", ESP.s_showDroppedESP));
            }
        }

        private static void ActionToggleESPDeposits(bool sendNotification = false)
        {
            ESP.s_showDepositESP = !ESP.s_showDepositESP;

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_misc_deposit_esp_button", ESP.s_showDepositESP));
            }
        }

        private static void ActionToggleESPPickables(bool sendNotification = false)
        {
            ESP.s_showPickableESP = !ESP.s_showPickableESP;

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_misc_pickable_esp_button", ESP.s_showPickableESP));
            }
        }

        private static void DamageAllCharacters()
        {
            DamageCharactersInRadius(-1f);
        }

        private static void DamageCharactersInRadius(float radius)
        {
            if (radius >= 0f && Player.m_localPlayer == null)
            {
                return;
            }

            Vector3 origin = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : Vector3.zero;
            foreach (Character character in Character.GetAllCharacters())
            {
                if (character == null || character.IsPlayer())
                {
                    continue;
                }

                if (radius >= 0f && global::Utils.DistanceXZ(origin, character.transform.position) > radius)
                {
                    continue;
                }

                character.VTDamage(1E+10f);
            }
        }

        public static void TryPinDeposit(Destructible destructible)
        {
            TryPinDeposit(destructible, s_autopinVisibleOnly, true);
        }

        private static bool TryPinDeposit(Destructible destructible, bool visibleOnly, bool requireToggle)
        {
            if ((requireToggle && !s_enableAutopinMap) || destructible == null || Player.m_localPlayer == null || Minimap.instance == null)
            {
                return false;
            }

            if (destructible.GetComponent<PinnedObject>() != null)
            {
                return false;
            }

            HoverText component = destructible.GetComponent<HoverText>();
            if (component == null)
            {
                return false;
            }

            string text = component.m_text != null ? component.m_text.ToLower() : "";
            if (!text.Contains("deposit") && !text.Contains("piece_mudpile"))
            {
                return false;
            }

            float radius = ConfigManager.ActionRadius;
            if (global::Utils.DistanceXZ(Player.m_localPlayer.transform.position, destructible.transform.position) > radius)
            {
                return false;
            }

            if (visibleOnly && !IsDepositExposed(destructible.gameObject))
            {
                return false;
            }

            string random_nounce = new string(Enumerable.Repeat("0123456789", 5).Select(s => s[s_random.Next(s.Length)]).ToArray());
            string name = component.GetHoverName() + " [VT" + random_nounce + "]";
            destructible.gameObject.AddComponent<PinnedObject>().Init(name);
            return true;
        }

        private static int PinNearbyDeposits(bool visibleOnly)
        {
            Destructible[] destructibles = UnityEngine.Object.FindObjectsOfType<Destructible>();
            if (destructibles == null)
            {
                return 0;
            }

            int pinned = 0;
            foreach (Destructible destructible in destructibles)
            {
                if (TryPinDeposit(destructible, visibleOnly, !visibleOnly))
                {
                    pinned++;
                }
            }

            return pinned;
        }

        private static bool IsDepositExposed(GameObject deposit)
        {
            Renderer[] renderers = deposit.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return false;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            Vector3[] samples = new Vector3[]
            {
                bounds.center,
                bounds.center + new Vector3(bounds.extents.x * 0.45f, 0f, 0f),
                bounds.center + new Vector3(-bounds.extents.x * 0.45f, 0f, 0f),
                bounds.center + new Vector3(0f, 0f, bounds.extents.z * 0.45f),
                bounds.center + new Vector3(0f, 0f, -bounds.extents.z * 0.45f)
            };

            for (int i = 0; i < samples.Length; i++)
            {
                Vector3 sample = samples[i];
                float ground;
                if (ZoneSystem.instance == null || !ZoneSystem.instance.GetGroundHeight(sample, out ground))
                {
                    ground = sample.y;
                }

                if (bounds.max.y < ground + 0.45f)
                {
                    continue;
                }

                Vector3 origin = new Vector3(sample.x, Mathf.Max(bounds.max.y, ground) + 40f, sample.z);
                RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int h = 0; h < hits.Length; h++)
                {
                    Collider collider = hits[h].collider;
                    if (collider == null)
                    {
                        continue;
                    }

                    if (collider.transform == deposit.transform || collider.transform.IsChildOf(deposit.transform))
                    {
                        return true;
                    }

                    if (hits[h].point.y > bounds.max.y + 0.2f)
                    {
                        break;
                    }
                }
            }

            return false;
        }

        private static void ClearDeathMarkers()
        {
            if (Minimap.instance == null)
            {
                return;
            }

            var pins = new List<Minimap.PinData>(Minimap.instance.GetFieldValue<List<Minimap.PinData>>("m_pins"));
            int removed = 0;
            foreach (Minimap.PinData pin in pins)
            {
                if (pin != null && pin.m_type == Minimap.PinType.Death)
                {
                    Minimap.instance.RemovePin(pin);
                    removed++;
                }
            }

            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.VTSendMessage(VTLocalization.instance.Localize("$vt_misc_clear_deaths_done") + " " + removed);
            }
        }

        private static readonly System.Random s_random = new System.Random();

        private static void DamageAllOtherPlayers()
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            foreach (Character character in Character.GetAllCharacters())
            {
                if (character.IsPlayer() && (character as Player).GetPlayerID() != Player.m_localPlayer.GetPlayerID())
                {
                    character.VTDamage(1E+10f);
                }
            }
        }

        private static void ChatMessage(Talker.Type type, string username, string message)
        {
            Player playerSender = Player.m_localPlayer;

            foreach (Player player in Player.GetAllPlayers())
            {
                if (player.GetPlayerName().ToLower().Equals(username.ToLower()))
                {
                    playerSender = player;
                    break;
                }
            }

            UserInfo localUser = UserInfo.GetLocalUser();
            UserInfo fakePlayerSender = new UserInfo
            {
                Name = username,
                UserId = localUser != null ? localUser.UserId : default
            };

            if (playerSender)
            {
                if (type == Talker.Type.Shout)
                {
                    if (ZRoutedRpc.instance != null)
                    {
                        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", new object[]
                        {
                            playerSender.GetHeadPoint(),
                            2,
                            fakePlayerSender,
                            message
                        });
                    }
                    return;
                }
                ZNetView nview = playerSender.GetComponent<Talker>().GetComponent<ZNetView>();

                if (nview)
                {
                    nview.InvokeRPC(ZNetView.Everybody, "Say", new object[]
                    {
                        (int)type,
                        fakePlayerSender,
                        message
                    });
                }
            }
        }
    }
}
