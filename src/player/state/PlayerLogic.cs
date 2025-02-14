namespace Woodblight;

using System;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public interface IPlayerLogic : ILogicBlock<PlayerLogic.State>;

[Meta]
[LogicBlock(typeof(State), Diagram = true)]
public partial class PlayerLogic
  : LogicBlock<PlayerLogic.State>,
    IPlayerLogic {
  protected override void HandleError(Exception e) => throw e;
  public override Transition GetInitialState() => To<State.Alive.Idle>();

  public class Data {
    public float Speed { get; set; }
    public Vector2 AttackDirection { get; set; }
  }

  public static class Input {
    public record struct AnimationFinished(StringName Animation);
    public record struct UpdateHitting(bool IsHitting);
    public record struct UpdateGlobalPosition(Vector2 GlobalPosition);
    public record struct Move(Vector2 Direction);
    public record struct Attack(Vector2 Direction);
    public record struct Damage(int Amount, Vector2 Direction);
    public record struct Die;
    public record struct Revive;
  }

  public static class Output {
    public record struct ForceApplied(Vector2 Force, bool IsImpulse);
    public record struct StartAttacking(Vector2 Direction);
    public record struct FinishedAttacking(Vector2 Direction);
    public record struct SetHitting(bool IsHitting);
    public record struct Damaged(int Amount, Vector2 Direction);
    public record struct AnimationUpdated(StringName Animation);
    public record struct FlipSprite(bool Flip);
    public record struct Teleport(Vector2 GlobalPosition);
  }
}
