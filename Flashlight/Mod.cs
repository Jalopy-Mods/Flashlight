using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using JaLoader;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Flashlight
{
    public class Flashlight : Mod
    {
        public override string ModID => "Flashlight";
        public override string ModName => "Flashlight";
        public override string ModAuthor => "MeblIkea";
        public override string ModDescription => "Add a flashlight (in the car).";
        public override string ModVersion => "1.0.0";
        public override WhenToInit WhenToInit => WhenToInit.InGame; // OR WhenToInit.InMenu (In menu is both)
        public override bool UseAssets => true; // Does your mod use custom asset bundles?

        public override void Start()
        {
            var patcher = new Harmony("com.flashlight.meb");
            patcher.Patch(typeof(ObjectPickupC).GetMethod(nameof(ObjectPickupC.SetToReturnObject)), prefix: new HarmonyMethod(GetType().GetMethod(nameof(FlashlightMoved))));
            patcher.Patch(typeof(ObjectPickupC).GetMethod("MoveToSlot2"), postfix: new HarmonyMethod(GetType().GetMethod(nameof(FlashlightMoved))));
        }

        // ReSharper disable once InconsistentNaming
        public static void FlashlightMoved(ObjectPickupC __instance)
        {
            if (__instance.gameObject.name != "Flashlight_FlashLight") return;
            __instance.gameObject.transform.GetChild(0).GetChild(0).GetComponent<Light>().enabled = false;
            __instance.gameObject.GetComponent<FlashlightBehaviour>().Reconsider();
        }


        public override void Awake()
        {

            var lightPrefab = Instantiate(LoadAsset("flashlight", "FlashLight", ""));
            ModHelper.Instance.AddBasicObjectLogic(lightPrefab, "Flashlight", "Flashlight shine bright in night-time",
                30, 1, false, false);
            ModHelper.Instance.AdjustCustomObjectPosition(lightPrefab, new Vector3(0, 0, -90), new Vector3(0, -0.3f, 0));
            lightPrefab.transform.localScale = new Vector3(10, 10, 10);
            var spot = lightPrefab.transform.GetChild(0).GetChild(0).GetComponent<Light>();
            spot.enabled = false;
            spot.range = 100;
            spot.intensity = 1;

            var assetBundle = AssetBundle.LoadFromFile(Path.Combine(AssetsPath, "flashlight"));
            var onSounds = new List<AudioClip>();
            var offSounds = new List<AudioClip>();
            for (var i = 0; i < 4; i++)
            {
                onSounds.Add(assetBundle.LoadAsset<AudioClip>("on" + i));
                offSounds.Add(assetBundle.LoadAsset<AudioClip>("off" + i));
            }
            var flashlightBehaviour = lightPrefab.AddComponent<FlashlightBehaviour>();
            flashlightBehaviour.onSounds = onSounds.ToArray();
            flashlightBehaviour.offSounds = offSounds.ToArray();

            var objToRender = new List<GameObject> { lightPrefab };
            for (var i = 0; i < lightPrefab.GetComponentsInChildren<Renderer>().Length; i++)
                objToRender.Add(lightPrefab.GetComponentsInChildren<Renderer>()[i].gameObject);
            lightPrefab.GetComponent<ObjectPickupC>().renderTargets = objToRender.ToArray();
            // CustomObjectsManager.Instance.RegisterObject(lightPrefab, "flashlight");
            // CustomObjectsManager.Instance.SpawnObject("flashlight");
        }
    }

    public class FlashlightBehaviour : MonoBehaviour
    {
        public AudioClip[] onSounds;
        public AudioClip[] offSounds;

        public void Start()
        {
            var frame = GameObject.Find("FrameHolder/TweenHolder/Frame");
            var lightLoc = new GameObject
            {
                transform =
                {
                    parent = frame.transform,
                    localPosition = frame.transform.Find("WalletLoc").localPosition - new Vector3(0, 0.3f, 1),
                    localScale = new Vector3(10, 10, 10)
                },
                name = "FlashlightLoc"
            };
            lightLoc.AddComponent<HoldingLogicC>();
            GetComponent<ObjectPickupC>().returnObject = lightLoc;
            GetComponent<ObjectPickupC>().targetDropOff = lightLoc;
            var rotation = lightLoc.transform.localRotation;
            lightLoc.transform.localPosition += new Vector3(0, 0, 0.5f);
            lightLoc.transform.localRotation = Quaternion.Euler(rotation.eulerAngles - new Vector3(0, 60, 180));
            lightLoc.transform.localScale *= -1;
            GetComponent<ObjectPickupC>().returnPosition = lightLoc.transform;
            GetComponent<ObjectPickupC>().ThrowLogic();
        }

        public void Reconsider()
        {
            var light = transform.GetChild(0).GetChild(0).GetComponent<Light>();
            transform.GetChild(2).GetComponent<AudioSource>().PlayOneShot(light.enabled
                ? onSounds[Random.Range(0, onSounds.Length)] : offSounds[Random.Range(0, offSounds.Length)]);
            var button = transform.GetChild(1);
            var buttonNewPos = button.localPosition + new Vector3(0, 0, !light.enabled? -0.005f : 0.005f);
            iTween.MoveTo(button.gameObject,
                iTween.Hash("position", buttonNewPos, "time", 0.02f, "islocal", true, "easetype",
                    iTween.EaseType.linear));
        }

        public void Update()
        {
            if (gameObject.layer != 11 || !Input.GetMouseButtonDown(0) || transform.parent.name != "CarryHolder1") return;
            var light = transform.GetChild(0).GetChild(0).GetComponent<Light>();
            light.enabled = !light.enabled;
            Reconsider();
        }
    }
}
