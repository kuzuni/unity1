using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Forge.Core.Data;
using Forge.Core.Save;

namespace Forge.Tests
{
    /// <summary>
    /// T13 — 세이브·오프라인(원작 state.js). 벡터는 정본 state.js 를 node vm 에 올려 뽑았다(생성 방법은 PROGRESS T13 완료 기록):
    /// `loadGame()` 을 손상 세이브 14종에 돌린 결과 JSON · `offlineRewardFor`/`pendingOffline`/`claimOfflineNow` · `stageName`/`stageKey`/`isUnlocked`.
    /// «같은 입력 → 같은 글자» 를 MiniJson.Serialize(= JSON.stringify) 로 통째로 견준다.
    /// </summary>
    static class SaveVectors
    {
        // ── 정본 벡터: state.js 를 node vm 에 올려(window=자기 자신 · U.now=NOW · localStorage 스텁 · Mounts.migrateInventory 없음 · TechTree.pct 스텁) 돌린 결과 ──
        public const double NOW = 1700000000000;
        /// <summary>loadGame 벡터: 이름 · 저장소 원문 · 반환값 · JSON.stringify(S) · console.error 줄 수.</summary>
        public static readonly object[][] Load = {
            new object[] { "array", @"[1,2]", false, @"{""version"":1,""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""activeMounts"":[],""quests"":[],""questsCleared"":0}", 0 },
            new object[] { "number", @"42", false, @"{""version"":1,""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""activeMounts"":[],""quests"":[],""questsCleared"":0}", 0 },
            new object[] { "null", @"null", false, @"{""version"":1,""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""activeMounts"":[],""quests"":[],""questsCleared"":0}", 0 },
            new object[] { "empty_obj", @"{}", true, @"{""version"":1,""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""activeMounts"":[],""quests"":[],""questsCleared"":0}", 0 },
            new object[] { "version0", @"{""version"":0,""coins"":9999}", true, @"{""version"":1,""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""activeMounts"":[],""quests"":[],""questsCleared"":0}", 0 },
            new object[] { "garbage", @"{not json", false, @"{""version"":1,""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""activeMounts"":[],""quests"":[],""questsCleared"":0}", 0 },
            new object[] { "corrupt1", @"{""version"":1,""coins"":""abc"",""hammers"":-5,""gems"":null,""chapter"":0,""stage"":99999,""difficulty"":9,""bestDifficulty"":-1,""bestChapter"":0,""bestStage"":0,""forgeLevel"":99,""equippedSkills"":[""ghost"",""powerStrike"",7,""powerStrike"",""fireball""],""pets"":[1,{""name"":""x""},null,[1]],""activePets"":[0,0,5,1,-1,1.5],""eggs"":[{""rarity"":""rare""},""bad""],""hatching"":[{""rarity"":""epic"",""endsAt"":1}],""equipment"":{""weapon"":""str"",""helmet"":{""age"":""nope"",""ageIdx"":2,""subs"":""x""},""armor"":[1],""ring"":{""age"":""stone"",""subs"":[]}},""summonMult"":{""skill"":5,""pet"":null},""autoForge"":{""keepAges"":""x"",""hammersPerBatch"":3},""activeMount"":""Brown Horse"",""mounts"":[],""inventory"":[1],""heldCrafts"":[{""a"":1}],""lastOfflineClaim"":0,""lastSeen"":1699999000000,""techResearch"":null}", true, @"{""version"":1,""coins"":500,""hammers"":0,""gems"":0,""chapter"":1,""stage"":10,""difficulty"":3,""bestDifficulty"":3,""bestChapter"":1,""bestStage"":10,""forgeLevel"":35,""equippedSkills"":[""powerStrike"",""powerStrike"",""fireball""],""pets"":[{""name"":""x""}],""activePets"":[0],""eggs"":[{""rarity"":""rare""}],""hatching"":[{""rarity"":""epic"",""endsAt"":1}],""equipment"":{""weapon"":null,""helmet"":{""age"":""earlyModern"",""ageIdx"":2,""subs"":[]},""armor"":null,""ring"":{""age"":""primitive"",""subs"":[],""ageIdx"":0},""gloves"":null,""necklace"":null,""shoes"":null,""belt"":null},""summonMult"":{""skill"":5,""pet"":1,""mount"":1},""autoForge"":{""keepAges"":[],""hammersPerBatch"":3,""filterOn"":false,""filterSubs"":[],""stopOnTarget"":false},""mounts"":[],""lastOfflineClaim"":1699999000000,""lastSeen"":1699999000000,""techResearch"":null,""activeMounts"":[],""createdAt"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""dungeonRun"":null,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""hatchSlotBonus"":0,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":{""a"":1},""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""quests"":[],""questsCleared"":0}", 3 },
            new object[] { "best_behind", @"{""version"":1,""chapter"":3,""stage"":4,""difficulty"":1,""bestChapter"":5,""bestStage"":9,""bestDifficulty"":0,""lastOfflineClaim"":5}", true, @"{""version"":1,""chapter"":3,""stage"":4,""difficulty"":1,""bestChapter"":3,""bestStage"":4,""bestDifficulty"":1,""lastOfflineClaim"":5,""activeMounts"":[],""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""dungeonRun"":null,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""quests"":[],""questsCleared"":0}", 0 },
            new object[] { "activeMounts_str", @"{""version"":1,""mounts"":[{""name"":""Brown Horse"",""rarity"":""common"",""level"":1,""xp"":0,""stars"":0,""subs"":[]}],""activeMounts"":[""Brown Horse"",0,0,2],""activePets"":[],""pets"":[]}", true, @"{""version"":1,""mounts"":[{""name"":""Brown Horse"",""rarity"":""common"",""level"":1,""xp"":0,""stars"":0,""subs"":[]}],""activeMounts"":[0],""activePets"":[],""pets"":[],""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""quests"":[],""questsCleared"":0}", 0 },
            new object[] { "held_no_pending", @"{""version"":1,""pendingCraft"":null,""heldCrafts"":[{""slot"":""weapon"",""age"":""stone""},{""slot"":""ring""}]}", true, @"{""version"":1,""pendingCraft"":{""slot"":""weapon"",""age"":""stone""},""activeMounts"":[],""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""quests"":[],""questsCleared"":0}", 0 },
            new object[] { "held_with_pending", @"{""version"":1,""pendingCraft"":{""slot"":""belt""},""heldCrafts"":[{""slot"":""weapon""}]}", true, @"{""version"":1,""pendingCraft"":{""slot"":""belt""},""activeMounts"":[],""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""quests"":[],""questsCleared"":0}", 0 },
            new object[] { "equip_ages", @"{""version"":1,""equipment"":{""weapon"":{""age"":""nope"",""ageIdx"":99,""subs"":[]},""helmet"":{""age"":""nope"",""ageIdx"":1,""subs"":[1]},""armor"":{""age"":""bronze"",""subs"":[]}}}", true, @"{""version"":1,""equipment"":{""weapon"":{""age"":""primitive"",""ageIdx"":0,""subs"":[]},""helmet"":{""age"":""medieval"",""ageIdx"":1,""subs"":[1]},""armor"":{""age"":""primitive"",""subs"":[],""ageIdx"":0},""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""activeMounts"":[],""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""quests"":[],""questsCleared"":0}", 3 },
            new object[] { "lastOfflineClaim_missing", @"{""version"":1,""lastSeen"":123456}", true, @"{""version"":1,""lastSeen"":123456,""activeMounts"":[],""createdAt"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""quests"":[],""questsCleared"":0}", 0 },
            new object[] { "lastOfflineClaim_and_lastSeen_missing", @"{""version"":1}", true, @"{""version"":1,""activeMounts"":[],""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""quests"":[],""questsCleared"":0}", 0 },
        };
        /// <summary>새 게임 → saveGame() 저장 원문(시각 = NOW).</summary>
        public static readonly string DefaultSave = @"{""version"":1,""createdAt"":1700000000000,""lastSeen"":1700000000000,""nickname"":""용사"",""avatarEmoji"":""🛡️"",""gender"":""♂"",""musicOn"":true,""settingsDummy"":{""vibration"":true,""chatShow"":true,""chatDark"":false,""clanChatPreview"":true},""lastOfflineClaim"":1700000000000,""chapter"":1,""stage"":1,""dungeonRun"":null,""difficulty"":0,""bestChapter"":1,""bestStage"":1,""bestDifficulty"":0,""kills"":0,""totalCrafts"":0,""clearedBosses"":{},""hammers"":80,""coins"":500,""gems"":0,""tickets"":40,""winders"":0,""potions"":0,""eggCurrency"":0,""petSummonCount"":0,""summonMult"":{""skill"":1,""pet"":1,""mount"":1},""hatchSlotBonus"":0,""techResearch"":null,""lineAscend"":{""forge"":0,""skill"":0,""pet"":0,""mount"":0},""forgeLevel"":1,""rollLevel"":{},""forgeUpgradeEndsAt"":null,""pendingCraft"":null,""autoMatchQueue"":[],""autoBatch"":null,""autoMatchHeld"":false,""autoForgeOn"":false,""autoForge"":{""keepAges"":[],""filterOn"":false,""filterSubs"":[],""hammersPerBatch"":10,""stopOnTarget"":false},""equipment"":{""weapon"":null,""helmet"":null,""armor"":null,""gloves"":null,""necklace"":null,""ring"":null,""shoes"":null,""belt"":null},""eggs"":[{""rarity"":""common""}],""hatching"":[],""pets"":[],""activePets"":[],""skills"":{""powerStrike"":{""level"":1,""dupes"":0,""stars"":0}},""equippedSkills"":[""powerStrike""],""autoCast"":true,""sfxOn"":true,""summonCount"":0,""mountOpens"":0,""mounts"":[],""activeMounts"":[],""quests"":[],""questsCleared"":0}";
        /// <summary>offlineRewardFor 벡터: elapsed · 배율(cap·coin·hammer) · counted · coins · hammers · coinRate · hammerRate.</summary>
        public static readonly double[][] Offline = {
            new[] { 100.0, 1.0, 1.0, 1.0, 100.0, 100.0, 1.0, 1.0, 1.0 },
            new[] { 5000.0, 1.0, 1.0, 1.0, 5000.0, 5000.0, 83.0, 1.0, 1.0 },
            new[] { 20000.0, 1.0, 1.0, 1.0, 14400.0, 14400.0, 240.0, 1.0, 1.0 },
            new[] { 20000.0, 1.5, 1.32, 1.1, 20000.0, 26400.0, 366.0, 1.32, 1.1 },
            new[] { 59.0, 1.0, 1.0, 1.0, 59.0, 59.0, 0.0, 1.0, 1.0 },
            new[] { 18000.0, 1.0, 1.0, 1.0, 14400.0, 14400.0, 240.0, 1.0, 1.0 },
            new[] { 14400.0, 1.0, 1.0, 1.0, 14400.0, 14400.0, 240.0, 1.0, 1.0 },
            new[] { 0.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0 },
            new[] { 7.0, 1.0, 0.5, 1.0, 7.0, 3.0, 0.0, 0.5, 1.0 },
        };
        /// <summary>pendingOffline/claimOfflineNow 벡터: agoMs · coinMult · pending(null=없음: elapsed·coins·hammers) · claimed 여부 · after coins·hammers·lastOfflineClaim.</summary>
        public static readonly object[][] Claim = {
            new object[] { 1234567.0, 1.0, 1234.0, 1234.0, 20.0, true, 1734.0, 100.0, 1700000000000.0 },
            new object[] { 500.0, 1.0, null, null, null, false, 500.0, 80.0, 1699999999500.0 },
            new object[] { 30000.0, 0.0, 30.0, 0.0, 0.0, false, 500.0, 80.0, 1699999970000.0 },
            new object[] { 18000000.0, 1.0, 18000.0, 14400.0, 240.0, true, 14900.0, 320.0, 1700000000000.0 },
        };
        /// <summary>진행 좌표 벡터: tier · chapter · stage · stageName · stageKey · label · absChapter · progressRank · «key=bool,…» 해금 · 모르는 키.</summary>
        public static readonly object[][] Stage = {
            new object[] { 0, 1, 1, "쉬움 1-1", "1-1", "쉬움", 1, 101, "autoForge=false", true },
            new object[] { 0, 2, 3, "보통 2-3", "2-3", "보통", 2, 203, "autoForge=false", true },
            new object[] { 0, 8, 10, "어려움 8-10", "8-10", "어려움", 8, 810, "autoForge=true", true },
            new object[] { 0, 9, 1, "매우 어려움 9-1", "9-1", "매우 어려움", 9, 901, "autoForge=true", true },
            new object[] { 0, 25, 10, "매우 어려움 25-10", "25-10", "매우 어려움", 25, 2510, "autoForge=true", true },
            new object[] { 1, 1, 1, "어려움 1-1", "d1:1-1", "어려움", 26, 2601, "autoForge=true", true },
            new object[] { 2, 13, 5, "매우 어려움 13-5", "d2:13-5", "매우 어려움", 63, 6305, "autoForge=true", true },
            new object[] { 3, 25, 10, "헬 25-10", "d3:25-10", "헬", 100, 10010, "autoForge=true", true },
            new object[] { 5, 3, 3, "헬 3-3", "d5:3-3", "헬", 78, 7803, "autoForge=true", true },
        };
    }

