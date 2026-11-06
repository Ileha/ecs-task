using NUnit.Framework;
using static LeoECSLiteExtensions.Analyzers.Tests.AnalyzerRunner;

namespace LeoECSLiteExtensions.Analyzers.Tests
{
    public class UnawaitedEcsTaskAnalyzerTests
    {
        [Test]
        public void EcsTaskCallAsStatement_IsReported()
        {
            AssertReports(@"
class System1
{
    async EcsTask RunAsync(CancellationToken ct)
    {
        Child(ct);
        await Child(ct);
    }

    async EcsTask Child(CancellationToken ct) => await UniTask.Delay(100, ct);
}", ("LEOECS105", "Child(ct)"));
        }

        [Test]
        public void GenericEcsTaskCallAsStatement_IsReported()
        {
            AssertReports(@"
class System1
{
    async EcsTask RunAsync(CancellationToken ct)
    {
        FindTarget(ct);
        var target = await FindTarget(ct);
    }

    async EcsTask<int> FindTarget(CancellationToken ct)
    {
        await UniTask.Delay(100, ct);
        return 1;
    }
}", ("LEOECS105", "FindTarget(ct)"));
        }

        [Test]
        public void EcsTaskAssignedOrDiscarded_IsFine()
        {
            AssertReports(@"
class System1
{
    EcsTask _cycle;

    void Run(CancellationToken ct)
    {
        _cycle = Child(ct);
        _ = Child(ct);
    }

    async EcsTask Child(CancellationToken ct) => await UniTask.Delay(100, ct);
}");
        }
    }
}
