using System.Collections.Generic;
using UnityEngine;

namespace TileGameCore
{
    [CreateAssetMenu(fileName = "Audio Clips", menuName = "Data/Core/Audio Clips")]
    public class AudioClips : ScriptableObject
    {
        [BoxGroup("UI", "UI")]
        public AudioClip buttonSound;
        [BoxGroup("UI", "UI")]
        public AudioClip buttonSoundSpread;

        [BoxGroup("UI")]
        public AudioClip levelComplete;
        [BoxGroup("UI")]
        public AudioClip levelFailed;
        [BoxGroup("UI")]
        public AudioClip bgChange;
        [BoxGroup("UI")]
        public AudioClip bgChange2;

        [BoxGroup("Gameplay", "Gameplay")]
        public AudioClip tileClick;
        [BoxGroup("Gameplay")]
        public AudioClip tileClickBlocked;
        [BoxGroup("Gameplay")]
        public AudioClip mergeSound;
        [BoxGroup("Gameplay")]
        public AudioClip fiveCombo;
        [BoxGroup("Gameplay")]
        public AudioClip autocomplete;
        [BoxGroup("Gameplay")]
        public AudioClip challengeOpen;
        [BoxGroup("Gameplay")]
        public AudioClip challengeOver;
        [BoxGroup("GP")]
        public AudioClip pockerSound;

        [BoxGroup("PowerUp")]
        public AudioClip tool_magic;
        [BoxGroup("PowerUp")]
        public AudioClip tool_random;
        [BoxGroup("PowerUp")]
        public AudioClip tool_return;
        [BoxGroup("PowerUp")]
        public AudioClip tool_highlight;
        [BoxGroup("PowerUp")]
        public AudioClip tool_add;

        [BoxGroup("Effects", "Effects")]
        public AudioClip iceCrackSound;
        [BoxGroup("Effects")]
        public AudioClip crateCrackSound;
        [BoxGroup("Effects")]
        public AudioClip rubberSound; //链接的用
        [BoxGroup("Effects")]
        public AudioClip unknowOpenSound;

        [BoxGroup("BoxCover")]
        public AudioClip boxCoverClick;
        [BoxGroup("BoxCover")]
        public AudioClip boxCoverOpen;
        [BoxGroup("BoxCover")]
        public AudioClip boxCoverCollect;

        [BoxGroup("ColorEgg")]
        public AudioClip colorEggShake;
        [BoxGroup("ColorEgg")]
        public AudioClip colorEggBroken;

        public AudioClip WoodBroken;
        public AudioClip WoodCollect;
        public AudioClip WoodClickBlocked;
        public AudioClip goldenTileCollect;

        [BoxGroup("CV")]
        public AudioClip CVAmazing;
        [BoxGroup("CV")]
        public AudioClip CVAwesome;
        [BoxGroup("CV")]
        public AudioClip CVExcellent;
        [BoxGroup("CV")]
        public AudioClip CVGreate;
        public AudioClip CVUnbelivable;

        [BoxGroup("CV")]
        public AudioClip CVPerfect;

        [BoxGroup("CV")]
        public AudioClip CVPerfectCollect;

        public AudioClip perfectCollectPanel;

        [BoxGroup("Coin")]
        public AudioClip coinAdd;
        [BoxGroup("Coin")]
        public AudioClip coinlevel_into;
        [BoxGroup("Coin")]
        public AudioClip coinlevel_rewards;

        [BoxGroup("Reward")]
        public AudioClip rewardGet;

        [BoxGroup("DailyWin")]
        public AudioClip dailyStreakFly;
        public AudioClip dailyStreakReward;
        public AudioClip dailyStreakNum;

        [BoxGroup("CircleDaily")]
        public AudioClip circleRotation;
        [BoxGroup("CircleDaily")]
        public AudioClip spin_bg;
        [BoxGroup("CircleDaily")]
        public AudioClip spin_double;

        [BoxGroup("RankUp")]
        public AudioClip rankUp;
        [BoxGroup("RankUp")]
        public AudioClip rank_promotion;
        [BoxGroup("RankUp")]
        public AudioClip rank_demotion;
        [BoxGroup("RankUp")]
        public AudioClip rank_maintain;
        [BoxGroup("RankUp")]
        public AudioClip rank_reward;
        public AudioClip winning_1;

        public AudioClip allClear;

        [BoxGroup("PuppyAdventure", "PuppyAdventure")]
        public AudioClip jungle_fail;
        [BoxGroup("PuppyAdventure")]
        public AudioClip jungle_down;
        [BoxGroup("PuppyAdventure")]
        public AudioClip jungle_forward;
        [BoxGroup("PuppyAdventure")]
        public AudioClip jungle_match;
        [BoxGroup("PuppyAdventure")]
        public AudioClip jungle_reward;

        [BoxGroup("PiggySmash")]
        public AudioClip piggySmashClick;
        [BoxGroup("PiggySmash")]
        public AudioClip piggySmashBroken;
        [BoxGroup("PiggySmash")]
        public AudioClip piggySmashOpen;
        [BoxGroup("PiggySmash")]
        public AudioClip piggySmashBack;

        [BoxGroup("ColorChallenge")]
        public AudioClip colorChallenge;

        [BoxGroup("Gameplay")]
        public AudioClip iqConvertElectric;

        [BoxGroup("Gameplay")]
        public List<AudioClip> tileBaseSounds = new();

    }
}

// -----------------
// Audio Controller v 0.4
// -----------------