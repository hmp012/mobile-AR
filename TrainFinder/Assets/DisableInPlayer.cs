using UnityEngine;

public class DisableInPlayer : MonoBehaviour
{
    void Awake()
    {
#if !UNITY_EDITOR
        gameObject.SetActive(false);
#endif
    }
}