﻿using BepInEx;
using HarmonyLib;
using Steamworks.Data;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using System.Text.RegularExpressions;
using TMPro;
using System.Security.Cryptography;
using LC_API;
using System.Security.Permissions;
using UnityEngine.SceneManagement;
using System.Linq;
using GameNetcodeStuff;
using System.Runtime.CompilerServices;
using System.Collections;
using BiggerLobby.UI;
using BiggerLobby.Models;

namespace BiggerLobby.Patches
{
    [HarmonyPatch]
    public class NonGamePatches
    {
        private static PropertyInfo _playbackVolumeProperty = typeof(Dissonance.Audio.Playback.VoicePlayback).GetInterface("IVoicePlaybackInternal").GetProperty("PlaybackVolume");
        private static FieldInfo _lobbyListField = AccessTools.Field(typeof(SteamLobbyManager), "currentLobbyList");

        [HarmonyPatch(typeof(StartOfRound), "UpdatePlayerVoiceEffects")]
        [HarmonyPrefix]
        public static void UpdatePlayerVoiceEffects(StartOfRound __instance)
        {
            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }
            (typeof(StartOfRound)).GetField("updatePlayerVoiceInterval", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance,2f);
            PlayerControllerB playerControllerB = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead || !(GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)) ? GameNetworkManager.Instance.localPlayerController : GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript);
            for (int i = 0; i < __instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB playerControllerB2 = __instance.allPlayerScripts[i];
                if ((!playerControllerB2.isPlayerControlled && !playerControllerB2.isPlayerDead) || playerControllerB2 == GameNetworkManager.Instance.localPlayerController)
                {
                    continue;
                }
                if (playerControllerB2.voicePlayerState == null || playerControllerB2.currentVoiceChatIngameSettings._playerState == null || playerControllerB2.currentVoiceChatAudioSource == null)
                {
                    __instance.RefreshPlayerVoicePlaybackObjects();
                    if (playerControllerB2.voicePlayerState == null || playerControllerB2.currentVoiceChatAudioSource == null)
                    {
                        Debug.Log($"Was not able to access voice chat object for player #{i}; {playerControllerB2.voicePlayerState == null}; {playerControllerB2.currentVoiceChatAudioSource == null}");
                        continue;
                    }
                }
                AudioSource currentVoiceChatAudioSource = __instance.allPlayerScripts[i].currentVoiceChatAudioSource;
                bool flag = playerControllerB2.speakingToWalkieTalkie && playerControllerB.holdingWalkieTalkie && playerControllerB2 != playerControllerB;
                if (playerControllerB2.isPlayerDead)
                {
                    currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().enabled = false;
                    currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled = false;
                    currentVoiceChatAudioSource.panStereo = 0f;
                    SoundManager.Instance.playerVoicePitchTargets[playerControllerB2.playerClientId] = 1f;
                    SoundManager.Instance.SetPlayerPitch(1f, (int)playerControllerB2.playerClientId);
                    if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
                    {
                        currentVoiceChatAudioSource.spatialBlend = 0f;
                        playerControllerB2.currentVoiceChatIngameSettings.set2D = true;
                        if (playerControllerB2.currentVoiceChatIngameSettings != null && playerControllerB2.currentVoiceChatIngameSettings._playbackComponent != null)
                        {
                            _playbackVolumeProperty.SetValue(playerControllerB2.currentVoiceChatIngameSettings._playbackComponent, Mathf.Clamp((SoundManager.Instance.playerVoiceVolumes[i] + 1) * (2 * Plugin._LoudnessMultiplier.Value), 0f, 1f));
                        }
                    }
                    else
                    {
                        currentVoiceChatAudioSource.spatialBlend = 1f;
                        playerControllerB2.currentVoiceChatIngameSettings.set2D = false;
                        //playerControllerB2.voicePlayerState.Volume = 0f;
                        if (playerControllerB2.currentVoiceChatIngameSettings != null && playerControllerB2.currentVoiceChatIngameSettings._playbackComponent != null)
                        {
                            _playbackVolumeProperty.SetValue(playerControllerB2.currentVoiceChatIngameSettings._playbackComponent, 0);
                        }
                    }
                    continue;
                }
                AudioLowPassFilter component = currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>();
                OccludeAudio component2 = currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
                component.enabled = true;
                component2.overridingLowPass = flag || __instance.allPlayerScripts[i].voiceMuffledByEnemy;
                currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled = flag;
                if (!flag)
                {
                    currentVoiceChatAudioSource.spatialBlend = 1f;
                    playerControllerB2.currentVoiceChatIngameSettings.set2D = false;
                    currentVoiceChatAudioSource.bypassListenerEffects = false;
                    currentVoiceChatAudioSource.bypassEffects = false;
                    currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerControllerB2.playerClientId];
                    component.lowpassResonanceQ = 1f;
                }
                else
                {
                    currentVoiceChatAudioSource.spatialBlend = 0f;
                    playerControllerB2.currentVoiceChatIngameSettings.set2D = true;
                    if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
                    {
                        currentVoiceChatAudioSource.panStereo = 0f;
                        currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerControllerB2.playerClientId];
                        currentVoiceChatAudioSource.bypassListenerEffects = false;
                        currentVoiceChatAudioSource.bypassEffects = false;
                    }
                    else
                    {
                        currentVoiceChatAudioSource.panStereo = 0.4f;
                        currentVoiceChatAudioSource.bypassListenerEffects = false;
                        currentVoiceChatAudioSource.bypassEffects = false;
                        currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerControllerB2.playerClientId];
                    }
                    component2.lowPassOverride = 4000f;
                    component.lowpassResonanceQ = 3f;
                }
                /*if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
                {
                    playerControllerB2.voicePlayerState.Volume = 0.8f;
                }
                else
                {*/
                if (playerControllerB2.currentVoiceChatIngameSettings != null && playerControllerB2.currentVoiceChatIngameSettings._playbackComponent != null)
                {
                    _playbackVolumeProperty.SetValue(playerControllerB2.currentVoiceChatIngameSettings._playbackComponent, Mathf.Clamp((SoundManager.Instance.playerVoiceVolumes[i] + 1) * (2 * Plugin._LoudnessMultiplier.Value), 0f, 1f));
                }
            }
        }
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        public static void ResizeLists(ref StartOfRound __instance)
        {
            __instance.allPlayerObjects = Helper.ResizeArray(__instance.allPlayerObjects, Plugin.MaxPlayers);
            __instance.allPlayerScripts = Helper.ResizeArray(__instance.allPlayerScripts, Plugin.MaxPlayers);
            __instance.gameStats.allPlayerStats = Helper.ResizeArray(__instance.gameStats.allPlayerStats, Plugin.MaxPlayers);
            __instance.playerSpawnPositions = Helper.ResizeArray(__instance.playerSpawnPositions, Plugin.MaxPlayers);
            for (int j = 4; j < Plugin.MaxPlayers; j++)
            {
                __instance.gameStats.allPlayerStats[j] = new PlayerStats();
                __instance.playerSpawnPositions[j] = __instance.playerSpawnPositions[0];
            }
        }

        [HarmonyPatch(typeof(HUDManager), "Awake")]
        [HarmonyPrefix]
        public static void ResizeHUD(ref HUDManager __instance)
        {
            var expandedStats = ExpandedStatsUI.GetFromAnimator(__instance.endgameStatsAnimator);
        }

        [HarmonyPatch(typeof(SoundManager), "SetPlayerVoiceFilters")]
        [HarmonyPrefix]
        public static bool SetPlayerVoiceFilters(ref SoundManager __instance)
        {
            for (int j = 0; j < StartOfRound.Instance.allPlayerScripts.Length; j++)
            {
                if (!StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled && !StartOfRound.Instance.allPlayerScripts[j].isPlayerDead)
                {
                    __instance.playerVoicePitches[j] = 1f;
                    __instance.playerVoiceVolumes[j] = 1f;
                    continue;
                }
                if (StartOfRound.Instance.allPlayerScripts[j].voicePlayerState != null) {
                    (typeof(Dissonance.Audio.Playback.VoicePlayback).GetProperty("Dissonance.Audio.Playback.IVoicePlaybackInternal.PlaybackVolume", BindingFlags.NonPublic | BindingFlags.Instance)).SetValue(StartOfRound.Instance.allPlayerScripts[j].currentVoiceChatIngameSettings._playbackComponent, Mathf.Clamp((SoundManager.Instance.playerVoiceVolumes[j] + 1) * (2 * Plugin._LoudnessMultiplier.Value), 0f, 1f));
                }
                if (Mathf.Abs(__instance.playerVoicePitches[j] - __instance.playerVoicePitchTargets[j]) > 0.025f)
                {
                    __instance.playerVoicePitches[j] = Mathf.Lerp(__instance.playerVoicePitches[j], __instance.playerVoicePitchTargets[j], 3f * Time.deltaTime);
                }
                else if (__instance.playerVoicePitches[j] != __instance.playerVoicePitchTargets[j])
                {
                    __instance.playerVoicePitches[j] = __instance.playerVoicePitchTargets[j];
                }
            }
            return (false);
        }
        [HarmonyPatch(typeof(MenuManager), "OnEnable")]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void CustomMenu(ref MenuManager __instance)
        {
            if (__instance.isInitScene)
            {
                return;
            }

            float spacingAmount = 30f;

            // Get references to all main menu UI objects we need to modify
            var hostSettingsContainer = __instance.HostSettingsOptionsNormal.transform.parent.parent.gameObject?.GetComponent<RectTransform>();
            var lobbyHostOptions = hostSettingsContainer?.Find("LobbyHostOptions")?.GetComponent<RectTransform>();
            var optionsNormal = lobbyHostOptions?.Find("OptionsNormal")?.GetComponent<RectTransform>();

            if (hostSettingsContainer == null || lobbyHostOptions == null || optionsNormal == null)
                return;

            var serverNameField = optionsNormal?.Find("ServerNameField")?.GetComponent<RectTransform>();
            var serverTagInputField = optionsNormal?.Find("ServerTagInputField")?.GetComponent<RectTransform>();
            var confirmButton = hostSettingsContainer?.Find("Confirm")?.GetComponent<RectTransform>();
            var backButton = hostSettingsContainer?.Find("Back")?.GetComponent<RectTransform>();
            var privatePublicDescription = hostSettingsContainer?.Find("PrivatePublicDescription")?.GetComponent<TextMeshProUGUI>();

            if (serverNameField == null || serverTagInputField == null || confirmButton == null || backButton == null || privatePublicDescription == null)
                return;

            // Create new field for user-inputted player counts
            var playerNumberField = UnityEngine.Object.Instantiate(serverNameField, serverNameField.transform.parent);
            var playerNumberInputField = playerNumberField.GetComponent<TMP_InputField>();

            // Resize and reposition UI elements to fit the new field
            hostSettingsContainer.sizeDelta = hostSettingsContainer.sizeDelta + new Vector2(0, spacingAmount / 2f);
            lobbyHostOptions.anchoredPosition = lobbyHostOptions.anchoredPosition + new Vector2(0, spacingAmount / 4f);
            serverTagInputField.anchoredPosition = serverTagInputField.anchoredPosition + new Vector2(0, -spacingAmount / 2f);
            confirmButton.anchoredPosition = confirmButton.anchoredPosition + new Vector2(0, -spacingAmount / 2f);
            backButton.anchoredPosition = backButton.anchoredPosition + new Vector2(0, -spacingAmount / 2f);

            foreach (RectTransform transform in optionsNormal)
            {
                if (transform == null || transform.name == "EnterAName" || transform == serverNameField || transform == serverTagInputField) 
                    continue;

                transform.anchoredPosition = transform.anchoredPosition + new Vector2(0, -spacingAmount);
                // Specifically make the tag 
            }

            // Finish setting up custom players field
            // TODO: Clean this bit up, it's still mostly legacy code
            playerNumberField.name = "ServerPlayersField";
            playerNumberInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            playerNumberField.transform.Find("Text Area").Find("Placeholder").gameObject.GetComponent<TextMeshProUGUI>().text = "Max players (16)...";

            void OnChange()
            {
                string text = Regex.Replace(playerNumberInputField.text, "[^0-9]", "");
                int newnumber;
                if (!(int.TryParse(text, out newnumber)))
                {
                    newnumber = 16;
                }
                newnumber = Math.Min(Math.Max(newnumber, 4), 40);
                Plugin.Logger?.LogInfo($"Setting max player count to: {newnumber}");
                if (newnumber > 16)
                {
                    privatePublicDescription.text = "Notice: High max player counts\nmay cause lag.";
                }
                else
                {
                    if (privatePublicDescription.text == "Notice: High max player counts\nmay cause lag.")
                    {
                        privatePublicDescription.text = "yeah you should be good now lol";
                    }
                }

            }
            playerNumberInputField.onValueChanged.AddListener(delegate { OnChange(); });
        }
        [HarmonyPatch(typeof(MenuManager), "StartHosting")]
        [HarmonyPrefix]
        public static bool StartHost(MenuManager __instance)
        {
            if (GameNetworkManager.Instance.currentLobby == null)
            {
                return (true);
            }
            GameObject SPF = __instance.HostSettingsOptionsNormal.transform.Find("ServerPlayersField").gameObject;
            GameObject Input = SPF.transform.Find("Text Area").Find("Text").gameObject;
            TextMeshProUGUI iTextMeshProUGUI = Input.GetComponent<TextMeshProUGUI>();
            string text = Regex.Replace(iTextMeshProUGUI.text, "[^0-9]", "");
            int newnumber;
            if (!(int.TryParse(text, out newnumber)))
            {
                newnumber = 16;
            }
            newnumber = Math.Min(Math.Max(newnumber, 4), 40);
            Lobby lobby = GameNetworkManager.Instance.currentLobby ?? new Lobby();
            lobby.SetData("MaxPlayers", newnumber.ToString());
            Plugin.Logger?.LogInfo($"Setting max players to {newnumber}!");
            Plugin.MaxPlayers = newnumber;
            if (GameNetworkManager.Instance != null) GameNetworkManager.Instance.maxAllowedPlayers = Plugin.MaxPlayers;
            return (true);
        }

        [HarmonyPatch(typeof(HUDManager), "FillEndGameStats")]
        [HarmonyPrefix]
        public static void FillEndGameStats(HUDManager __instance)
        {
            // Modify the arrays to our liking
            // This is probably unnecessary to run every time, and could just be initalized when the player count changes
            var expandedStats = ExpandedStatsUI.GetFromAnimator(__instance.endgameStatsAnimator);
            if (expandedStats == null || StartOfRound.Instance == null) return; // something has gone terribly wrong
            var statsList = expandedStats.GetStatsListFromPlayerCount(Plugin.GetRealPlayerScripts(StartOfRound.Instance).Length);

            __instance.statsUIElements.playerNamesText = statsList.Names.ToArray();
            __instance.statsUIElements.playerStates = statsList.States.ToArray();
            __instance.statsUIElements.playerNotesText = statsList.Notes.ToArray();

            Plugin.Logger?.LogInfo("Adding EXPANDED stats!");
        }

        [HarmonyPatch(typeof(HUDManager), "FillEndGameStats")]
        [HarmonyPostfix]
        public static void FillEndGameStatsPostfix(HUDManager __instance)
        {
            // Make notes smaller for 5+ players so we can fit everything on the screen
            if (StartOfRound.Instance == null) return;
            var playerCount = Plugin.GetRealPlayerScripts(StartOfRound.Instance).Length;
            if (playerCount > 4)
            {
                foreach (var notesText in __instance.statsUIElements.playerNotesText)
                {
                    if (notesText.text == "") continue;
                    notesText.text = notesText.text.Replace("Notes:", "").Trim();
                }

                var replacementCheckmark = ExpandedStatsUI.GetReplacementCheckmark();
                if (replacementCheckmark == null) return;
                foreach (var playerState in __instance.statsUIElements.playerStates)
                {
                    if (playerState.sprite != __instance.statsUIElements.aliveIcon) continue;
                    playerState.sprite = replacementCheckmark;
                }
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager),"StartHost")]
        [HarmonyPrefix]
        public static bool DoTheThe()
        {
            Plugin.CustomNetObjects.Clear();
            return (true);
        }
        [HarmonyPatch(typeof(GameNetworkManager), "StartClient")]
        [HarmonyPrefix]
        public static bool StartClient(GameNetworkManager __instance)
        {
            Plugin.CustomNetObjects.Clear();
            return (true);
        }
        [HarmonyPatch(typeof(MenuManager), "StartAClient")]
        [HarmonyPrefix]
        public static bool StartAClient()
        {
            Plugin.CustomNetObjects.Clear();
            Plugin.Logger?.LogInfo("LAN Running.");
            return (true);
        }
        [HarmonyPatch(typeof(SteamLobbyManager), "loadLobbyListAndFilter")]
        [HarmonyPostfix]
        public static IEnumerator LoadLobbyListAndFilter(IEnumerator result, SteamLobbyManager __instance)
        {
            // Run original enumerator code
            while (result.MoveNext())
                yield return result.Current;

            // Ideally this would happen as the enumerator executes but I just want to get something working RN
            // inject into existing server list 
            Plugin.Logger?.LogInfo("Injecting BL playercounts into lobby list.");
            LobbySlot[] lobbySlots = __instance.levelListContainer.GetComponentsInChildren<LobbySlot>(true);

            foreach(var lobbySlot in lobbySlots)
            {
                try
                {
                    // TODO: replace with custom graphic or something neat
                    // TODO: Make this not count towards the 40 character max. don't wanna fix rn
                    lobbySlot.LobbyName.text = lobbySlot.LobbyName.text.Replace("[BiggerLobby]", "[BL]");
                    string text = lobbySlot.thisLobby.GetData("MaxPlayers");
                    int maxPlayers;
                    if (!(int.TryParse(text, out maxPlayers)))
                    {
                        maxPlayers = 4;
                    }
                    lobbySlot.playerCount.text = lobbySlot.playerCount.text.Replace("/ 4", $"/ {maxPlayers}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning("Exception while injecting BL lobby metadata:");
                    Plugin.Logger?.LogWarning(ex);
                }
            }
        }

        [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.CreateLobbyAsync))]
        [HarmonyPrefix]
        public static void SetMaxMembers(ref int maxMembers)
        {
            maxMembers = Plugin.MaxPlayers;
        }
        [HarmonyPatch(typeof(GameNetworkManager))]
        internal class InternalPatches
        {
            static MethodInfo TargetMethod()
            {
                return typeof(GameNetworkManager)
                    .GetMethod("ConnectionApproval",
                               BindingFlags.NonPublic | BindingFlags.Instance);
            }
            [HarmonyPrefix]
            static bool PostFix(GameNetworkManager __instance, NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
            {
                Debug.Log("Connection approval callback! Game version of client request: " + Encoding.ASCII.GetString(request.Payload).ToString());
                Debug.Log($"Joining client id: {request.ClientNetworkId}; Local/host client id: {NetworkManager.Singleton.LocalClientId}");
                if (request.ClientNetworkId == NetworkManager.Singleton.LocalClientId)
                {
                    Debug.Log("Stopped connection approval callback, as the client in question was the host!");
                    return (false);
                }
                bool flag = !__instance.disallowConnection;
                if (flag)
                {
                    string @string = Encoding.ASCII.GetString(request.Payload);
                    string[] array = @string.Split(",");
                    if (string.IsNullOrEmpty(@string))
                    {
                        response.Reason = "Unknown; please verify your game files.";
                        flag = false;
                    }
                    else if (__instance.gameHasStarted)
                    {
                        response.Reason = "Game has already started!";
                        flag = false;
                    }
                    else if (__instance.gameVersionNum.ToString() != array[0])
                    {
                        response.Reason = $"Game version mismatch! Their version: {__instance.gameVersionNum}. Your version: {array[0]}";
                        flag = false;
                    }
                    else if (!__instance.disableSteam && (StartOfRound.Instance == null || array.Length < 2 || StartOfRound.Instance.KickedClientIds.Contains((ulong)Convert.ToInt64(array[1]))))
                    {
                        response.Reason = "You cannot rejoin after being kicked.";
                        flag = false;
                    }
                    else if (!(@string.Contains("BiggerLobbyVersion2.5.0")))
                    {
                        response.Reason = "You need to have <color=#008282>BiggerLobby V2.5.0</color> to join this server!";
                        flag = false;
                    }
                }
                else
                {
                    response.Reason = "The host was not accepting connections.";
                }
                Debug.Log($"Approved connection?: {flag}. Connected players #: {__instance.connectedPlayers}");
                Debug.Log("Disapproval reason: " + response.Reason);
                response.CreatePlayerObject = false;
                response.Approved = flag;
                response.Pending = false;
                return (false);
            } //etc
        }
        [HarmonyPatch(typeof(GameNetworkManager))]
        internal class InternalPatches2
        {
            static MethodInfo TargetMethod()
            {
                return typeof(GameNetworkManager)
                    .GetMethod("SteamMatchmaking_OnLobbyCreated",
                               BindingFlags.NonPublic | BindingFlags.Instance);
            }
            [HarmonyPostfix]
            static void PostFix(GameNetworkManager __instance, Result result, Lobby lobby)
            {
                lobby.SetData("name", "[BiggerLobby]" + lobby.GetData("name"));
            } //etc
        }
        [HarmonyPatch(typeof(GameNetworkManager), "SetConnectionDataBeforeConnecting")]
        [HarmonyPrefix]
        public static bool SetConnectionDataBeforeConnecting(GameNetworkManager __instance)
        {
            __instance.localClientWaitingForApproval = true;
            Debug.Log("Game version: " + __instance.gameVersionNum);
            if (__instance.disableSteam)
            {
                NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(__instance.gameVersionNum.ToString() + "," + "BiggerLobbyVersion2.5.0");//this nonsense ass string exists to tell the server if youre running biggerlobby for some reason. Also she fortnite on my burger till I battle pass
            }
            else
            {
                NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(__instance.gameVersionNum + "," + (ulong)SteamClient.SteamId + "," + "BiggerLobbyVersion2.5.0");
            }
            return (false);
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.LobbyDataIsJoinable))]
        [HarmonyPrefix]
        public static bool SkipLobbySizeCheck(ref GameNetworkManager __instance, ref bool __result, Lobby lobby)
        {
            string data = lobby.GetData("vers");
            string text = lobby.GetData("MaxPlayers");
            int newnumber;
            if (!(int.TryParse(text, out newnumber)))
            {
                newnumber = 16;
            }
            newnumber = Math.Min(Math.Max(newnumber, 4), 40);
            if (lobby.MemberCount >= newnumber || lobby.MemberCount < 1)
            {
                Debug.Log($"Lobby join denied! Too many members in lobby! {lobby.Id}");
                UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.Full, "The server is full!");
                __result = false;
                return false;
            }
            if (data != __instance.gameVersionNum.ToString())
            {
                Debug.Log($"Lobby join denied! Attempted to join vers.{data} lobby id: {lobby.Id}");
                UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.DoesntExist, $"The server host is playing on version {data} while you are on version {__instance.gameVersionNum}.");
                __result = false;
                return false;
            }
            if (lobby.GetData("joinable") == "false")
            {
                Debug.Log("Lobby join denied! Host lobby is not joinable");
                UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.DoesntExist, "The server host has already landed their ship, or they are still loading in.");
                __result = false;
                return false;
            }
            Plugin.MaxPlayers = newnumber;
            Plugin.Logger?.LogInfo($"Setting max players to {newnumber}!");
            if (__instance != null) __instance.maxAllowedPlayers = Plugin.MaxPlayers;
            // Lobby member count check is skipped here, see original method
            __result = true;
            return false;
        }
    }
}
