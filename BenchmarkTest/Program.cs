// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using BenchmarkTest;

Console.WriteLine("Hello, World!");
BenchmarkRunner.Run<DecompileBenchmark>();
