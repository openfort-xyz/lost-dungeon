
namespace Assets.Scripts.AuthService
{
    /// <summary>
    /// List of possible platforms of the <see cref="AuthenticationKit"/>.
    /// </summary>
    public enum AuthenticationKitPlatform
    {
        None,
        Android,
        iOS,
        WalletConnect,
        WebGL
    }

    /// <summary>
    /// List of possible states of the <see cref="AuthenticationKit"/>.
    /// </summary>
    public enum AuthenticationKitState
    {
        None,
        PreInitialized,
        Initializing,
        Initialized,
        WalletConnecting,
        WalletConnected,
        WalletSigning,
        WalletSigned,
        AuthServiceLoggingIn,
        AuthServiceLoggedIn,
        Disconnecting,
        Disconnected
    }
}
