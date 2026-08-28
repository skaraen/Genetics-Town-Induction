using Fusion;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class NetworkManagerLifetime : MonoBehaviour
{
    private void Awake()
    {
        NetworkRunner runner = GetComponent<NetworkRunner>();
        if (runner == null)
            return;

        NetworkRunner existingRunner = FindFirstObjectByType<NetworkRunner>();
        if (existingRunner != null && existingRunner != runner)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
}
