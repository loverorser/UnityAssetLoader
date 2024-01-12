using System.IO;
using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using DW.Asset;
using UnityEngine.Networking;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Load : MonoBehaviour
{
    [SerializeField]
    MeshRenderer m_MeshRenderer;
    string s;
    void Log(object o)
    {
        Debug.Log(o);
        s += "\n"+o.ToString();
    }
    public int LocalVersion
    {
        get
        {
            if (!PlayerPrefs.HasKey("LocalVersion"))
            {
                PlayerPrefs.SetInt("LocalVersion", -1);
            }
            return PlayerPrefs.GetInt("LocalVersion");
        }
        set
        {
            PlayerPrefs.SetInt("LocalVersion", value);
        }
    }
    IEnumerator CopyAll()
    {
        var platform = Application.platform;
        string suffix;
        if (platform == RuntimePlatform.WindowsEditor)
        {
            suffix = "StandaloneWindows64";
        }
        else if (platform == RuntimePlatform.WindowsPlayer)
        {
            suffix = "StandaloneWindows64";
        }
        else if (platform == RuntimePlatform.WebGLPlayer)
        {
            suffix = "WebGL";
        }
        else if (platform == RuntimePlatform.Android)
        {
            suffix = "Android";
        }
        else
        {
            throw new Exception();
        }
        var txt = Resources.Load<TextAsset>("StreamingAssetsFileList"+suffix);
        Log(txt);
        try
        {
            Directory.Delete(Application.persistentDataPath, true);
        }catch(Exception e)
        {
            Log(e);
        }

        foreach(var file in txt.text.Split('\n'))
        {
            if (file == "")
                continue;
            Debug.Log(file);
            
            
            UnityWebRequest www = UnityWebRequest.Get(Application.streamingAssetsPath+"/"+suffix+"/"+file);
            var r = www.SendWebRequest();
            while (!r.isDone)
            {
                yield return null;
                //Log(r.progress);
            }
            try
            {
                var prePath = Application.persistentDataPath + "/";
                //Log(r.webRequest);
                //Log(r.webRequest.downloadHandler);
                if (file.Contains("/"))
                {
                    if(!Directory.Exists(prePath + file.Substring(0,file.LastIndexOf('/'))))
                        Directory.CreateDirectory(prePath + file.Substring(0,file.LastIndexOf('/')));
                }
                File.WriteAllBytes(prePath + file, r.webRequest.downloadHandler.data);
                //Log(r.webRequest.downloadHandler.data.Length);
                //DownloadHandlerAssetBundle b = r.webRequest.downloadHandler as DownloadHandlerAssetBundle;
                //Log(b);
                //var ab = b.assetBundle;
                //Log(ab);
            }
            catch (Exception e)
            {
                Log(e.Message+"\n"+e.StackTrace);
            }
            yield return null;

        }
        
    }
    void LoadAll()
    {
        try
        {
            Log(File.Exists(Application.persistentDataPath + "/" + "materials.ab"));
            var ab=AssetBundle.LoadFromFile(Application.persistentDataPath + "/" + "materials.ab");
            Log(ab.GetAllAssetNames().Length);
        }
        catch(Exception e)
        {
            Log(e);
        }
    }
    
    // Start is called before the first frame update
    IEnumerator Start()
    {
        Log("data:    "+Application.dataPath);
        Log("Streaming:    "+Application.streamingAssetsPath);
        Log("Persistent:    "+Application.persistentDataPath);
        var t = Time.realtimeSinceStartup;
        yield return StartCoroutine(CopyAll());
        Log("CopyAllTime:" + (Time.realtimeSinceStartup - t));
        //LoadAll();
        m_MeshRenderer.material = DWAsset.Load<Material>("Materials/M.mat");
        //return;
        try
        {
            //var ab = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/materials.ab");
            //Log(ab.GetAllAssetNames()[0]);
        }
        catch(Exception e)
        {
            Log(e);
        }
        
        //第一步 复制StreamingAssets到persistent下
        //三个版本号 LocalVersion 游戏的 PackVersion 打包的 RemoteVersion 热更的
        Log($"本地版本:{LocalVersion}");
        Log($"打包版本:{DWAsset.Settings.PackVersion}");
        yield break;
        if (LocalVersion < DWAsset.Settings.PackVersion)
        {
            LocalVersion = DWAsset.Settings.PackVersion;
            //Directory.copy(Application.streamingAssetsPath, Application.persistentDataPath);
        }
        //Application.persistentDataPath
        var material = DW.Asset.DWAsset.Load<Material>("Materials/M.mat");
        m_MeshRenderer.material = material;
        Debug.Log(material);
        yield return null;
        
        try
        {
            File.Create(Application.streamingAssetsPath + "/streaming.txt");
        }catch(Exception e)
        {
            Log(e);
        }
        try
        {
            File.Create(Application.dataPath+ "/data.txt");
        }
        catch (Exception e)
        {
            Log(e);
        }
        try
        {
            File.WriteAllText(Application.persistentDataPath + "/persistent.txt", "persistent");
        }
        catch (Exception e)
        {
            Log(e);
        }
        try
        {
            var str=File.ReadAllText(Application.persistentDataPath + "/persistent.txt");
            Log("read:"+str);
        }catch(Exception e)
        {
            Log(e);
        }
        
        //m_MeshRenderer.material=
    }
    private void OnGUI()
    {
        GUILayout.Label(s);
    }
    // Update is called once per frame
    void Update()
    {

    }
}

