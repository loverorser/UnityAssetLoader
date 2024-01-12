using UnityEngine;
namespace DW.Asset
{
    [CreateAssetMenu(fileName = "DWAssetSettings", menuName = "DWAsset/Create Settings")]
    public class DWAssetSettings : ScriptableObject
    {
        [SerializeField]
        public string ResourcePrefix = "";
        [SerializeField]
        public string AssetDatabasePrefix = "Assets/GameAssets/";
        [SerializeField]
        public string AssetBundlePrefix = "";
        [SerializeField]
        public PackType PackType;

        [SerializeField]
        public int PackVersion;
    }
}