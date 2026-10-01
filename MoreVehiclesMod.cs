// <copyright file="MoreVehiclesMod.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace MoreVehicles
{
    using System.Collections.Generic;
    using ColossalFramework.Plugins;
    using ICities;
    using MoreVehicles.Patches;
    using MoreVehicles.Utils;
    using SkyTools.Patching;
    using SkyTools.Tools;
    using static MoreVehicles.Utils.Constants;

    /// <summary>The main class of the More Vehicles mod.</summary>
    public sealed class MoreVehiclesMod : LoadingExtensionBase, IUserMod
    {
        private const string HarmonyId = "com.cities_skylines.dymanoid.morevehicles";

        private MethodPatcher patcher;

        /// <summary>Gets the name of this mod.</summary>
        public string Name => "More Vehicles Renewed";

        /// <summary>Gets the description string of this mod.</summary>
        public string Description => "Increases the maximum allowed number of spawned vehicles.";

        /// <summary>Called when this mod is enabled.</summary>
        public void OnEnabled()
        {
            if (Compatibility.AreAnyIncompatibleModsActive())
            {
                Log.Info("'More Vehicles' cannot be started because of incompatible mods");
                return;
            }

            Log.Info("'More Vehicles' has been enabled.");

            IPatch[] patches =
            {
                BuildingPatch.RemoveOwnVehicle,
                BuildingPatch.IsOwnVehicle,
                BuildingPatch.RemoveGuestVehicle,
                BuildingPatch.IsGuestVehicle,
                CinematicCameraControllerPatch.GetRandomVehicle,
                CinematicCameraControllerPatch.GetVehicleWithName,
                CinematicCameraControllerPatch.GetNearestVehicle,
                EventManagerPatch.InvalidatePathsOnClosedRoads,
                GuideManagerPatch.ServiceStep,
                OutsideConnectionAIPatch.DummyTrafficProbability,
                PathVisualizerPatch.AddPathsImpl,
                ResidentAIPatch.DoRandomMove,
                TouristAIPatch.DoRandomMove,
                VehicleManagerPatch.DataDeserialize,
                VehicleManagerPatch.UpdateData,
                VehicleManagerPatch.AirlineModified,
                VehicleManagerPatch.SimulationStepImpl,
                VehiclePatch.GetTargetFrame,
                VehicleLinkedListPatch.EndRenderingImpl,
                VehicleLinkedListPatch.PlayAudioImpl,
                VehicleLinkedListPatch.ReleaseVehicleImplementation,
            };

            patcher = new MethodPatcher(HarmonyId, patches);

            var patchedMethods = patcher.Apply();
            if (patchedMethods.Count == patches.Length)
            {
                PluginManager.instance.eventPluginsChanged += ModsChanged;
                VehicleManagerCustomizer.Customize();
            }
            else
            {
                Log.Error("'More Vehicles' failed to perform method redirections");
                patcher.Revert();
                patcher = null;
            }
        }

        /// <summary>Called when this mod is disabled.</summary>
        public void OnDisabled()
        {
            if (patcher == null)
            {
                return;
            }

            PluginManager.instance.eventPluginsChanged -= ModsChanged;

            patcher.Revert();
            patcher = null;
            VehicleManagerCustomizer.Revert();

            Log.Info("'More Vehicles' has been disabled.");
        }

        /// <summary>
        /// Performs mod registration when a game level is loaded.
        /// </summary>
        /// <param name="mode">The mode the game level is loaded in.</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            if (patcher == null)
            {
                return;
            }

            switch (mode)
            {
                case LoadMode.NewGame:
                case LoadMode.LoadGame:
                case LoadMode.LoadScenario:
                case LoadMode.NewGameFromScenario:
                    break;

                default:
                    return;
            }

            // Safety: VehicleManager is a DontDestroyOnLoad singleton so Awake() normally
            // runs only once, but guard against edge cases (e.g. mod enabled mid-session)
            // where the arrays may have been reset to vanilla sizes.
            if (VehicleManager.instance.m_vehicles.m_buffer.Length != ModdedMaxVehicleCount)
            {
                Log.Info("The 'More Vehicles' mod is re-applying vehicle manager customization after level load.");
                VehicleManagerCustomizer.Customize();
            }

            var gameMetadata = SimulationManager.instance.m_metaData;
            if (gameMetadata == null)
            {
                return;
            }

            lock (gameMetadata)
            {
                if (gameMetadata.m_modOverride == null)
                {
                    gameMetadata.m_modOverride = new Dictionary<string, bool>();
                }

                gameMetadata.m_modOverride[MetadataModName] = true;
            }
        }

        private void ModsChanged()
        {
            if (Compatibility.AreAnyIncompatibleModsActive())
            {
                Log.Info("'More Vehicles' cannot be started because of incompatible mods");
                OnDisabled();
            }
        }
    }
}
