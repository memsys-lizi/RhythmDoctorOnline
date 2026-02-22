using System.Text;
using UnityEngine;

namespace RDOnline.Utils
{
    public class GameUtils
    {
        public static RankScore GetCurrentScore()
        {
            /*var colors = RDConstants.data.hitMarginColoursUI;
            var rank = new RankScore
            {
                perfects = GetHits(HitMargin.Perfect),
                earlyPerfects = GetHits(HitMargin.EarlyPerfect),
                latePerfects = GetHits(HitMargin.LatePerfect),
                veryEarlies = GetHits(HitMargin.VeryEarly),
                veryLates = GetHits(HitMargin.VeryLate),
                tooEarlies = GetHits(HitMargin.TooEarly),
                misses = GetHits(HitMargin.FailMiss),
                tooLates = GetHits(HitMargin.TooLate),
                overloads = GetHits(HitMargin.FailOverload),
                autos = GetHits(HitMargin.Auto),
                accuracy = scrController.instance.mistakesManager.percentAcc * 100,
                xAccuracy = scrController.instance.mistakesManager.percentXAcc * 100,
                difficulty = scrMistakesManager.hardestDifficulty.ToString()
            };

            string space = " ";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"{Result(rank.overloads, colors.colourFail)}");
            stringBuilder.Append(space);
            stringBuilder.Append($"{Result(rank.tooEarlies, colors.colourTooEarly)}");
            stringBuilder.Append(space);
            stringBuilder.Append($"{Result(rank.veryEarlies, colors.colourVeryEarly)}");
            stringBuilder.Append(space);
            stringBuilder.Append($"{Result(rank.earlyPerfects, colors.colourLittleEarly)}");
            stringBuilder.Append(space);
            stringBuilder.Append($"{Result(rank.perfects, colors.colourPerfect, rank.autos)}");
            stringBuilder.Append(space);
            stringBuilder.Append($"{Result(rank.latePerfects, colors.colourLittleLate)}");
            stringBuilder.Append(space);
            stringBuilder.Append($"{Result(rank.veryLates, colors.colourVeryLate)}");
            stringBuilder.Append(space);
            stringBuilder.Append($"{Result(rank.tooLates, colors.colourTooLate)}");
            stringBuilder.Append(space);
            stringBuilder.Append($"{Result(rank.misses, colors.colourFail)}");
            rank.score = stringBuilder.ToString();
            return rank;

            string Result(int resultCount, Color color, int autoCount = 0)
            {
                return autoCount == 0
                    ? $"<color={color.ToHex()}>{resultCount}</color>"
                    : $"<color={color.ToHex()}>{resultCount}({autoCount})</color>";
            }

            int GetHits(HitMargin hitMargin)
            {
                return scrController.instance.mistakesManager.GetHits(hitMargin);
            }*/
            return Patches.RankScore;
        }

        public static void LoadLevel(string path)
        {
            scnBase.currentLevelSelect = "ScnRoom";
            scnGame.pauseBlocked = true;
            scnBase.GoToLevel(path);
        }
    }
#if ADOFAI
    public struct RankScore
    {
        public int perfects;
        public int earlyPerfects;
        public int latePerfects;
        public int veryEarlies;
        public int veryLates;
        public int tooEarlies;
        public int tooLates;
        public int misses;
        public int overloads;
        public int autos;
        public double accuracy;
        public double xAccuracy;
        public string difficulty; //判定
        public string score; // "1 10 0 200 0 2 1 0"
        public override string ToString()
        {
            return $"{score} {accuracy:N2}% {xAccuracy:N2}%";
        }
    }
#endif
    
#if RHYTHMDOCTOR
    public struct RankScore
    {
        public string rank;

        public int earlyOffsetsSum;

        public int lateOffsetsSum;

        public int totalOffsetsSum;

        public int earlyOffsetsSumP1;

        public int lateOffsetsSumP1;

        public int totalOffsetsSumP1;

        public int earlyOffsetsSumP2;

        public int lateOffsetsSumP2;

        public int totalOffsetsSumP2;

        public float mistakes;

        public float mistakesP1;

        public float mistakesP2;
        
        public string score => ToString();
        public override string ToString()
        {
            return $"总计: 提前{earlyOffsetsSum}帧, 滞后{lateOffsetsSum}帧, 总共{totalOffsetsSum}帧, 失误: {mistakes} 评分: {rank}";
        }
    }
#endif
}