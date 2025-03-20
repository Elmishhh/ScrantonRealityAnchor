using BepInEx;
using BepInEx.Logging;
using ScrantonRealityAnchor.Behaviours;
using System.IO;
using System.Reflection;
using UnityEngine;
using PluginInfo = ScrantonRealityAnchor.MyPluginInfo;
using LethalLib;
using LethalLib.Modules;

namespace ScrantonRealityAnchor
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        public static Item SRAItemProps;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {PluginInfo.Name} is loaded!");
            LoadItem();
            NetworkPatch();
        }

        private void NetworkPatch()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        public void LoadItem()
        {
            using (Stream bundlestream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrantonRealityAnchor.Resources.anchorassets"))
            {
                AssetBundle bundle = AssetBundle.LoadFromStream(bundlestream);
                if (bundle == null)
                {
                    Logger.LogError("SRA Bundle is null");
                    return;
                }
                SRAItemProps = bundle.LoadAsset<Item>("SRAprops");

                SRAItem SRA = SRAItemProps.spawnPrefab.AddComponent<SRAItem>();
                SRA.itemProperties = SRAItemProps;
                
                SRA.grabbable = true;
                SRA.grabbableToEnemies = false;

                SRA.activeSFX = bundle.LoadAsset<AudioClip>("active hum");

                SRA.SRA_AudioSource = SRA.gameObject.GetComponent<AudioSource>();
                SRA.SRA_AudioSource.spatialize = true;
            }
            Utilities.FixMixerGroups(SRAItemProps.spawnPrefab);

            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "???";

            Items.RegisterShopItem(SRAItemProps, price: 1000);
            NetworkPrefabs.RegisterNetworkPrefab(SRAItemProps.spawnPrefab);
        }
    }
}
