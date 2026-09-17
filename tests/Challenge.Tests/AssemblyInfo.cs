using NUnit.Framework;

[assembly: LevelOfParallelism(2)]
[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
