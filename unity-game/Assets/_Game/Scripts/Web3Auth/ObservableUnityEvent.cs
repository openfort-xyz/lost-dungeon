using System;
using UnityEngine.Events;

namespace Assets.Scripts.AuthService
{
    /// <summary>
    /// The main event for <see cref="Observable{t}"/>.
    /// </summary>
    public class ObservableUnityEvent<T> : UnityEvent<T> where T : struct, IConvertible
    {
    }
}
