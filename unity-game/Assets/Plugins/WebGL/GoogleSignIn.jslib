mergeInto(LibraryManager.library, {

    // Load the Google Identity Services SDK
    LoadGoogleSDK: function(clientIdPtr) {
        var clientId = UTF8ToString(clientIdPtr); // Convert the pointer to a JavaScript string

        // Log the client_id for debugging
        console.log("Initialized gapi with client_id:", clientId);

        var script = document.createElement('script');
        script.src = 'https://accounts.google.com/gsi/client';
        script.onload = function() {
            // Initialize the Google Identity Services client
            google.accounts.id.initialize({
                client_id: clientId,
                callback: function(response) {

                    // Decode the JWT to get the sub field
                    var parts = response.credential.split('.');
                    if (parts.length !== 3) {
                        throw new Error('JWT is invalid');
                    }
                    var payloadStr = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
                    var payload = JSON.parse(payloadStr);

                    var sub = payload.sub;
                    var email = payload.email;

                    // Package the data into a JSON object
                    var data = {
                        customID: sub,
                        email: email
                    };

                    // Convert the JSON object to a string
                    var jsonData = JSON.stringify(data);

                    // Notify Unity with the serialized JSON data
                    Module.SendMessage('GoogleAuthController', 'OnReceiveAuthData', jsonData);
                },
                // Other configurations and event listeners can be added here...
            });
            
            // Notify Unity when Google SDK is initialized
            Module.SendMessage('GoogleAuthController', 'GoogleSDKLoaded');
        };
        document.head.appendChild(script);
    },

    // Trigger the Google Sign-In process
    StartGoogleSignIn: function() {
        if (typeof google === 'undefined' || !google.accounts || !google.accounts.id) {
            console.error("Google API is not fully initialized yet.");
            return;
        }

        // Prompt the user to select an account.
        google.accounts.id.prompt();
    }
});