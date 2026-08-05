using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private static List<CinemachineCamera> cameras = new List<CinemachineCamera>();

    public static CinemachineCamera ActiveCamera = null;

    public bool IsActiveCamera(CinemachineCamera camera)
    {
        return cameras.Contains(camera);
    }

    public void SwitchCamera(CinemachineCamera newCamera)
    {
        newCamera.Priority = 10;
        ActiveCamera = newCamera;

        foreach (CinemachineCamera camera in cameras)
        {
            if (camera != newCamera)
            {
                camera.Priority = 0;
            }
        }
    }

    public static void Register(CinemachineCamera camera)
    {
        cameras.Add(camera);
    }

    public static void Unregister(CinemachineCamera camera)
    {
        cameras.Remove(camera);
    }
}
