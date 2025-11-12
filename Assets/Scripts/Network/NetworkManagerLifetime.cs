using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class NetworkManagerLifetime : MonoBehaviour
{
    void Awake()
    {
        var nm = GetComponent<NetworkManager>();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton != nm)
        {
            Destroy(gameObject);            // kill duplicates if scene accidentally has another
            return;
        }
        DontDestroyOnLoad(gameObject);      // persist across Avatar/Game scenes
    }
}