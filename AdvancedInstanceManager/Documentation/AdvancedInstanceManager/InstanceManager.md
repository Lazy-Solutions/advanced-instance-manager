## InstanceManager
The main class of Instance Manager.

### Events:

>##### OnSecondInstanceStarted
>Occurs during startup if current instance is secondary.

>##### OnPrimaryPause
>Occurs when primary instance is paused.

>##### OnPrimaryUnpause
>Occurs when primary instance is unpaused.

>##### OnPrimaryEnterPlayMode
>Occurs when primary instance enters play mode.

>##### OnPrimaryExitPlayMode
>Occurs when primary instance exiting play mode.

>##### OnPrimaryAssetsChanged
>Occurs when primary instance has had its assets changed.

### Properties:

>##### System.Collections.Generic.IEnumerable<InstanceManager.Models.UnityInstance> instances { get; }
>The secondary instances that have been to this project.

>##### InstanceManager.Models.UnityInstance instance { get; }
>The current instance. null if primary.

>##### bool isPrimaryInstance { get; }
>Gets if the current instance is the primary instance.

>##### bool isSecondaryInstance { get; }
>Gets if the current instance is a secondary instance.

>##### string id { get; }
>Gets the id of the current instance.

### Methods:

>##### SyncWithPrimaryInstance()
>Sync this instance with the primary instance, does nothing if current instance is primary.