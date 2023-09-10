using BRCreator.RacePlugin.Extensions;
using HarmonyLib;
using Reptile;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


namespace BRCreator.RacePlugin.Race
{
    public class RaceManager
    {
        private RaceState state;
        private float time;
        private Queue<CheckpointPin> checkpointPins = new Queue<CheckpointPin>();
        private bool shouldLoadRaceStageASAP = false;
        private bool hasAdditionRaceConfigToLoad = false;
        private RaceConfig currentRaceConfig;

        public RaceManager()
        {
            Core.OnUpdate += Update;
            state = RaceState.None;
        }

        public void Update()
        {
            if (BepInEx.UnityInput.Current.GetKeyDown(KeyCode.F5) && state == RaceState.None)
            {
                LoadRace();

            }

            if (state == RaceState.Racing || state == RaceState.Starting)
            {
                checkpointPins.Peek().UpdateUIIndicator();

                time += Time.deltaTime;
                if (state == RaceState.Racing)
                    ShowTimer();

                if (state == RaceState.Starting)
                    ShowTimerReverse();
            }

            switch (state)
            {
                case RaceState.Starting:
                    var currentCheckpointPin = checkpointPins.Peek();
                    if (!currentCheckpointPin.Pin.gameObject.activeSelf)
                    {
                        currentCheckpointPin.Activate();
                    }

                    if (GetTimeInSecs() > 2)
                    {
                        Plugin.ShouldIgnoreInput = false;
                        state = RaceState.Racing;
                        time = 0;

                        var currentPlayer = WorldHandler.instance.GetCurrentPlayer();
                        Characters character = Traverse.Create(currentPlayer).Field<Characters>("character").Value;
                        Core.Instance.AudioManager.PlayVoice(character, AudioClipID.VoiceBoostTrick);
                    }

                    break;
                case RaceState.Racing:

                    break;
                case RaceState.Finished:
                    OnRaceFinished();
                    ResetAll();
                    break;
            }
        }

        private void LoadRace()
        {
            string filePath = Directory.GetFiles(".\\BepInEx\\plugins\\BRCreatorRacePlugin", "*.json").FirstOrDefault();

            if (!string.IsNullOrEmpty(filePath))
            {
                Plugin.Log.LogInfo("Race found!");

                var tmp = RaceConfig.ToRaceConfig(filePath);

                Plugin.Log.LogInfo($"Stage : {tmp.Stage}");

                currentRaceConfig = tmp;

                state = RaceState.Loaded;
                OnRaceInitialize();
            }


        }

        public bool IsInRace()
        {
            return state != RaceState.None && state != RaceState.ShowRanking;
        }

        public void LoadConf(RaceConfig raceConfig)
        {
            currentRaceConfig = raceConfig;
            time = 0;

            Core.Instance.AudioManager.PlaySfx(SfxCollectionID.MenuSfx, AudioClipID.confirm);

            OnRaceInitialize();
        }

        public void OnRaceInitialize()
        {
            var instance = Traverse.Create(typeof(BaseModule)).Field("instance").GetValue<BaseModule>();
            var currentStage = Traverse.Create(instance).Field("currentStage").GetValue<Stage>();

            if (currentRaceConfig.Stage.ToBRCStage() != currentStage)
            {
                Plugin.Log.LogInfo("Switching stage to " + currentRaceConfig.Stage);
                shouldLoadRaceStageASAP = true;
                state = RaceState.LoadingStage;

                return;
            }

            Plugin.Log.LogInfo("Stage is already loaded!");

            AdditionalRaceInitialization();
        }

        public void AdditionalRaceInitialization()
        {
            checkpointPins = new Queue<CheckpointPin>(currentRaceConfig!.MapPins.Count());

            foreach (var mapPin in currentRaceConfig.MapPins)
            {
                var checkpointPin = Mapcontroller.Instance.InstantiateRaceCheckPoint(mapPin.ToVector3());

                checkpointPin.Pin.gameObject.SetActive(false);
                checkpointPins.Enqueue(checkpointPin);
            }

            var player = WorldHandler.instance.GetCurrentPlayer();
            player.tf.position = currentRaceConfig.StartPosition.ToVector3().ToUnityVector3();
            player.motor.SetVelocityTotal(Vector3.zero, Vector3.zero, Vector3.zero);
            player.StopCurrentAbility();
            player.boostCharge = 0;

            ////Respawn all boost pickups
            UnityEngine.Object.FindObjectsOfType<Pickup>()
                .Where((pickup) => pickup.pickupType is Pickup.PickUpType.BOOST_CHARGE || pickup.pickupType is Pickup.PickUpType.BOOST_BIG_CHARGE)
                .ToList()
                .ForEach((boost) =>
                {
                    boost.SetPickupActive(true);
                });

            Mapcontroller.Instance.UpdateOriginalPins(false);

            OnRaceStart();
        }

