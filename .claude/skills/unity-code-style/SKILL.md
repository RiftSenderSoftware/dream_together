---
name: unity-code-style
description: Code style rules for C# Unity scripts in the dream_space project. Use whenever writing or editing any .cs script in this project (MonoBehaviour, controllers, player/camera/visual components).
---

# Unity code style (dream_space)

## Do not hide errors with null checks

Do not write defensive `if (x == null) return;`, `if (x != null) ...`, `x?.Method()` or
`?? fallback` for references that must exist (serialized fields, references passed to `Init`,
components found via `GetComponent*`, camera, animator, bones, etc.).

A missing reference is a setup error. Let Unity throw `NullReferenceException` /
`MissingReferenceException` so the error is visible in the console immediately, rather than
the component silently doing nothing.

```csharp
// Bad: hides a broken setup
private void LateUpdate()
{
    if (_view == null || _body == null)
        return;
    ...
}

if (_head != null)
    _head.LookTarget = _lookTarget;

// Good: assume the reference exists; if it doesn't, Unity shows an error
private void LateUpdate()
{
    ...
}

_head.LookTarget = _lookTarget;
```

Also do not add "fallback" values or code paths for a missing reference
(e.g. `_fallbackHeadLimit` when `_head` is not assigned). Make the reference required.

### Allowed exceptions

- Data that is genuinely optional by design, e.g. humanoid bones that an avatar may not have
  (`HumanBodyBones.UpperChest`), or `Keyboard.current` / `Gamepad.current` when the device
  is not connected.
- Auto-filling serialized fields in `OnValidate` / `Init` (`if (_x == null) _x = GetComponent<X>();`)
  is fine, but do not guard later usage after that.

## Tooltips only where the meaning is not obvious

Do not put `[Tooltip]` on every field. Add one only to key fields whose meaning is not clear
from the name alone (e.g. `_turnThreshold` — "how far past the head + spine limit before the
whole body turns"). Fields like `_turnSpeed`, `_animator`, `_lookDistance` need no tooltip.

## English only

All comments, `[Tooltip]` and `[Header]` texts, and log messages are written in English,
even when the conversation with the user is in Russian.
