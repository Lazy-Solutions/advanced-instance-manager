## GUIExt
Contains a few extra gui functions.

### Methods:

>##### EndColorScope()
>Ends the color scope, that was started with BeginColorScope(UnityEngine.Color).

>##### EndEnabledScope()
>Ends the enabled scope, that was started with !:BeginEnabledScope(bool).

>##### UnfocusOnClick()
>
>
>Unfocuses elements when blank area of EditorWindow clicked.
>
>Returns true if element was unfocused, you may want to Repaint() then.