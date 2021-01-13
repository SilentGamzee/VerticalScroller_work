using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Objects
{
    [ExecuteInEditMode]
    public class ProgressBarUI : MonoBehaviour
    {
        //Editor
        [SerializeField] private RectTransform progressRect;
        [SerializeField] private Text progressText;

        //Private
        [SerializeField] private float _barHeight;

        private void Start()
        {
            _barHeight = (int)(progressRect.rect.height - progressRect.offsetMax.y);
            GameManager.OnProgressUpdate += OnProgressUpdate;
        }

        void OnProgressUpdate(int current, int stagesCount)
        {
            var progress = Mathf.Lerp(0, 700, current / (float)stagesCount);
            progressRect.offsetMax = new Vector2(0, progress - _barHeight);
            progressText.text = current + "";
        }
    }
}