namespace DW.Asset
{
    public interface ILoader
    {
        public T Load<T>(string path) where T : UnityEngine.Object;
    }
    public class ResourceLoader : ILoader
    {
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            return Resources.Load<T>(path);
        }
    }
    public class AssetBundleLoader : ILoader
    {
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            //Assets  / StreamingAssets / Materials / M.mat
            //先找到abm
            AssetBundleManifest abm = AssetBundle.LoadFromFile(Application.persistentDataPath+"/"+DWAsset.GetSuffix()).LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            //再找到bundle 默认第一个目录就是
            var bundleName = path.Substring(0, path.IndexOf('/')) + ".ab";
            Debug.Log(bundleName);
            //重要！！因为AB打包会自动小写，所以加载的时候也要小写 其他平台不区分大小写 webGL会有问题！

            bundleName = bundleName.ToLower();
            Debug.Log(bundleName);

            var dpdcs = abm.GetAllDependencies(bundleName);
            Debug.Log(dpdcs[0]);

            AssetBundle.LoadFromFile(Application.persistentDataPath +"/"+ dpdcs[0]);
            var ab = AssetBundle.LoadFromFile(Application.persistentDataPath+"/"+bundleName);
            
            /* 1、打包的时候 获取依赖 打到一个包
             * 如果有两个包 依赖1个包 不行
             * 2、打包的时候 获取依赖 放到文件里
             * 3、也读文件 读unity自动生成的
            //如果
            */
            //abm.GetAllDependencies()
            return ab.LoadAsset<T>(path.Substring(path.IndexOf('/')+1));
        }
    }
    public class AssetDatabaseLoader : ILoader
    {
        public T Load<T>(string path) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(path);
#endif
            throw new System.Exception();
        }
    }


    public static class DWAsset
    {
        public static string GetSuffix()
        {
            var platform=Application.platform;
            var suffix = "";
            if (platform == RuntimePlatform.WindowsEditor)
            {
                suffix = "StandaloneWindows64";
            }
            else if (platform == RuntimePlatform.WindowsPlayer)
            {
                suffix = "StandaloneWindows64";
            }
            else if (platform == RuntimePlatform.WebGLPlayer)
            {
                suffix = "WebGL";
            }
            else if (platform == RuntimePlatform.Android)
            {
                suffix = "Android";
            }
            else
            {
                throw new Exception();
            }
            return suffix;
        }
        static ILoader s_Loader;
        public static DWAssetSettings Settings
        {
            get
            {
                if (s_Settings == null)
                {
                    s_Settings = Resources.Load<DWAssetSettings>("DWAssetSettings");
#if UNITY_EDITOR
                    s_Settings = AssetDatabase.LoadAssetAtPath<DWAssetSettings>("Assets/Resources/DWAssetSettings.asset");
#endif
                }
                return s_Settings;
            }
        }
        static DWAssetSettings s_Settings;
        static DWAsset()
        {
            if (Settings.PackType == PackType.Resource)
            {
                s_Loader = new ResourceLoader();
            }
            else if (Settings.PackType == PackType.AssetDatabase)
            {
                s_Loader = new AssetDatabaseLoader();
            }
            else if (Settings.PackType == PackType.AssetBundle)
            {
                s_Loader = new AssetBundleLoader();
            }
        }
        /// <summary>
        /// 对外的加载接口
        /// 只需要传GameAssets下的相对路径即可，其他不用管
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public static T Load<T>(string path) where T : UnityEngine.Object
        {
            return s_Loader.Load<T>(GetPrefix() + GetSuffix(path));
        }
        private static string GetPrefix()
        {
            if (s_Settings.PackType == PackType.Resource)
            {
                return s_Settings.ResourcePrefix;
            }else if(s_Settings.PackType == PackType.AssetDatabase)
            {
                return s_Settings.AssetDatabasePrefix;
            }else if(s_Settings.PackType == PackType.AssetBundle)
            {
                return s_Settings.AssetBundlePrefix;
            }
            throw new System.Exception();
        }
        private static string GetSuffix(string rawPath)
        {
            if (s_Settings.PackType == PackType.Resource)
            {
                return rawPath.Substring(0, rawPath.LastIndexOf('.'));
            }
            else if (s_Settings.PackType == PackType.AssetDatabase)
            {
                return rawPath;
            }
            else if (s_Settings.PackType == PackType.AssetBundle)
            {
                return rawPath;
                return rawPath.Substring(0, rawPath.LastIndexOf('.'))+".ab";
            }
            throw new System.Exception();
        }
    }
    public enum PackType
    {
        Resource,
        AssetBundle,
        AssetDatabase,
    }