    static class SaveFixture
    {
        static SaveDefs _defs;
        public static SaveDefs Defs { get { return _defs ?? (_defs = SaveDefs.Parse(File.ReadAllText(Path.Combine(DataDir.Path, SaveDefs.FileName)))); } }
        public static GameDefs Game { get { return DataDir.Game.Defs; } }
        public static LoadResult Load(string raw) { return SaveCodec.Load(raw, Defs, Game, SaveVectors.NOW); }
    }

    /// <summary>T13 — state.json(SaveDefs): 정본 상수 · 기본 상태.</summary>
    public class SaveDefsTests
    {
        [Test]
        public void 정본_상수가_표에서_온다()
        {
            var d = SaveFixture.Defs;
            Assert.AreEqual("forgeclone_save_v1", d.SaveKey);
            Assert.AreEqual(4 * 3600, d.OfflineCapSec, "오프라인 캡 4시간(ROUTINE T13)");
            Assert.AreEqual(1, d.OfflineCoinPerSec, "코인 1/초");
            Assert.AreEqual(1, d.OfflineHammerPerMin, "해머 1/분");
            Assert.AreEqual(SaveFixture.Game.ChapterThemes.Count, d.ChaptersPerCycle, "사이클 길이 = 맵 종류 수(CHAPTER_THEMES)");
            Assert.AreEqual(d.MaxDifficulty + 1, d.DifficultyNames.Length);
            Assert.AreEqual("", d.DifficultyNames[0]);
            Assert.AreEqual(10, d.StagesPerChapter);
            Assert.AreEqual(DataDir.Game.Balance.Forge.MaxLevel, d.ForgeMaxLevel, "Forge.MAX_LEVEL = 확률표 행 수");
            Assert.AreEqual(3, d.PetMaxActive, "출전은 3마리까지");
            Assert.AreEqual(3, d.SkillMaxActive);
            Assert.AreEqual(1, d.MountMaxActive, "탈것 장착 1");
            Assert.AreEqual(new[] { "summonMult", "autoForge", "lineAscend", "settingsDummy", "equipment" }, d.ShapeKeys);
            Assert.AreEqual(new[] { "version", "chapter", "stage", "bestChapter", "bestStage", "forgeLevel" }, d.MinOneKeys);
            foreach (string k in d.ShapeKeys) Assert.IsInstanceOf<JsonObject>(d.DefaultTemplate[k], k);
            foreach (string k in d.MinOneKeys) Assert.IsInstanceOf<double>(d.DefaultTemplate[k], k);
        }

