using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LGU
{
    public class Example001Controller : MonoBehaviour
    {
        public Sprite[] sprites;
        public Image backgroundImage;
        private SmoothFollowMouse smoothFollowMouse;

        void Start()
        {
            Application.targetFrameRate = 120;
            smoothFollowMouse = GetComponent<SmoothFollowMouse>();
        }

        public void SetBackgroundImage(int index)
        {
            backgroundImage.sprite = sprites[index];
        }

        public void SetTarget()
        {
            if (Application.isMobilePlatform)
                return;

            GameObject clicked = EventSystem.current.currentSelectedGameObject;
            if (clicked == null) return;

            RectTransform rect = clicked.GetComponent<RectTransform>();
            if (rect == null) return;

            smoothFollowMouse.ChangeTarget(rect);
        }
    }
}