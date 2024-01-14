using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LC_HandHeldDevice.Patches;
using UnityEngine;
using LethalLib.Modules;
using GameNetcodeStuff;
using System.Collections;
using System.IO;
using System.Linq;
using System;
using Unity.Netcode;

namespace LC_HandHeldDevice
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class HandHeldMapMod : BaseUnityPlugin
    {
        private const string modGUID = "Mellowdy.HandHeldMapMod";
        private const string modName = "HandHeldMapMod";
        private const string modVersion = "0.0.1";

        private const string assetName = "map.device";
        private const string gameObjectName = "HandHeldMapDevice.prefab";


        private readonly Harmony harmony = new Harmony(modGUID);

        public static ManualLogSource mls;
        private static HandHeldMapMod instance;



        static string Name = "Hand-held Map";

        static ConfigEntry<int> price;
        static ConfigEntry<float> BatteryUsage;
        static ConfigEntry<bool> BatteryFail;
        static ConfigEntry<float> FailureBreakPoint;
        static ConfigEntry<float> BaseChance;

        static Vector3 positionInHand = new Vector3(-0.08f, -0.05f, 0.07f);
        static Vector3 rotationInHand = new Vector3(-165, -75f, 0);

        static float size = 4f;
        

        void Awake()
        {
            if (instance == null) instance = this;

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            harmony.PatchAll(typeof(HandHeldMapMod));


            //Assign Config Settings

            price = Config.Bind<int>("Price", "MapPrice", 80, "Credits needed to buy the hand-held map");
            
            BatteryUsage = Config.Bind<float>("Battery", "Battery Charge", 100, "The amount of time in seconds the map can be active");
            BatteryFail = Config.Bind<bool>("Battery", "Battery Failure", true, "decription");
            FailureBreakPoint = Config.Bind<float>("Battery", "Battery Failure Break Point", 0.5f, "The charge breakpoint where the map will begin to glitch and fail to update as often (0.5 = map will start failing at 50% charge)");
            BaseChance = Config.Bind<float>("Battery", "Base Chance", 0.3f, "The base chance for the map to update every second after battery failure (0.5 = 50% chance per second)") ;
            
            HandHeldMap.BatteryUsage = BatteryUsage.Value;
            HandHeldMap.BatteryFail = BatteryFail.Value;
            HandHeldMap.FailureBreakPoint = FailureBreakPoint.Value;
            HandHeldMap.BaseChance = BaseChance.Value;


            //Asset Bundle

            string currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(currentDirectory, assetName).Replace("\\", "/");
            AssetBundle assets = AssetBundle.LoadFromFile(path);

            Item map = ScriptableObject.CreateInstance<Item>();

            GameObject mapObject = assets.LoadAsset<GameObject>(gameObjectName);


            //Sounds

            HandHeldMap.turnOn = assets.LoadAsset<AudioClip>("On.wav");
            HandHeldMap.turnOff = assets.LoadAsset<AudioClip>("Off.wav");

            map.grabSFX = assets.LoadAsset<AudioClip>("Grab.wav");
            map.pocketSFX = assets.LoadAsset<AudioClip>("Pocket.wav");
            map.dropSFX = assets.LoadAsset<AudioClip>("Drop.wav");
            map.throwSFX = map.dropSFX;

            //Item
            #region

            map.name = Name;
            map.itemName = Name;

            map.restingRotation = new Vector3(0, 90, 0);
            map.canBeGrabbedBeforeGameStart = false;
            map.isConductiveMetal = true;
            map.isScrap = false;
            map.canBeInspected = false;
            map.itemIcon = assets.LoadAsset<Sprite>("Icon.png");


            map.requiresBattery = true;
            map.automaticallySetUsingPower = true;


            map.rotationOffset = rotationInHand;
            var positions = positionInHand / 3f * size;
            map.positionOffset = new Vector3(positions.y, positions.z, positions.x);

            map.toolTips = new string[] {"Toggle Screen : [LMB]"};

            #endregion
            
            //GameObject
            #region

            //Assign Components

            mapObject.transform.localScale = Vector3.one * size;

            mapObject.AddComponent<NetworkObject>();
            mapObject.AddComponent<AudioSource>();
            HandHeldMap hhm = mapObject.AddComponent<HandHeldMap>();
            hhm.originalScale = mapObject.transform.localScale;
            hhm.grabbable = true;
            hhm.itemProperties = map;
            hhm.floorYRot = -1;


            //Scan Node

            var nodeObject = new GameObject("Node");
            nodeObject.transform.parent = mapObject.transform;
            nodeObject.transform.localPosition = Vector3.zero;
            nodeObject.layer = LayerMask.NameToLayer("ScanNode");
            ScanNodeProperties scanNode = nodeObject.AddComponent<ScanNodeProperties>();
            scanNode.headerText = Name;
            scanNode.subText = "An employee's best friend.";

            map.spawnPrefab = mapObject;

            #endregion

            Items.RegisterItem(map);
            Items.RegisterShopItem(map, null, null, CreateInfoNode("Hand-heldMapDevice", "Allows easy viewing of the map from anywhere."), price.Value);
            mls.LogInfo("HandHeldMap mod is active");
        }
        private TerminalNode CreateInfoNode(string name, string description)
        {
            TerminalNode val = ScriptableObject.CreateInstance<TerminalNode>();
            val.clearPreviousText = true;
            val.name = name + "InfoNode";
            val.displayText = description + "\n\n";
            return val;
        }
    }
}
