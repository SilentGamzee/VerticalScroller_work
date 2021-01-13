using Managers;
using UnityEngine;

namespace Objects
{
    public class ObjectsHandler : MonoBehaviour
    {
        //Private
        [SerializeField] private int handlerIndex;
        private float speed;
        private RectTransform _rect;

        public void Init(int handlerIndex, float speed)
        {
            this.handlerIndex = handlerIndex;
            this.speed = speed;
            _rect = (RectTransform)transform;
        }

        private void Update()
        {
            _rect.anchoredPosition += Vector2.down * speed * Time.deltaTime;
            if (_rect.anchoredPosition.y <= 0)
            {
                GameManager.Instance.OnDespawn(handlerIndex);
                Destroy(gameObject);
            }
        }
    }
}
