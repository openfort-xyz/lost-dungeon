using UnityEngine;

public class MySingleton<T> : MonoBehaviour where T : Component
{
    // Static instance of the class.
    private static T _instance;

    // Flag to control destruction on load.
    public bool destroyOnLoad = false;

    // Public access to the instance.
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find the instance in the scene.
                _instance = FindObjectOfType<T>();
                if (_instance == null)
                {
                    // Create a new GameObject if no instance is found.
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name;
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            // Initialize the singleton instance.
            _instance = this as T;
            if (!destroyOnLoad)
            {
                // Make the instance persistent across scenes.
                DontDestroyOnLoad(this.gameObject);
            }
        }
        else
        {
            if (this != _instance)
            {
                // Destroy if another instance is already present.
                Destroy(this.gameObject);
            }
        }
    }
}