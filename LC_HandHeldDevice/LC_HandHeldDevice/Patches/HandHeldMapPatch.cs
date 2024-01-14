using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LC_HandHeldDevice.Patches
{
    public class HandHeldMap : GrabbableObject
    {
        ManualCameraRenderer mcr;
        RenderTexture renderTexture;

        public static AudioClip turnOn;
        public static AudioClip turnOff;

        public static float BatteryUsage;//In seconds
        public static bool BatteryFail;
            public static float FailureBreakPoint;
            public static float BaseChance;

        public AudioSource audioSource;
        public float MaxCharge = 0;
        bool isOn = false;
        

        public override void Start()
        {
            base.Start();

            insertedBattery = new Battery(isEmpty: false, 1f);
            MaxCharge = insertedBattery.charge;
            itemProperties.batteryUsage = BatteryUsage;

            //Assure correct spawning

            if (isInShipRoom && isInElevator)
            {
                transform.SetParent(StartOfRound.Instance.elevatorTransform, false);
                targetFloorPosition = transform.localPosition - transform.parent.position;
            }

            mainObjectRenderer = GetComponent<MeshRenderer>();
            audioSource = GetComponent<AudioSource>();

            //Setting up ManualCameraRenderer
            #region

            ManualCameraRenderer originalMCR = GameObject.Find("CameraMonitorScript").GetComponent<ManualCameraRenderer>();

            //Set up MCR
            mcr = gameObject.AddComponent<ManualCameraRenderer>();
            mcr.mesh = mainObjectRenderer;
            var g = Instantiate(originalMCR.cam);
            Camera camera = g.GetComponent<Camera>();

            var animator = camera.GetComponent<Animator>();
            if(animator == null) animator = gameObject.AddComponent<Animator>();
            mcr.mapCameraAnimator = animator;
            mcr.cam = camera;
            mcr.mapCamera = camera;
            mcr.shipArrowPointer = originalMCR.shipArrowPointer;
            mcr.shipArrowUI = originalMCR.shipArrowUI;

            //Material

            mcr.materialIndex = 0;
            renderTexture = new RenderTexture(camera.targetTexture.width, camera.targetTexture.height, camera.targetTexture.depth);
            camera.targetTexture = renderTexture;
            mcr.onScreenMat = new Material(originalMCR.onScreenMat);
            mcr.onScreenMat.mainTexture = renderTexture;
            mcr.offScreenMat = originalMCR.offScreenMat;
            TurnOn(false);

            //Add target

            mcr.radarTargets.Clear();
            mcr.AddTransformAsTargetToRadar(transform, "You are here", true);

            #endregion
        }

        public override void Update()
        {
            base.Update();

            if (isHeld)
            {
                if(insertedBattery.charge/MaxCharge < FailureBreakPoint && BatteryFail)
                {
                    float chance = (BaseChance + insertedBattery.charge/MaxCharge) * Time.deltaTime;
                    float random = UnityEngine.Random.Range(0, 1f);
                    if (random <= chance) mcr.cam.Render();
                }
                else mcr.cam.Render();
            }
        }


        public void TurnOn(bool on)
        {
            bool change = isOn != on;

            isOn = on;
            isBeingUsed = on;
            mcr.SwitchScreenOn(on);
            if (change)
            {
                if (on) audioSource.PlayOneShot(turnOn);
                else audioSource.PlayOneShot(turnOff);
                RoundManager.Instance.PlayAudibleNoise(transform.position, 10f, 0.65f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            }
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            TurnOn(used);
        }
        public override void UseUpBatteries()
        {
            base.UseUpBatteries();
            TurnOn(false);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            TurnOn(false);
        }
        public override void ChargeBatteries()
        {
            base.ChargeBatteries();
            MaxCharge = insertedBattery.charge;
        }


        public override void OnDestroy()
        {
            base.OnDestroy();
            renderTexture.Release();
        }
    }
}
