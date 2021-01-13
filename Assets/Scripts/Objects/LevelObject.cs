
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Objects
{
    public class LevelObject : MonoBehaviour
    {
        //Editor
        [SerializeField] private Image _img;

        public void Init(Sprite sprite)
        {
            _img.sprite = sprite;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Debug.Log("TriggerTAG: " + collision.gameObject.tag);
        }
    }
}
