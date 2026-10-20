using UnityEngine;

namespace PongWall
{
    public class BestScoreStore
    {
        private const string BestScoreKey = "PongWall.BestScore";

        public int BestScore
        {
            get => PlayerPrefs.GetInt(BestScoreKey, 0);
            set
            {
                if (value <= BestScore)
                    return;

                // Written on every hit during a run, so avoid forcing a disk flush
                // each time. GameOverView calls Flush() once when the run ends.
                PlayerPrefs.SetInt(BestScoreKey, value);
            }
        }

        public void Flush()
        {
            PlayerPrefs.Save();
        }
    }
}