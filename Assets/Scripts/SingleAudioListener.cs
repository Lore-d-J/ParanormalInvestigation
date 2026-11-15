using UnityEngine;

internal static class SingleAudioListener
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void DisableDuplicateListeners()
    {
        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
        AudioListener primary = null;

        foreach (var listener in listeners)
        {
            if (listener.enabled && listener.gameObject.name == "PlayerCamera")
            {
                primary = listener;
                break;
            }
        }

        if (primary == null)
        {
            foreach (var listener in listeners)
            {
                var camera = listener.GetComponent<Camera>();
                if (listener.enabled && camera != null && camera.CompareTag("MainCamera"))
                {
                    primary = listener;
                    break;
                }
            }
        }

        if (primary == null)
        {
            foreach (var listener in listeners)
            {
                if (listener.enabled)
                {
                    primary = listener;
                    break;
                }
            }
        }

        if (primary == null)
            return;

        var disabledCount = 0;
        foreach (var listener in listeners)
        {
            if (listener.enabled && listener != primary)
            {
                listener.enabled = false;
                disabledCount++;
            }
        }

        if (disabledCount > 0)
            Debug.Log($"Disabled {disabledCount} duplicate AudioListener(s); keeping '{primary.gameObject.name}'.");
    }
}
