// UI tests must run serially: one Appium session drives a single device at a time.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]