#if UNITY_EDITOR
    public static class DWPacker
    {
        [MenuItem("DWAsset/GenerateFileList")]
        public static void G()
        {
            GStandloneWindows64();
            GWebGL();
            GAndroid();
        }
        public static void GWebGL()
        {
            string list = "";
            var files = Directory.GetFiles("Assets/StreamingAssets/WebGL", "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                    continue;
                var rep = file.Replace('\\', '/');
                rep = rep[(rep.IndexOf('/') + 1)..];
                rep = rep[(rep.IndexOf('/') + 1)..];
                rep = rep[(rep.IndexOf('/') + 1)..];
                list += rep + "\n";
            }
            list = list[..^1];
            File.WriteAllText("Assets/Resources/StreamingAssetsFileListWebGL.txt", list);
        }
        public static void GStandloneWindows64()
        {
            string list = "";
            var files = Directory.GetFiles("Assets/StreamingAssets/StandaloneWindows64", "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                    continue;
                var rep = file.Replace('\\', '/');
                rep = rep[(rep.IndexOf('/') + 1)..];
                rep = rep[(rep.IndexOf('/') + 1)..];
                rep = rep[(rep.IndexOf('/') + 1)..];
                list += rep + "\n";
            }
            list = list[..^1];
            File.WriteAllText("Assets/Resources/StreamingAssetsFileListStandaloneWindows64.txt", list);
        }
        public static void GAndroid()
        {
            string list="";
            var files=Directory.GetFiles("Assets/StreamingAssets/Android","*", SearchOption.AllDirectories);
            foreach(var file in files)
            {
                if (file.EndsWith(".meta"))
                    continue;
                var rep = file.Replace('\\', '/');
                rep=rep[(rep.IndexOf('/') + 1)..];
                rep = rep[(rep.IndexOf('/') + 1)..];
                rep = rep[(rep.IndexOf('/') + 1)..];
                list += rep + "\n";
            }
            list = list[..^1];
            File.WriteAllText("Assets/Resources/StreamingAssetsFileListAndroid.txt", list);
        }
        [MenuItem("DWAsset/Pack")]
        public static void Pack()
        {
            if(Directory.Exists("Assets/StreamingAssets/" + BuildTarget.StandaloneWindows64))
            Directory.Delete("Assets/StreamingAssets/"+ BuildTarget.StandaloneWindows64, true);
            if (Directory.Exists("Assets/StreamingAssets/" + BuildTarget.WebGL))
                Directory.Delete("Assets/StreamingAssets/" + BuildTarget.WebGL, true);
            if (Directory.Exists("Assets/StreamingAssets/" + BuildTarget.Android))
                Directory.Delete("Assets/StreamingAssets/" + BuildTarget.Android, true);

            Directory.CreateDirectory("Assets/StreamingAssets/"+ BuildTarget.StandaloneWindows64);
            Directory.CreateDirectory("Assets/StreamingAssets/" + BuildTarget.WebGL);
            Directory.CreateDirectory("Assets/StreamingAssets/" + BuildTarget.Android);

            List<AssetBundleBuild> abbs = new List<AssetBundleBuild>();
            var dirs = Directory.GetDirectories("Assets/GameAssets");
            foreach (var dir in dirs)
            {
                AssetBundleBuild abb = new AssetBundleBuild();
                abb.assetBundleVariant = "ab";
                abb.assetBundleName = dir[(dir.LastIndexOf('\\') + 1)..];
                List<string> assetNames = new();
                var files = Directory.GetFiles(dir,"*", SearchOption.AllDirectories);
                foreach (var item in files)
                {
                    if (item.EndsWith(".meta"))
                        continue;
                    assetNames.Add(item.Replace('\\', '/'));
                }
                abb.assetNames = assetNames.ToArray();
                //abb.assetNames
                abbs.Add(abb);
                Debug.Log(abb.assetBundleName);
            }
            BuildPipeline.BuildAssetBundles("Assets/StreamingAssets/"+BuildTarget.StandaloneWindows64, abbs.ToArray(), BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
            BuildPipeline.BuildAssetBundles("Assets/StreamingAssets/" + BuildTarget.WebGL, abbs.ToArray(), BuildAssetBundleOptions.None, BuildTarget.WebGL);
            BuildPipeline.BuildAssetBundles("Assets/StreamingAssets/" + BuildTarget.Android, abbs.ToArray(), BuildAssetBundleOptions.None, BuildTarget.Android);
        }
    }
#endif
}