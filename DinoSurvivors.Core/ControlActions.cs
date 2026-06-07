using System.Numerics;

namespace DinoSurvivors.Core;

public record ControlActions(
    Vector2 MoveDirection,
    Vector2 AimDirection,
    bool Fire = false,
    bool Pause = false,
    bool Confirm = false,
    bool Cancel = false
);
