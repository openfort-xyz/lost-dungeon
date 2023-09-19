using UnityEngine;

public class StateMachineSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static object lockObject = new object();

    public static T Instance
    {
        get
        {
            // Check if the instance is already set.
            if (instance == null)
            {
                // Lock the thread to ensure only one instance is created.
                lock (lockObject)
                {
                    // Attempt to find an existing instance in the scene.
                    instance = FindObjectOfType<T>();

                    // If no instance is found, create a new one.
                    if (instance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(T).Name);
                        instance = singletonObject.AddComponent<T>();
                    }
                }
            }

            return instance;
        }
    }

    // Optional Awake method to initialize any state or setup the singleton.
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}