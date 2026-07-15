using UnityEditor;
using UnityEngine;

namespace FirstForm.Editor
{
    /// <summary>
    /// 런타임 캐릭터 애니메이션 프레임을 공통 도트 스프라이트 설정으로 가져옵니다.
    /// </summary>
    public sealed class FirstFormCharacterAnimationImporter : AssetPostprocessor
    {
        private const string AnimationPathMarker = "/Characters/Prototype/Animations/";

        /// <summary>
        /// 투명 여백과 픽셀 경계를 보존해 모든 프레임의 피벗이 흔들리지 않게 합니다.
        /// </summary>
        private void OnPreprocessTexture()
        {
            string normalizedPath = assetPath.Replace('\\', '/');
            if (!normalizedPath.Contains(AnimationPathMarker))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
        }
    }
}
