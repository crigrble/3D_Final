using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NekoLegends
{
    public class MiniGameGUI : MonoBehaviour
    {
        #region Singleton
        public static MiniGameGUI Instance
        {
            get
            {
                if (instance == null)
                    instance = Object.FindFirstObjectByType(typeof(MiniGameGUI)) as MiniGameGUI;

                return instance;
            }
            set
            {
                instance = value;
            }
        }
        private static MiniGameGUI instance;
        #endregion


        [Space]
        [SerializeField] protected TextMeshProUGUI scoreTxt,comboTxt;
        [SerializeField] protected TextMeshProUGUI timerTxt;
        [SerializeField] protected TextMeshProUGUI rankNum;
        [SerializeField] protected TextMeshProUGUI rankTitle;


        [SerializeField] protected List<GameObject> hearts;


        [SerializeField] protected GameObject gameContainerUI, gameplayUICanvas, endGameGUI, timerGUIContainer;


        [SerializeField] protected AnimationCurve punchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] protected int countDownStartLimit = 80;

        protected int[] scoreThresholds = {};
        protected string[] rankTitles = {
        "Whisker Apprentice",
        "Paw Novice",
        "Feline Adept",
        "Shadow Striker",
        "Purr Guardian",
        "Nimble Lynx",
        "Mystic Meow",
        "Eclipse Enforcer",
        "Celestial Meowster",
        "Neko Legend"
        };


        protected int score, damageIndex, currentTimeLeft;

     
        protected bool gameStarted { get; set; }


        public virtual void Start()
        {
            HideUI();
        }

        public void HideUI()
        {
            if(timerGUIContainer)
                timerGUIContainer.SetActive(false);
            if (gameContainerUI)
                gameContainerUI.SetActive(false);
            gameplayUICanvas.SetActive(false);
            endGameGUI.SetActive(false);
        }
    
        public virtual void GameStart()
        {
            if (timerGUIContainer)
                timerGUIContainer.SetActive(true);

            if (gameContainerUI)
                gameContainerUI.SetActive(true);
            gameplayUICanvas.SetActive(true);
            endGameGUI.SetActive(false);
            score = 0; 
            scoreTxt.SetText("Score");
            comboTxt.SetText("Combo");
            gameStarted = true;
            RefillHearts();
            StopCoroutine(StartCountDown());
            StartCoroutine(StartCountDown());
            DemoScenes.Instance.SetDOFFromDataIndex(1, 2);

        }



        public IEnumerator StartCountDown()
        {
            ResetTimer();

            while (gameStarted)
            {
                yield return new WaitForSeconds(1f);
                currentTimeLeft--;
              

                timerTxt.SetText(currentTimeLeft + "");
                if (currentTimeLeft <= 0)
                {
                    ShowEndResult();
                }

            }
        }

        protected virtual void CalculateScore()
        {
            scoreTxt.SetText(score+"");
        }
      

        private void ResetTimer()
        {
            currentTimeLeft = countDownStartLimit;
            timerTxt.SetText(currentTimeLeft + "");
        }

        private void RefillHearts()
        {
            foreach (GameObject heart in hearts)
            {
                heart.transform.localScale = new Vector3(.75f, .75f, .75f);
            }
            damageIndex = 0;
        }

        public virtual void DeductHeart()
        {
            if (gameStarted)
            {
                int currentIndex = damageIndex;
                if (currentIndex < hearts.Count)
                {
                    // Start the punch scale coroutine
                    StartCoroutine(PunchScaleAndHide(hearts[currentIndex].transform, new Vector3(1f, 1f, 1f), 1f, punchCurve));
                }
                damageIndex++;
                if (damageIndex >= hearts.Count)
                {
                    ShowEndResult();
                }
            }
        }

        private void ShowEndResult()
        {
            if (gameStarted)
            {
                StartCoroutine(EndGame());
            }
            gameStarted = false;

        }

        public virtual IEnumerator EndGame()
        {
            yield return new WaitForSeconds(.5f);

            DemoScenes.Instance.SetDOFFromDataIndex(0, 1);
            UpdateRank();
            endGameGUI.SetActive(true);
            if (timerGUIContainer)
                timerGUIContainer.SetActive(false);
        }

        private IEnumerator PunchScaleAndHide(Transform target, Vector3 punch, float duration, AnimationCurve curve)
        {
            Vector3 originalScale = target.localScale;
            Vector3 targetScale = originalScale + punch;

            float halfDuration = duration / 5f;
            float elapsed = 0f;

            // Scale up with AnimationCurve
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                float curveValue = curve.Evaluate(t);
                target.localScale = Vector3.Lerp(originalScale, targetScale, curveValue);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure it reaches targetScale
            target.localScale = targetScale;

            // Reset elapsed time for scaling back
            elapsed = 0f;

            // Scale back with AnimationCurve
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                float curveValue = curve.Evaluate(t);
                target.localScale = Vector3.Lerp(targetScale, originalScale, curveValue);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure it returns to originalScale
            target.localScale = originalScale;

            // After animation completes, set scale to (0,0,0)
            target.localScale = Vector3.zero;
        }

        public int UpdateRank()
        {
            bool rankAssigned = false;
            int rankNumber = 10;

            // Check if arrays are null or have mismatched lengths
            if (rankTitles == null || scoreThresholds == null)
            {
                Debug.Log("Error: rankTitles or scoreThresholds is null.");
                return rankNumber;
            }

            if (rankTitles.Length != scoreThresholds.Length)
            {
                Debug.Log("Error: rankTitles and scoreThresholds have different lengths.");
                return rankNumber;
            }

            if (rankTitles.Length == 0)
            {
                Debug.Log("Error: rankTitles array is empty.");
                return rankNumber;
            }

            // Loop through ranks
            for (int i = rankTitles.Length - 1; i >= 0; i--)
            {
                if (score >= scoreThresholds[i])
                {
                    rankTitle.text = rankTitles[i];
                    rankNumber = rankTitles.Length - i;
                    rankNum.text = "Rank #" + rankNumber;
                    rankAssigned = true;
                    break;
                }
            }

            // If no rank was assigned, set to the lowest rank
            if (!rankAssigned)
            {
                rankTitle.text = rankTitles[0];
                rankNum.text = "Rank #" + rankTitles.Length;
            }

            return rankNumber;
        }
        
        public virtual void UpdateScore(int in_score, string fromObjectName = "") { }
        public virtual void PlayCollectSFX() { }
        public virtual void LoadMainMenu() {
            HideUI();
            DemoScenes.Instance.LoadMainMenu();
        }
    }
}