        [Test]
        public void 기본_상태는_시각만_채운_깊은_복제()
        {
            var d = SaveFixture.Defs;
            var s = d.DefaultState(SaveVectors.NOW);
            Assert.AreEqual(55, s.Count);
            Assert.AreEqual(1.0, s["version"]);
            Assert.AreEqual(SaveVectors.NOW, s["createdAt"]);
            Assert.AreEqual(SaveVectors.NOW, s["lastSeen"]);
            Assert.AreEqual(SaveVectors.NOW, s["lastOfflineClaim"]);
            Assert.AreEqual(0.0, d.DefaultTemplate["createdAt"], "원본 템플릿은 그대로 0");
            Assert.AreEqual(80.0, s["hammers"]);
            Assert.AreEqual(500.0, s["coins"]);
            Assert.AreEqual(40.0, s["tickets"]);
            Assert.AreEqual(1, J.Arr(s["eggs"]).Count, "시작 알 1개");
            Assert.AreEqual("common", J.Str(J.Obj(J.Arr(s["eggs"])[0])["rarity"]));
            Assert.AreEqual("powerStrike", J.Str(J.Arr(s["equippedSkills"])[0]), "시작 스킬 강타");
            Assert.IsFalse(s.Has("activeMount"), "구 필드 접근자는 세이브에 안 실린다");
            // 깊은 복제 — 한쪽을 고쳐도 다른 쪽·템플릿이 안 변한다
            var t = d.DefaultState(SaveVectors.NOW);
            J.Obj(s["summonMult"])["skill"] = 5.0;
            J.Arr(s["eggs"]).Clear();
            Assert.AreEqual(1.0, J.Obj(t["summonMult"])["skill"]);
            Assert.AreEqual(1, J.Arr(t["eggs"]).Count);
            Assert.AreEqual(1, J.Arr(d.DefaultTemplate["eggs"]).Count);
        }

