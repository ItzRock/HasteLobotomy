using HarmonyLib;
using UnityEngine;
using Zorro.Settings;
using Landfall.Modding;
using System.Reflection;
using UnityEngine.Localization;
using System.Collections;
using Landfall.Haste;
using UnityEngine.Audio;
using Unity.Mathematics;
namespace Lobotomy;

[LandfallPlugin]
public class Lobotomy {
    public static Harmony harmony;
    public static string GUID = "AnthonyStai.Lobotomy";
    public static List<Vector3> positions;
    public static bool Enabled = true;
    public static bool SoundOnly = false;
    public static float volume = 0.7f;
    public static GameObject sfxPrefab;
    public static float counter = 0;
    public static GameObject player;
    public static string AssemblyDirectory {
        get {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }

    static Lobotomy() {
        Debug.Log($"Loading {GUID} running in {AssemblyDirectory}");
        Debug.Log("Loading assetbundle");

        AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(AssemblyDirectory, "lobotomy.assetbundle"));
        if (assetBundle == null) {
            Debug.Log("Failed to load AssetBundle!");
            return;
        }

        sfxPrefab = assetBundle.LoadAsset<GameObject>("SFX");
        sfxPrefab.GetComponent<AudioSource>().playOnAwake = false;
        positions = new();
        harmony = new(GUID);
        harmony.PatchAll();

    }
    public static bool Lobotomizing = false;
    public static IEnumerator Lobotomize(GameObject player) {
        if (!Lobotomizing && Enabled) {
            Lobotomizing = true;
            GameObject clone = MonoBehaviour.Instantiate(sfxPrefab);
            clone.GetComponent<AudioSource>().volume = volume;
            clone.GetComponent<AudioSource>().Play();
            if (!SoundOnly) {
                int icePicks = 3;
                for (int i = 0; i < icePicks; i++) {
                    float time = UnityEngine.Random.Range(.1f, .6f);
                    float timer = 0f;
                    while (timer < time) {
                        float timeScale = Mathf.Lerp(1f, 20f, timer / time);
                        Time.timeScale = timeScale;
                        timer += Time.unscaledDeltaTime;
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(.5f);
                }
                Time.timeScale = 0;
                float timer2 = 0f;
                float endTime = 0.75f;
                Vector3 posStart = player.transform.position;
                Vector3 posEnd = positions[0];
                while (timer2 < endTime) {
                    player.transform.position = Vector3.Lerp(posStart, posEnd, timer2 / endTime);
                    timer2 += Time.unscaledDeltaTime;
                    yield return null;
                }
                player.transform.position = posEnd;
                Time.timeScale = 1;
            }
            Lobotomizing = false;
        } else Debug.Log("Already Lobotomizing");
    }
}



[HarmonyPatch(typeof(PlayerMovement))]
public class PlayerMovementPatches {
    [HarmonyPatch(nameof(PlayerMovement.Start))]
    [HarmonyPostfix]
    private static void StartPostfix(PlayerMovement __instance) {
        Lobotomy.Lobotomizing = false;
        if(Lobotomy.Enabled) __instance.badLandingThreshold = 0.85f;
        Lobotomy.player = __instance.gameObject;
    }
    [HarmonyPatch(nameof(PlayerMovement.BadLanding))]
    [HarmonyPostfix]
    private static void BadLandingPostfix(PlayerMovement __instance) {
        __instance.StartCoroutine(Lobotomy.Lobotomize(__instance.gameObject));
    }
    [HarmonyPatch(nameof(PlayerMovement.Update))]
    [HarmonyPostfix]
    private static void UpdatePostfix(PlayerMovement __instance) {
        Lobotomy.counter += Time.deltaTime;
        if(Lobotomy.counter > .5f) {
            Lobotomy.counter = 0f;
            if (Lobotomy.positions.Count >= 4) Lobotomy.positions.RemoveAt(0);
            Lobotomy.positions.Add(__instance.transform.position);
        }
    }
}
[HarmonyPatch(typeof(Player))]
public class PlayerPatches {
    [HarmonyPatch(nameof(Player.TakeDamage))]
    [HarmonyPostfix]
    private static void TakeDamagePostfix(Player __instance) {
        __instance.StartCoroutine(Lobotomy.Lobotomize(Lobotomy.player));
    }
}
[HasteSetting]
public class LobotomySetting : OffOnSetting, IExposedSetting {
    public override void ApplyValue() {
        Lobotomy.Enabled = base.Value == OffOnMode.ON;
    }

    public string GetCategory() => "Lobotomy";

    // Token: 0x0600062A RID: 1578 RVA: 0x00024CD8 File Offset: 0x00022ED8
    public override OffOnMode GetDefaultValue() {
        return OffOnMode.ON;
    }

    public LocalizedString GetDisplayName() => new UnlocalizedString("Enable Lobotomy Mod?");

    // Token: 0x0600062B RID: 1579 RVA: 0x00024CEC File Offset: 0x00022EEC
    public override List<LocalizedString> GetLocalizedChoices() {
        return new List<LocalizedString>
        {
            new LocalizedString("Settings", "DisabledGraphicOption"),
            new LocalizedString("Settings", "EnabledGraphicOption")
        };
    }
}
[HasteSetting]
public class SoundOnlySetting : OffOnSetting, IExposedSetting {
    public override void ApplyValue() {
        Lobotomy.SoundOnly = base.Value == OffOnMode.ON;
    }

    public string GetCategory() => "Lobotomy";
    public override OffOnMode GetDefaultValue() {
        return OffOnMode.OFF;
    }
    public LocalizedString GetDisplayName() => new UnlocalizedString("Play only the sound?");
    public override List<LocalizedString> GetLocalizedChoices() {
        return new List<LocalizedString>
        {
            new LocalizedString("Settings", "DisabledGraphicOption"),
            new LocalizedString("Settings", "EnabledGraphicOption")
        };
    }
}

[HasteSetting]
public class LobotomyVolume : FloatSetting, IExposedSetting {
    public override void ApplyValue() {
        Lobotomy.volume = Mathf.Clamp(base.Value,0,1);
    }
    public string GetCategory() => "Lobotomy";
    public override float GetDefaultValue() => 0.7f;
    public LocalizedString GetDisplayName() => new UnlocalizedString("Volume between 0-1");
    public override float2 GetMinMaxValue() => new float2(0, 1);
}