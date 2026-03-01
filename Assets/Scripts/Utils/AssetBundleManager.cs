using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RDOnline.Utils
{
    public class AssetBundleManager
    {
        public static AssetBundleManager instance = new();
        
        public AssetBundle sceneBundle;        
        public AssetBundle[] resourcesBundles;
        
        private readonly string LoadPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        private AssetBundleManager()
        {
            try
            {
                instance = this;
                sceneBundle = AssetBundle.GetAllLoadedAssetBundles()
                    .ToArray()
                    .First(a => a.name.Contains("rdol.scenes"));
                resourcesBundles = AssetBundle.GetAllLoadedAssetBundles()
                    .ToArray()
                    .Where(a => a.name.Contains("rdol.resources")).ToArray();
            
                if (sceneBundle == null)
                {
                    sceneBundle = AssetBundle.LoadFromFile(Path.Combine(LoadPath, "rdol.scenes.assets"));
                }
                if (resourcesBundles == null)
                {
                    resourcesBundles = Directory.GetFiles(LoadPath, "*.assets").ToList()
                        .Where(a => Path.GetFileName(a).StartsWith("rdol.resources")).Select(AssetBundle.LoadFromFile).ToArray();
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        public static bool DryLoad()
        {
            var tips = instance.LoadAsset<TextAsset>("tips.json");
            return tips != null;
        }
        
        public T LoadAsset<T>(string pathOrName) where T : UnityEngine.Object
        {
            T result = Resources.Load<T>(pathOrName);
            if (result != null) return result;
            foreach (var resourcesBundle in resourcesBundles)
            {
                result = resourcesBundle.LoadAsset<T>(pathOrName);
                try
                {
                    if (result == null)
                    {
                        result = resourcesBundle.LoadAsset<T>(resourcesBundle.GetAllAssetNames().First(a => a.Contains(pathOrName)));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }

                if (result != null && typeof(T) == typeof(Shader))
                {
                    //InternalErrorShader
                    if (result.name == "Hidden/InternalErrorShader") continue;
                }
                if (result != null) return result;
            }
            return result;
        }
    }
}