        [Test]
        public void 새_게임_저장_원문은_원작과_같은_글자()
        {
            var s = new SaveState(SaveFixture.Defs.DefaultState(SaveVectors.NOW));
            Assert.AreEqual(SaveVectors.DefaultSave, SaveCodec.Serialize(s, SaveVectors.NOW));
        }
    }

    /// <summary>T13 — SaveCodec: 원작 loadGame/saveGame 과 벡터 대조 · 왕복 · 마이그레이션 훅.</summary>
    public class SaveCodecTests
    {
        [Test]
        public void 왕복_저장은_손실이_없고_lastSeen_을_갱신한다()
        {
            var d = SaveFixture.Defs;
            var s = new SaveState(d.DefaultState(SaveVectors.NOW));
            s.Coins = 1234; s.Chapter = 3; s.Stage = 7; s.BestChapter = 3; s.BestStage = 7; s.Nickname = "대장장이";
            s.ForgeUpgradeEndsAt = SaveVectors.NOW + 90000;
            J.Obj(s["clearedBosses"])["1-5"] = true;
            string text = SaveCodec.Serialize(s, SaveVectors.NOW + 5000);
            Assert.AreEqual(SaveVectors.NOW + 5000, s.LastSeen);
            var r = SaveFixture.Load(text);
            Assert.IsTrue(r.Loaded);
            Assert.AreEqual(0, r.Fixed, "온전한 세이브는 아무것도 안 메꾼다");
            Assert.AreEqual(0, r.Warnings.Count);
            Assert.AreEqual(text, MiniJson.Serialize(r.State.Root));
            Assert.AreEqual(1234.0, r.State.Coins);
            Assert.AreEqual("어려움 3-7", r.State.StageName(d));
            Assert.AreEqual(SaveVectors.NOW + 90000, r.State.ForgeUpgradeEndsAt);
        }

