## UnityInstance
Represents a secondary unity instance.

### Properties:

>##### bool needsRepair { get; }
>Gets if this instance needs repairing.

>##### string displayName { get;  set; }
>The display name of this instance.

>##### string effectiveDisplayName { get; }
>Gets either displayName has value.

>##### string preferredLayout { get;  set; }
>Gets or sets the window layout.

>##### bool autoSync { get;  set; }
>Gets or sets whatever this instance should auto sync asset changes.

>##### bool openEditorInPrimaryEditor { get;  set; }
>Gets or sets whatever scripts should open in the editor that is associated with the primary instance.

>##### bool enterPlayModeAutomatically { get;  set; }
>Gets or sets whatever this instance should enter / exit play mode automatically when primary instance does.

>##### string[] scenes { get;  set; }
>Gets the scenes this instance should open when starting.

>##### bool isRunning { get; }
>Gets whatever this instance is running.

>##### string id { get; }
>Gets the id of this instance.

>##### string primaryID { get; }
>Gets the primary instance id that this instance is associated with.

>##### string path { get; }
>Gets the path of this instance.

>##### bool isSettingUp { get; }
>Gets if the instance is currently being set up.

>##### System.Diagnostics.Process InstanceProcess { get;  set; }
>Gets the process of this instance, if it is running.

### Methods:

>##### Save()
>Saves the instance settings to disk.

>##### Remove()
>Removes the instance from disk.

>##### Refresh()
>Refreshes this UnityInstance.

>##### ToggleOpen()
>Open if not running, othewise close.

>##### Open()
>Open instance.

>##### Close()
>Closes this instance.

>##### Close()
>Closes this instance.