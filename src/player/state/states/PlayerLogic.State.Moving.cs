namespace Woodblight;
using Chickensoft.LogicBlocks;

public partial class PlayerLogic {
  public partial record State {
    public partial record Alive {
      public partial record Moving : Alive, IGet<Input.Move> {
        public Moving() {
          OnAttach(() => { });
          OnDetach(() => { });

          this.OnEnter(() => Output(new Output.AnimationUpdated("run")));
          this.OnExit(() => { });
        }

        public Transition On(in Input.Move input) {
          var data = Get<Data>();
          var velocity = input.Direction * data.Speed;

          Output(new Output.ForceApplied(velocity, false));

          if (velocity.X < 0) {
            Output(new Output.FlipSprite(true));
          }
          else if (velocity.X > 0) {
            Output(new Output.FlipSprite(false));
          }

          return velocity.IsZeroApprox()
            ? To<Idle>()
            : ToSelf();
        }
      }
    }
  }
}