        [Test]
        public void 없는_세이브는_새_게임()
        {
            foreach (string raw in new[] { null, "" })
            {
                var r = SaveFixture.Load(raw);
                Assert.IsFalse(r.Loaded);
                Assert.AreEqual(SaveVectors.DefaultSave, MiniJson.Serialize(r.State.Root));
            }
        }

        [Test]
        public void 손상_세이브_14종이_원작_loadGame_과_같은_글자()
        {
            foreach (object[] v in SaveVectors.Load)
            {
                string name = (string)v[0];
                var r = SaveFixture.Load((string)v[1]);
                Assert.AreEqual((bool)v[2], r.Loaded, name + ": loaded");
                Assert.AreEqual((string)v[3], MiniJson.Serialize(r.State.Root), name + ": JSON.stringify(S)");
                Assert.AreEqual((int)v[4], r.Warnings.Count, name + ": console.error 줄 수");
                foreach (string w in r.Warnings) Assert.IsTrue(w.StartsWith("[state] "), w);
            }
        }

        [Test]
        public void 손상_세이브_보정_내용()
        {
            var d = SaveFixture.Defs;
            var r = SaveFixture.Load((string)SaveVectors.Load[6][1]);   // corrupt1
            var s = r.State;
            Assert.AreEqual(500.0, s.Coins, "문자열 코인 → 기본값");
            Assert.AreEqual(0.0, s.Hammers, "음수 재화 → 0 하한");
            Assert.AreEqual(0.0, s.Gems, "1e400(Infinity) → 기본값");
            Assert.AreEqual(1, s.Chapter, "챕터 0 → 1 하한");
            Assert.AreEqual(d.StagesPerChapter, s.Stage, "스테이지 99999 → 상한");
            Assert.AreEqual(d.MaxDifficulty, s.Difficulty, "티어 9 → 상한");
            Assert.AreEqual(d.ForgeMaxLevel, s.ForgeLevel, "대장간 99 → 만렙");
            Assert.AreEqual(new object[] { "powerStrike", "powerStrike", "fireball" }, s.EquippedSkills.ToArray(), "없는 스킬·수는 빠지고 상한 3");
            Assert.AreEqual(1, s.Pets.Count, "객체가 아닌 펫은 버린다");
            Assert.AreEqual(new object[] { 0.0 }, s.ActivePets.ToArray(), "중복·범위 밖·소수 인덱스 제거");
            Assert.AreEqual(0, s.ActiveMounts.Count, "구 activeMount 문자열은 인덱스가 아니라 버려진다(이관 훅 없음)");
            Assert.IsFalse(s.Has("activeMount"));
            Assert.IsFalse(s.Has("inventory"));
            Assert.IsFalse(s.Has("heldCrafts"));
            Assert.AreEqual(1.0, J.Obj(s["pendingCraft"])["a"], "heldCrafts[0] → pendingCraft");
            Assert.IsNull(s.Equipment["weapon"], "문자열 장비 → null");
            Assert.IsNull(s.Equipment["armor"], "배열 장비 → null");
            var helmet = J.Obj(s.Equipment["helmet"]);
            Assert.AreEqual(0, J.Arr(helmet["subs"]).Count, "subs 배열화");
            Assert.AreEqual(SaveFixture.Game.Ages[2], J.Str(helmet["age"]), "없는 시대 키 → ageIdx 로 되살림");
            var ring = J.Obj(s.Equipment["ring"]);
            Assert.AreEqual(SaveFixture.Game.Ages[0], J.Str(ring["age"]), "ageIdx 도 없으면 첫 시대");
            Assert.AreEqual(0.0, ring["ageIdx"]);
            Assert.AreEqual(5.0, J.Obj(s["summonMult"])["skill"], "고정 형태 레코드의 성한 칸은 남긴다");
            Assert.AreEqual(1.0, J.Obj(s["summonMult"])["pet"], "null 칸은 기본값");
            Assert.AreEqual(1.0, J.Obj(s["summonMult"])["mount"], "빠진 칸은 기본값");
            Assert.AreEqual(1699999000000.0, s.LastOfflineClaim, "lastOfflineClaim 0(거짓) → lastSeen");
            Assert.AreEqual(3, r.Warnings.Count);
            Assert.Greater(r.Fixed, 10);
        }

