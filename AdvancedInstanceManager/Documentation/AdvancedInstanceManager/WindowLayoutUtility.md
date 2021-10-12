## WindowLayoutUtility
Provides methods for enumerating and applying window layouts.

### Properties:

>##### bool isAvailable { get; }
>Gets whatever the utility was able to find the internal unity methods or not.

>##### string layoutsPath { get;  set; }
>The path to the layouts folder.

>##### InstanceManager.Utility.WindowLayoutUtility.Layout[] availableLayouts { get; }
>Finds all available layouts.

### Methods:

>##### GetCurrent()
>Gets the current layout.