//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Columns;

namespace WPFHexaEditor.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance))
                .AddExporter(HtmlExporter.Default)
                .AddExporter(MarkdownExporter.GitHub)
                .AddColumn(StatisticColumn.Mean)
                .AddColumn(StatisticColumn.StdDev)
                .AddColumn(StatisticColumn.Median)
                .AddColumn(BaselineRatioColumn.RatioMean)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}
