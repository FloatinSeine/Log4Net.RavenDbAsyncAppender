using System;
using log4net;
using log4net.Config;

namespace SampleLoggerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ILog log = CreateLogger();

            Console.Out.WriteLine("starting");

            for (var i = 0; i < 50; i++)
                log.Debug("a debug message " + i);


            for (var i = 0; i < 50; i++)
                log.Error("An error message " + i);

            log.Warn("A warning message");

            Pause();
        }

        private static void Pause()
        {
            Console.In.ReadLine();
        }

        private static ILog CreateLogger()
        {
            XmlConfigurator.Configure();
            return LogManager.GetLogger(typeof(Program));
        }
    }
}