        [Test]
        public void 최고_기록이_현재보다_뒤지면_끌어올린다()
        {
            var d = SaveFixture.Defs;
            var s = SaveFixture.Load((string)SaveVectors.Load[7][1]).State;   // best_behind
            Assert.AreEqual(s.CurRank(d), s.BestRank(d));
            Assert.AreEqual(1, s.BestDifficulty);
            Assert.AreEqual(3, s.BestChapter);
            Assert.AreEqual(4, s.BestStage);
        }

        [Test]
        public void 탈것_이관_훅이_이름을_인덱스로_옮긴다()
        {
            // T11 이 붙일 Mounts.migrateInventory 자리 — 훅이 activeMounts 의 이름을 인덱스로 옮기면 prune 이 그것을 살린다.
            string raw = "{\"version\":1,\"activeMount\":\"Brown Horse\",\"mounts\":[{\"name\":\"Gray Wolf\"},{\"name\":\"Brown Horse\"}]}";
            int calls = 0;
            var r = SaveCodec.Load(raw, SaveFixture.Defs, SaveFixture.Game, SaveVectors.NOW, root =>
            {
                calls++;
                var mounts = J.Arr(root["mounts"]);
                var idx = new List<object>();
                foreach (object n in J.Arr(root["activeMounts"]))
                    for (int i = 0; i < mounts.Count; i++) if (J.Str(J.Obj(mounts[i])["name"]) == (string)n) idx.Add((double)i);
                root["activeMounts"] = idx;
            });
            Assert.AreEqual(1, calls);
            Assert.AreEqual(new object[] { 1.0 }, r.State.ActiveMounts.ToArray());
            Assert.IsFalse(r.State.Has("activeMount"));
            // 훅 없이 같은 세이브 → 이름은 버려진다(원작 «여기까지 문자열이 흘러오면 그냥 버린다»)
            Assert.AreEqual(0, SaveFixture.Load(raw).State.ActiveMounts.Count);
        }
    }

