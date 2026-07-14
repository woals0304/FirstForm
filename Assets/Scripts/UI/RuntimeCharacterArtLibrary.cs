using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 런타임 시각 프로토타입에서 사용할 캐릭터 스프라이트 경로와 적별 매핑을 관리합니다.
    /// 새 적 스프라이트를 추가할 때 Resources 경로와 매핑만 이 클래스에 확장하면 됩니다.
    /// </summary>
    internal static class RuntimeCharacterArtLibrary
    {
        private const string PlayerResourcePath = "FirstForm/Characters/Prototype/player_disciple";
        private const string StrongholdLeaderResourcePath = "FirstForm/Characters/Prototype/enemy_stronghold_leader";

        private static Sprite playerSprite;
        private static Sprite strongholdLeaderSprite;
        private static bool playerLoadAttempted;
        private static bool strongholdLeaderLoadAttempted;

        /// <summary>
        /// 이름 없는 제자의 임시 전신 스프라이트를 반환합니다.
        /// </summary>
        internal static Sprite GetPlayerSprite()
        {
            if (!playerLoadAttempted)
            {
                playerLoadAttempted = true;
                playerSprite = LoadSprite(PlayerResourcePath, "PlayerDiscipleRuntimeSprite");
            }

            return playerSprite;
        }

        /// <summary>
        /// 실제 임시 스프라이트가 준비된 적만 반환하고, 나머지는 기존 실루엣을 사용하게 합니다.
        /// </summary>
        internal static bool TryGetEnemySprite(EnemyArchetype archetype, out Sprite sprite)
        {
            sprite = null;
            if (archetype != EnemyArchetype.StrongholdLeader)
            {
                return false;
            }

            if (!strongholdLeaderLoadAttempted)
            {
                strongholdLeaderLoadAttempted = true;
                strongholdLeaderSprite = LoadSprite(StrongholdLeaderResourcePath, "StrongholdLeaderRuntimeSprite");
            }

            sprite = strongholdLeaderSprite;
            return sprite != null;
        }

        /// <summary>
        /// Sprite 또는 일반 Texture2D 임포트 모두 지원해 프로토타입 리소스 추가 절차를 단순하게 유지합니다.
        /// </summary>
        private static Sprite LoadSprite(string resourcePath, string runtimeName)
        {
            Sprite importedSprite = Resources.Load<Sprite>(resourcePath);
            if (importedSprite != null)
            {
                return importedSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning("[FirstForm] 캐릭터 임시 스프라이트를 찾지 못했습니다: Resources/" + resourcePath);
                return null;
            }

            Sprite runtimeSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect);
            runtimeSprite.name = runtimeName;
            runtimeSprite.hideFlags = HideFlags.DontSave;
            return runtimeSprite;
        }
    }
}
