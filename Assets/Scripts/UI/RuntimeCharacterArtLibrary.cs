using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstForm
{
    internal enum RuntimeCharacterFrameState
    {
        Idle,
        Attack,
        Hit,
        StrongPrepare,
        StrongAttack,
        Death
    }

    /// <summary>
    /// 한 캐릭터의 대기, 공격, 피격 프레임과 런타임 표시 크기를 함께 보관합니다.
    /// </summary>
    internal sealed class RuntimeCharacterFrameSet
    {
        internal readonly Sprite[] idleFrames;
        internal readonly Sprite[] attackFrames;
        internal readonly Sprite[] hitFrames;
        internal readonly Sprite[] strongPrepareFrames;
        internal readonly Sprite[] strongAttackFrames;
        internal readonly Sprite[] deathFrames;
        internal readonly Vector2 artworkSize;
        internal readonly Vector2 artworkOffset;

        internal RuntimeCharacterFrameSet(
            Sprite[] idleFrames,
            Sprite[] attackFrames,
            Sprite[] hitFrames,
            Sprite[] strongPrepareFrames,
            Sprite[] strongAttackFrames,
            Sprite[] deathFrames,
            Vector2 artworkSize,
            Vector2 artworkOffset)
        {
            this.idleFrames = idleFrames;
            this.attackFrames = attackFrames;
            this.hitFrames = hitFrames;
            this.strongPrepareFrames = strongPrepareFrames;
            this.strongAttackFrames = strongAttackFrames;
            this.deathFrames = deathFrames;
            this.artworkSize = artworkSize;
            this.artworkOffset = artworkOffset;
        }

        /// <summary>
        /// 현재 애니메이션 상태에 맞는 프레임 배열을 반환합니다.
        /// </summary>
        internal Sprite[] GetFrames(RuntimeCharacterFrameState state)
        {
            switch (state)
            {
                case RuntimeCharacterFrameState.Attack:
                    return attackFrames;
                case RuntimeCharacterFrameState.Hit:
                    return hitFrames;
                case RuntimeCharacterFrameState.StrongPrepare:
                    return strongPrepareFrames != null ? strongPrepareFrames : idleFrames;
                case RuntimeCharacterFrameState.StrongAttack:
                    return strongAttackFrames != null ? strongAttackFrames : attackFrames;
                case RuntimeCharacterFrameState.Death:
                    return deathFrames != null ? deathFrames : hitFrames;
                default:
                    return idleFrames;
            }
        }
    }

    /// <summary>
    /// 런타임 시각 프로토타입에서 사용할 캐릭터 스프라이트 경로와 적별 매핑을 관리합니다.
    /// 새 적 스프라이트를 추가할 때 Resources 경로와 매핑만 이 클래스에 확장하면 됩니다.
    /// </summary>
    internal static class RuntimeCharacterArtLibrary
    {
        private const string PlayerResourcePath = "FirstForm/Characters/Prototype/player_disciple";
        private const string PlayerAnimationPath = "FirstForm/Characters/Prototype/Animations/player_disciple";
        private const int ExpectedIdleFrameCount = 4;
        private const int ExpectedAttackFrameCount = 4;
        private const int ExpectedHitFrameCount = 3;
        private const int PlayerStrongPrepareFrameCount = 3;
        private const int PlayerStrongAttackFrameCount = 5;
        private const int PlayerDeathFrameCount = 5;
        private const int SwiftScoutStrongPrepareFrameCount = 4;
        private const int SwiftScoutStrongAttackFrameCount = 6;
        private const int SwiftScoutDeathFrameCount = 5;
        private const int IronGuardStrongPrepareFrameCount = 4;
        private const int IronGuardStrongAttackFrameCount = 6;
        private const int IronGuardDeathFrameCount = 6;
        private const int EnergySapperStrongPrepareFrameCount = 4;
        private const int EnergySapperStrongAttackFrameCount = 5;
        private const int EnergySapperDeathFrameCount = 5;
        private const int BerserkerStrongPrepareFrameCount = 4;
        private const int BerserkerStrongAttackFrameCount = 6;
        private const int BerserkerDeathFrameCount = 6;

        private static readonly FrameSetEntry PlayerFrameSetEntry = new FrameSetEntry(
            PlayerAnimationPath,
            new Vector2(390f, 390f),
            new Vector2(0f, 18f),
            PlayerStrongPrepareFrameCount,
            PlayerStrongAttackFrameCount,
            PlayerDeathFrameCount);

        private static readonly Dictionary<EnemyArchetype, SpriteEntry> EnemySpriteEntries =
            new Dictionary<EnemyArchetype, SpriteEntry>
            {
                {
                    EnemyArchetype.SwiftScout,
                    new SpriteEntry(
                        "FirstForm/Characters/Prototype/enemy_swift_scout",
                        "SwiftScoutRuntimeSprite")
                },
                {
                    EnemyArchetype.IronGuard,
                    new SpriteEntry(
                        "FirstForm/Characters/Prototype/enemy_iron_guard",
                        "IronGuardRuntimeSprite")
                },
                {
                    EnemyArchetype.EnergySapper,
                    new SpriteEntry(
                        "FirstForm/Characters/Prototype/enemy_energy_sapper",
                        "EnergySapperRuntimeSprite")
                },
                {
                    EnemyArchetype.Berserker,
                    new SpriteEntry(
                        "FirstForm/Characters/Prototype/enemy_berserker",
                        "BerserkerRuntimeSprite")
                },
                {
                    EnemyArchetype.StrongholdLeader,
                    new SpriteEntry(
                        "FirstForm/Characters/Prototype/enemy_stronghold_leader",
                        "StrongholdLeaderRuntimeSprite")
                }
            };

        private static readonly Dictionary<EnemyArchetype, FrameSetEntry> EnemyFrameSetEntries =
            new Dictionary<EnemyArchetype, FrameSetEntry>
            {
                {
                    EnemyArchetype.SwiftScout,
                    new FrameSetEntry(
                        "FirstForm/Characters/Prototype/Animations/enemy_swift_scout",
                        new Vector2(460f, 460f),
                        new Vector2(0f, 42f),
                        SwiftScoutStrongPrepareFrameCount,
                        SwiftScoutStrongAttackFrameCount,
                        SwiftScoutDeathFrameCount)
                },
                {
                    EnemyArchetype.IronGuard,
                    new FrameSetEntry(
                        "FirstForm/Characters/Prototype/Animations/enemy_iron_guard",
                        new Vector2(500f, 500f),
                        new Vector2(0f, 50f),
                        IronGuardStrongPrepareFrameCount,
                        IronGuardStrongAttackFrameCount,
                        IronGuardDeathFrameCount)
                },
                {
                    EnemyArchetype.EnergySapper,
                    new FrameSetEntry(
                        "FirstForm/Characters/Prototype/Animations/enemy_energy_sapper",
                        new Vector2(470f, 470f),
                        new Vector2(0f, 46f),
                        EnergySapperStrongPrepareFrameCount,
                        EnergySapperStrongAttackFrameCount,
                        EnergySapperDeathFrameCount)
                },
                {
                    EnemyArchetype.Berserker,
                    new FrameSetEntry(
                        "FirstForm/Characters/Prototype/Animations/enemy_berserker",
                        new Vector2(480f, 480f),
                        new Vector2(0f, 50f),
                        BerserkerStrongPrepareFrameCount,
                        BerserkerStrongAttackFrameCount,
                        BerserkerDeathFrameCount)
                }
            };

        private static Sprite playerSprite;
        private static bool playerLoadAttempted;

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
        /// 플레이어 애니메이션 세트를 원자적으로 불러옵니다.
        /// 프레임이 하나라도 누락되면 false를 반환해 기존 단일 이미지로 되돌립니다.
        /// </summary>
        internal static bool TryGetPlayerFrameSet(out RuntimeCharacterFrameSet frameSet)
        {
            frameSet = PlayerFrameSetEntry.GetOrLoad();
            return frameSet != null;
        }

        /// <summary>
        /// 프레임 애니메이션이 준비된 적만 세트를 반환합니다.
        /// </summary>
        internal static bool TryGetEnemyFrameSet(EnemyArchetype archetype, out RuntimeCharacterFrameSet frameSet)
        {
            frameSet = null;
            FrameSetEntry entry;
            if (!EnemyFrameSetEntries.TryGetValue(archetype, out entry))
            {
                return false;
            }

            frameSet = entry.GetOrLoad();
            return frameSet != null;
        }

        /// <summary>
        /// 실제 임시 스프라이트가 준비된 적만 반환하고, 나머지는 기존 실루엣을 사용하게 합니다.
        /// </summary>
        internal static bool TryGetEnemySprite(EnemyArchetype archetype, out Sprite sprite)
        {
            sprite = null;
            SpriteEntry entry;
            if (!EnemySpriteEntries.TryGetValue(archetype, out entry))
            {
                return false;
            }

            if (!entry.loadAttempted)
            {
                entry.loadAttempted = true;
                entry.sprite = LoadSprite(entry.resourcePath, entry.runtimeName);
            }

            sprite = entry.sprite;
            return sprite != null;
        }

        /// <summary>
        /// 적별 리소스 경로와 지연 로드 캐시를 한곳에 보관합니다.
        /// 새 적 아트는 EnemySpriteEntries에 항목 하나를 추가해 연결할 수 있습니다.
        /// </summary>
        private sealed class SpriteEntry
        {
            internal readonly string resourcePath;
            internal readonly string runtimeName;
            internal bool loadAttempted;
            internal Sprite sprite;

            internal SpriteEntry(string resourcePath, string runtimeName)
            {
                this.resourcePath = resourcePath;
                this.runtimeName = runtimeName;
            }
        }

        /// <summary>
        /// 애니메이션 폴더와 표시 규격, 지연 로드 결과를 함께 캐시합니다.
        /// </summary>
        private sealed class FrameSetEntry
        {
            private readonly string rootPath;
            private readonly Vector2 artworkSize;
            private readonly Vector2 artworkOffset;
            private readonly int strongPrepareFrameCount;
            private readonly int strongAttackFrameCount;
            private readonly int deathFrameCount;
            private bool loadAttempted;
            private RuntimeCharacterFrameSet frameSet;

            internal FrameSetEntry(
                string rootPath,
                Vector2 artworkSize,
                Vector2 artworkOffset,
                int strongPrepareFrameCount,
                int strongAttackFrameCount,
                int deathFrameCount)
            {
                this.rootPath = rootPath;
                this.artworkSize = artworkSize;
                this.artworkOffset = artworkOffset;
                this.strongPrepareFrameCount = strongPrepareFrameCount;
                this.strongAttackFrameCount = strongAttackFrameCount;
                this.deathFrameCount = deathFrameCount;
            }

            internal RuntimeCharacterFrameSet GetOrLoad()
            {
                if (!loadAttempted)
                {
                    loadAttempted = true;
                    frameSet = LoadFrameSet(
                        rootPath,
                        artworkSize,
                        artworkOffset,
                        strongPrepareFrameCount,
                        strongAttackFrameCount,
                        deathFrameCount);
                }

                return frameSet;
            }
        }

        /// <summary>
        /// 세 상태 폴더를 한 번에 검증해 부분 프레임 세트가 화면에 섞이지 않게 합니다.
        /// </summary>
        private static RuntimeCharacterFrameSet LoadFrameSet(
            string rootPath,
            Vector2 artworkSize,
            Vector2 artworkOffset,
            int strongPrepareFrameCount,
            int strongAttackFrameCount,
            int deathFrameCount)
        {
            Sprite[] idleFrames = LoadSortedFrames(rootPath + "/idle", ExpectedIdleFrameCount);
            Sprite[] attackFrames = LoadSortedFrames(rootPath + "/attack", ExpectedAttackFrameCount);
            Sprite[] hitFrames = LoadSortedFrames(rootPath + "/hit", ExpectedHitFrameCount);
            if (idleFrames == null || attackFrames == null || hitFrames == null)
            {
                Debug.LogWarning("[FirstForm] 캐릭터 프레임 세트가 불완전해 단일 이미지로 대체합니다: Resources/" + rootPath);
                return null;
            }

            Sprite[] strongPrepareFrames = LoadOptionalSortedFrames(
                rootPath + "/strong_prepare",
                strongPrepareFrameCount);
            Sprite[] strongAttackFrames = LoadOptionalSortedFrames(
                rootPath + "/strong_attack",
                strongAttackFrameCount);
            Sprite[] deathFrames = LoadOptionalSortedFrames(
                rootPath + "/death",
                deathFrameCount);

            return new RuntimeCharacterFrameSet(
                idleFrames,
                attackFrames,
                hitFrames,
                strongPrepareFrames,
                strongAttackFrames,
                deathFrames,
                artworkSize,
                artworkOffset);
        }

        /// <summary>
        /// 선택 프레임은 누락되어도 기본 세트를 폐기하지 않고 상태별 fallback을 사용합니다.
        /// </summary>
        private static Sprite[] LoadOptionalSortedFrames(string resourcePath, int expectedCount)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            if (frames == null || frames.Length == 0)
            {
                return null;
            }

            if (frames.Length != expectedCount)
            {
                Debug.LogWarning(
                    "[FirstForm] 선택 애니메이션 프레임 수가 맞지 않아 fallback을 사용합니다: Resources/" +
                    resourcePath + " (expected " + expectedCount + ", actual " + frames.Length + ")");
                return null;
            }

            Array.Sort(frames, delegate(Sprite left, Sprite right)
            {
                return string.CompareOrdinal(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty);
            });

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null)
                {
                    return null;
                }
            }

            return frames;
        }

        /// <summary>
        /// 상태 폴더의 프레임을 이름 순으로 정렬하고 예상 개수를 확인합니다.
        /// </summary>
        private static Sprite[] LoadSortedFrames(string resourcePath, int expectedCount)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            if (frames == null || frames.Length != expectedCount)
            {
                return null;
            }

            Array.Sort(frames, delegate(Sprite left, Sprite right)
            {
                return string.CompareOrdinal(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty);
            });

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null)
                {
                    return null;
                }
            }

            return frames;
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