    /// <summary>T13 — 오프라인 보상: 수급률 · 4시간 캡 · 정수 내림 · [수집] 만 지급.</summary>
    public class OfflineTests
    {
        [Test]
        public void 수급률_벡터가_원작_offlineRewardFor_과_같다()
        {
            var d = SaveFixture.Defs;
            foreach (double[] v in SaveVectors.Offline)
            {
                var r = Offline.RewardFor(d, v[0], new OfflineMults(v[1], v[2], v[3]));
                string tag = "elapsed " + v[0] + " ×(" + v[1] + "," + v[2] + "," + v[3] + ")";
                Assert.AreEqual(v[4], r.Counted, tag + " counted");
                Assert.AreEqual(v[5], r.Coins, tag + " coins");
                Assert.AreEqual(v[6], r.Hammers, tag + " hammers");
                Assert.AreEqual(v[7], r.CoinRate, tag + " coinRate");
                Assert.AreEqual(v[8], r.HammerRate, tag + " hammerRate");
            }
        }

        [Test]
        public void 오프라인_4시간_캡()
        {
            var d = SaveFixture.Defs;
            var r = Offline.RewardFor(d, 5 * 3600, OfflineMults.One);
            Assert.AreEqual(4 * 3600, r.Counted);
            Assert.AreEqual(4 * 3600, r.Coins);
            Assert.AreEqual(240, r.Hammers);
            var r2 = Offline.RewardFor(d, 5 * 3600, new OfflineMults(1.5, 1, 1));
            Assert.AreEqual(5 * 3600, r2.Counted, "기술트리 캡 배율은 캡을 늘린다");
        }

        [Test]
        public void 미리보기는_상태를_안_건드리고_수집만_지급한다()
        {
            var d = SaveFixture.Defs;
            foreach (object[] v in SaveVectors.Claim)
            {
                var s = new SaveState(d.DefaultState(SaveVectors.NOW));
                s.LastOfflineClaim = SaveVectors.NOW - (double)v[0];
                var m = new OfflineMults(1, (double)v[1], 1);
                string tag = "ago " + v[0] + "ms ×coin " + v[1];
                var p = Offline.Pending(d, s, SaveVectors.NOW, m);
                if (v[2] == null) Assert.IsNull(p, tag + " pending");
                else
                {
                    Assert.IsNotNull(p, tag + " pending");
                    Assert.AreEqual((double)v[2], p.Elapsed, tag + " elapsed");
                    Assert.AreEqual((double)v[3], p.Coins, tag + " coins");
                    Assert.AreEqual((double)v[4], p.Hammers, tag + " hammers");
                }
                Assert.AreEqual(500.0, s.Coins, tag + " 미리보기는 지급하지 않는다");
                Assert.AreEqual(SaveVectors.NOW - (double)v[0], s.LastOfflineClaim, tag + " 미리보기는 기준 시각을 안 바꾼다");
                var c = Offline.ClaimNow(d, s, SaveVectors.NOW, m);
                Assert.AreEqual((bool)v[5], c != null, tag + " claimed");
                Assert.AreEqual((double)v[6], s.Coins, tag + " after coins");
                Assert.AreEqual((double)v[7], s.Hammers, tag + " after hammers");
                Assert.AreEqual((double)v[8], s.LastOfflineClaim, tag + " after lastOfflineClaim (지급이 0 이면 리셋하지 않는다)");
            }
        }
    }

