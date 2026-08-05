using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LGU
{
    public class MenuController : MonoBehaviour
    {
        void Start()
        {
            Application.targetFrameRate = 120;
        }

        public void GoToScene(string scene)
        {
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
        }
    }
}