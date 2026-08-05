using LGU;
using UnityEngine;
using TMPro;

namespace LGU
{
    public class PrintFPS : MonoBehaviour
    {
        public TMP_Text mukText;

        private float deltaTime = 0.0f;
        private float refreshRate = 1.0f; // Refresh every second
        private float timer = 0.0f;

        void Update()
        {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            timer += Time.deltaTime;

            if (timer >= refreshRate)
            {
                timer = 0;

                float fps = 1.0f / deltaTime;
                mukText.text = "FPS: " + Mathf.Ceil(fps).ToString();

            }
        }
    }
}