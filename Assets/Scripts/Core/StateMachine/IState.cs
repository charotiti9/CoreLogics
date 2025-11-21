public interface IState<TContext> where TContext : class
{
    void Enter(TContext context);

    void Update(TContext context, float deltaTime);

    void Exit(TContext context);
}
