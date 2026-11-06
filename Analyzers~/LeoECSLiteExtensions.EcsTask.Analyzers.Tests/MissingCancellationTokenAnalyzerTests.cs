using NUnit.Framework;
using static LeoECSLiteExtensions.Analyzers.Tests.AnalyzerRunner;

namespace LeoECSLiteExtensions.Analyzers.Tests
{
    public class MissingCancellationTokenAnalyzerTests
    {
        [Test]
        public void OptionalTokenNotPassed_IsReported()
        {
            AssertReports(@"
class System1
{
    async EcsTask RunAsync(CancellationToken ct)
    {
        await UniTask.Delay(1000);
    }
}", ("LEOECS103", "UniTask.Delay"));
        }

        [Test]
        public void OptionalTokenNotPassed_InGenericEcsTask_IsReported()
        {
            AssertReports(@"
class System1
{
    async EcsTask<int> FindTarget(CancellationToken ct)
    {
        await UniTask.Delay(1000);
        return 1;
    }
}", ("LEOECS103", "UniTask.Delay"));
        }

        [Test]
        public void OverloadWithTokenExists_IsReported()
        {
            AssertReports(@"
class System1
{
    async EcsTask RunAsync(CancellationToken ct)
    {
        await Task.Delay(1000);
    }
}", ("LEOECS103", "Task.Delay"));
        }

        [Test]
        public void TokenPassed_OrNoWayToPassIt_IsFine()
        {
            AssertReports(@"
class System1
{
    async EcsTask RunAsync(CancellationToken ct)
    {
        await UniTask.Delay(1000, ct);
        await Task.Delay(1000, ct);
        await Task.Yield();
    }
}");
        }

        [Test]
        public void NoTokenInScope_IsFine()
        {
            AssertReports(@"
class System1
{
    async EcsTask RunAsync(CancellationToken _)
    {
        await UniTask.Delay(1000);
    }
}");
        }

        [Test]
        public void TaskMethod_IsNotChecked()
        {
            AssertReports(@"
class System1
{
    async Task Helper(CancellationToken ct)
    {
        await UniTask.Delay(1000);
    }
}");
        }
    }
}
