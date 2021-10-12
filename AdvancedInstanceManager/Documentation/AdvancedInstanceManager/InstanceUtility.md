## InstanceUtility
Provides utility functions for working with secondary instances.

### Events:

>##### onInstancesChanged
>Occurs when an instance is changed.

### Fields:

>##### string instanceFileName
>The name of the instance settings file.

### Methods:

>##### LocalInstance()
>Loads local instance file. Returns null if none exists or instance is primary.

>##### Enumerate()
>Enumerates all secondary instances for this project.

>##### Create()
>Create a new secondary instance. Returns null if current instance is secondary.