        public void OnRaceStart()
        {
            Plugin.ShouldIgnoreInput = true;
            state = RaceState.Starting;
        }

        public bool OnCheckpointReached(MapPin mapPin)
        {
            var currentCheckpointPin = checkpointPins.Peek();
            if (currentCheckpointPin.Pin != mapPin)
            {
                return false;
            }

            if (checkpointPins.Count > 0)
            {
                currentCheckpointPin.Deactivate();

                checkpointPins.Dequeue();

                if (checkpointPins.Count > 0)
                {
                    var checkpointPin = checkpointPins.Peek();
                    checkpointPin.Activate();
                }
                else
                {
                    state = RaceState.Finished;
                }

                Core.Instance.AudioManager.PlaySfx(SfxCollectionID.MenuSfx, AudioClipID.confirm);

                return true;
            }

            return false;
        }

        private void OnRaceFinished()
        {
            state = RaceState.WaitingForFullRanking;
        }


        public void OnStart()
        {
            if (IsInRace())
            {
                Plugin.Log.LogDebug("You are already in a race !");
                return;
            }

            state = RaceState.WaitingForRace;
        }

        private void ResetAll()
        {
            state = RaceState.None;
            time = 0;
            checkpointPins = new Queue<CheckpointPin>();
            shouldLoadRaceStageASAP = false;
            hasAdditionRaceConfigToLoad = false;
            currentRaceConfig = null;

            var uiManager = Core.Instance.UIManager;
            var gameplayUI = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
            gameplayUI.challengeGroup.SetActive(false);
            gameplayUI.timeLimitLabel.text = "";

            Mapcontroller.Instance.UpdateOriginalPins(true);
            var currentPlayer = WorldHandler.instance.GetCurrentPlayer();
            currentPlayer.normalBoostSpeed = RaceVelocityModifier.OriginalBoostSpeedTarget;
        }


        private void ShowTimer()
        {
            var uiManager = Core.Instance.UIManager;
            var gameplayUI = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
            gameplayUI.challengeGroup.SetActive(true);

            gameplayUI.timeLimitLabel.text = GetTimeFormatted(time).ToString();
        }

        private void ShowTimerReverse()
        {
            var uiManager = Core.Instance.UIManager;
            var gameplayUI = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
            gameplayUI.challengeGroup.SetActive(true);

            gameplayUI.timeLimitLabel.text = GetTimeFrom().ToString();
        }


        public bool HasStarted()
        {
            return state == RaceState.Racing;
        }


        public CheckpointPin GetNextCheckpointPin()
        {
            if (checkpointPins == null || checkpointPins.Count == 0)
            {
                return null;
            }

            return checkpointPins.Peek();
        }

        public bool ShouldLoadRaceStageASAP()
        {
            return shouldLoadRaceStageASAP;
        }

        public void SetShouldLoadRaceStageASAP(bool shouldLoadRaceStageASAP)
        {
            this.shouldLoadRaceStageASAP = shouldLoadRaceStageASAP;
        }

        public bool HasAdditionRaceConfigToLoad()
        {
            return hasAdditionRaceConfigToLoad;
        }

        public void SetHasAdditionRaceConfigToLoad(bool hasAdditionRaceConfigToLoad)
        {
            this.hasAdditionRaceConfigToLoad = hasAdditionRaceConfigToLoad;
        }

        private int GetTimeInSecs()
        {
            return (int)time % 60;
        }

        public float GetDeltaTime()
        {
            return time;
        }

        public Stage GetStage()
        {
            return currentRaceConfig.Stage.ToBRCStage();
        }

        internal RaceState GetState()
        {
            return state;
        }

        private string GetTimeFormatted(float time)
        {
            var minutes = Mathf.FloorToInt(time / 60);
            var seconds = Mathf.FloorToInt(time % 60);
            var fraction = Mathf.FloorToInt(time * 100 % 100);
            return $"{minutes:00}:{seconds:00}:{fraction:00}";
        }

        private string GetTimeFrom(int from = 3)
        {
            var time = GetDeltaTime();
            var seconds = Mathf.FloorToInt(from - time);
            var ms = Mathf.FloorToInt((from - time) * 100 % 100);
            return $"{seconds:00}:{ms:00}";
        }

        internal bool IsStarting()
        {
            return state == RaceState.Starting;
        }
    }
}