    /// <summary>T13 — 절대시각 타이머: 세이브를 껐다 켜도 벽시계로 이어진다.</summary>
    public class AbsTimerTests
    {
        [Test]
        public void Date_now_과_같은_ms()
        {
            Assert.AreEqual(0, AbsTimer.NowMs(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual(1000, AbsTimer.NowMs(new DateTime(1970, 1, 1, 0, 0, 1, DateTimeKind.Utc)));
            Assert.AreEqual(1700000000000, AbsTimer.NowMs(new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc)));
            Assert.AreEqual(Math.Floor(AbsTimer.NowMs(DateTime.UtcNow)), AbsTimer.NowMs(DateTime.UtcNow.ToLocalTime()), 1000, "지역시각을 줘도 UTC 로 잰다");
        }

        [Test]
        public void 타이머는_세이브를_지나_절대시각으로_이어진다()
        {
            var d = SaveFixture.Defs;
            double now = SaveVectors.NOW;
            var s = new SaveState(d.DefaultState(now));
            s.ForgeUpgradeEndsAt = AbsTimer.EndsAt(now, 3600);
            Assert.AreEqual(now + 3600000, s.ForgeUpgradeEndsAt);
            string text = SaveCodec.Serialize(s, now + 1000);
            // «앱을 껐다» — 1000초 뒤에 다시 연다
            var back = SaveFixture.Load(text).State;
            double later = now + 1000 * 1000;
            Assert.AreEqual(2600, AbsTimer.RemainingSec(back.ForgeUpgradeEndsAt.Value, later));
            Assert.IsFalse(AbsTimer.IsDone(back.ForgeUpgradeEndsAt, later));
            Assert.IsTrue(AbsTimer.IsDone(back.ForgeUpgradeEndsAt, now + 3600 * 1000));
            Assert.AreEqual(0, AbsTimer.RemainingSec(back.ForgeUpgradeEndsAt.Value, now + 9999 * 1000), "남은 시간 0 하한");
            back.ForgeUpgradeEndsAt = null;
            Assert.IsFalse(AbsTimer.IsDone(back.ForgeUpgradeEndsAt, later), "null = 미진행");
            Assert.AreEqual("null", MiniJson.Serialize(back.Root["forgeUpgradeEndsAt"]));
        }
    }

    /// <summary>T13 — state.js 의 진행 좌표 함수(stageName · stageKey · 난이도 라벨 · 절대 챕터 · 랭크 · 해금).</summary>
    public class StageHelperTests
    {
        [Test]
        public void 진행_좌표_벡터가_원작과_같다()
        {
            var d = SaveFixture.Defs;
            var g = SaveFixture.Game;
            foreach (object[] v in SaveVectors.Stage)
            {
                var s = new SaveState(d.DefaultState(SaveVectors.NOW));
                s.Difficulty = (int)v[0]; s.Chapter = (int)v[1]; s.Stage = (int)v[2];
                s.BestDifficulty = (int)v[0]; s.BestChapter = (int)v[1]; s.BestStage = (int)v[2];
                string tag = v[0] + "/" + v[1] + "-" + v[2];
                Assert.AreEqual((string)v[3], s.StageName(d), tag + " stageName");
                Assert.AreEqual((string)v[4], s.StageKey(), tag + " stageKey");
                Assert.AreEqual((string)v[5], d.StageDifficultyLabel((int)v[1], (int)v[0]), tag + " label");
                Assert.AreEqual((int)v[6], d.AbsChapter((int)v[0], (int)v[1]), tag + " absChapter");
                Assert.AreEqual((int)v[7], s.CurRank(d), tag + " rank");
                foreach (string pair in ((string)v[8]).Split(','))
                {
                    string[] kv = pair.Split('=');
                    Assert.AreEqual(kv[1] == "true", s.IsUnlocked(d, g, kv[0]), tag + " unlock " + kv[0]);
                }
                Assert.AreEqual((bool)v[9], s.IsUnlocked(d, g, "zzz"), tag + " 모르는 키는 열린 것");
            }
        }
    }
}
