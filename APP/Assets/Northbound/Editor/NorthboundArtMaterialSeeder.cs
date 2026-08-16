using UnityEditor;
using UnityEngine;

namespace Northbound.Editor
{
    public static class NorthboundArtMaterialSeeder
    {
        private const string MaterialPath = "Assets/Northbound/Resources/Northbound/ChromaKeySprite.mat";

        public static void Rebuild()
        {
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null) return;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Northbound/Resources/Northbound/ChromaKeySprite.shader");
            if (shader == null) throw new System.InvalidOperationException("Chroma key shader did not import.");
            AssetDatabase.CreateAsset(new Material(shader), MaterialPath);
            AssetDatabase.SaveAssets();
        }
    }
}
