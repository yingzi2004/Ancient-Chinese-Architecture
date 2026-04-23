using UnityEngine;
using UnityEngine.SceneManagement;

public class CubeClick : MonoBehaviour
{
    public string sceneName = "YourSceneName";

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 左键点击
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {
                    SceneManager.LoadScene(sceneName);
                }
            }
        }
    }
}
