namespace Woodblight;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Woodblight.Traits;

public interface IPlayer : IRigidBody2D, IStateDebugInfo, IDamageable, IProvide<IPlayerRepo> {
}

[Meta(typeof(IAutoNode))]
public partial class Player : RigidBody2D, IPlayer {

  #region Exports
  #endregion

  #region Nodes
  [Node] private ISprite2D PlayerModel { get; set; } = default!;
  [Node] private IAnimationPlayer AnimationPlayer { get; set; } = default!;
  [Node] private IAttacker Attacker { get; set; } = default!;
  [Node] private ColorRect Whiteout { get; set; } = default!;
  [Node] private AudioStreamPlayer2D HurtAudio { get; set; } = default!;

  #endregion

  #region Provisions
  public IPlayerRepo Value() => PlayerRepo;
  #endregion

  #region Dependencies
  [Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();
  #endregion

  #region State
  private IPlayerRepo PlayerRepo { get; set; } = new PlayerRepo(3);
  private PlayerLogic Logic { get; set; } = default!;
  private PlayerLogic.IBinding Binding { get; set; } = default!;
  #endregion

  #region IPlayer
  string IStateDebugInfo.Name => Name;
  public string State => Logic.Value.GetType().Name;
  #endregion


  #region Dependency Lifecycle
  public void Setup() => Logic = new();

  public void OnResolved() {
    Binding = Logic.Bind();

    // Bind functions to state outputs here
    Binding.Handle(
      (in PlayerLogic.Output.ForceApplied output) => OnOutputForceApplied(output.Force, output.IsImpulse)
    ).Handle(
      (in PlayerLogic.Output.AnimationUpdated output) => OnOutputAnimationUpdated(output.Animation)
    ).Handle(
      (in PlayerLogic.Output.FlipSprite output) => OnOutputFlipSprite(output.Flip)
    ).Handle(
      (in PlayerLogic.Output.StartAttacking output) => OnOutputStartAttacking(output.Direction)
    ).Handle(
      (in PlayerLogic.Output.SetHitting output) => OnOutputSetHitting(output.IsHitting)
    ).Handle(
      (in PlayerLogic.Output.Teleport output) => OnOutputTeleport(output.GlobalPosition)
    ).Handle(
      (in PlayerLogic.Output.Damaged output) => OnOutputDamaged(output.Amount, output.Direction));

    Logic.Set(GameRepo);
    Logic.Set(PlayerRepo);
    Logic.Set(new PlayerLogic.Data() {
      Speed = 1000f
    });

    this.Provide();
    AddToGroup(StateDebug.GROUP);
    Logic.Start();
  }
  #endregion



  #region Godot Lifecycle
  public override void _Notification(int what) => this.Notify(what);

  public void OnReady() {
    SetProcess(true);
    SetPhysicsProcess(true);

    AnimationPlayer.AnimationFinished += OnAnimationFinished;
  }

  public void OnProcess(double delta) { }

  public void OnPhysicsProcess(double delta) {
    var inputDirection = Input.GetVector(Inputs.Left, Inputs.Right, Inputs.Up, Inputs.Down);

    Move(inputDirection);
    Logic.Input(new PlayerLogic.Input.UpdateGlobalPosition(GlobalPosition));

    if (!inputDirection.IsZeroApprox()) {
      _lastInputDirection = inputDirection;
    }
  }

  public void OnExitTree() {
    Logic.Stop();
    Binding.Dispose();
  }

  // FIXME hack helper var for aiming attack?
  private Vector2 _lastInputDirection;
  public override void _UnhandledInput(InputEvent @event) {
    if (@event.IsActionPressed(Inputs.Attack)) {
      Logic.Input(new PlayerLogic.Input.Attack(_lastInputDirection));
    }
  }
  #endregion

  #region Input Callbacks
  public void Damage(int amount, Vector2 direction) =>
    Logic.Input(new PlayerLogic.Input.Damage(amount, direction));
  private void StartHitting() => Logic.Input(new PlayerLogic.Input.UpdateHitting(true));
  private void StopHitting() => Logic.Input(new PlayerLogic.Input.UpdateHitting(false));
  private void OnAnimationFinished(StringName animName) => Logic.Input(new PlayerLogic.Input.AnimationFinished(animName));
  private void Move(Vector2 direction) => Logic.Input(new PlayerLogic.Input.Move(direction));
  #endregion

  #region Output Callbacks
  private void OnOutputForceApplied(Vector2 velocity, bool isImpulse = false) {
    if (isImpulse) {
      ApplyImpulse(velocity);
    }
    else {
      ApplyForce(velocity);
    }
  }
  private void OnOutputAnimationUpdated(StringName animation) => AnimationPlayer.Play(animation);
  private void OnOutputFlipSprite(bool flip) => PlayerModel.FlipH = flip;
  private void OnOutputStartAttacking(Vector2 direction) => Attacker.Attack(direction);
  private void OnOutputSetHitting(bool isHitting) => Attacker.SetActive(isHitting);
  private void OnOutputTeleport(Vector2 globalPosition) => GlobalPosition = globalPosition;

  // FIXME remove tween?
  private Tween? _damagedTween;
  private void OnOutputDamaged(int amount, Vector2 direction) {
    ApplyImpulse(direction * 400);
    if (_damagedTween is not null && _damagedTween.IsRunning()) {
      _damagedTween.Kill();
    }
    _damagedTween = CreateTween();
    Whiteout.Modulate = Colors.White;
    _damagedTween.TweenProperty(Whiteout, "modulate:a", 0.0, 0.2);
    HurtAudio.Play();
  }
  #endregion
}
