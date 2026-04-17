using UnityEngine;

public class ChangeShader : MonoBehaviour
{
    private bool isFirstColor = false;
    Material mat;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (isFirstColor)
        {
            mat.EnableKeyword("isFirstColor");
        }
        else
        {
            mat.DisableKeyword("isFirstColor");
        }
        isFirstColor = !isFirstColor;
    }
}
