using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Threading;
using NLog;

namespace SDL_HelpBotLibrary.Tools
{
    // http://moinmo.in/HelpOnConfiguration/SurgeProtection
    public static class SurgeProtection
    {
        private static readonly Logger _logger = LogManager.GetLogger(nameof(SurgeProtection));

        public static TimeSpan WaitTimeSpan { get; set; } = TimeSpan.FromSeconds(7);
        public static TimeSpan RequestTimeSpan { get; set; } = TimeSpan.FromSeconds(3);
        public static int RequestsPerRequestTimeSpan { get; set; }  = 4;
        public static int RequestCount { get; private set; }
        public static DateTime LastRequestDateTime { get; private set; }
        public static string WarningTextToDetect => "surge protection";
        public static bool Enabled { get; set; } = true;

        /// <summary>
        /// Call this method before every request made to a moinmoin wiki page so that it can determine if requests 
        /// were made too quickly and wait if necessary.
        /// </summary>
        /// <returns>True if too many requests were detected and this method enforces a wait on the thread.</returns>
        public static bool CheckBeforeRequest()
        {
            if (!Enabled) return false;

            bool waited = false;
            TimeSpan timeElapsedSinceLastRequest = DateTime.Now - LastRequestDateTime;

            if(RequestCount >= RequestsPerRequestTimeSpan && timeElapsedSinceLastRequest <= RequestTimeSpan)
            {
                WaitInternal($"Max requests, {RequestCount}, per time span, {RequestTimeSpan.TotalSeconds} seconds, reached at {timeElapsedSinceLastRequest.TotalSeconds} seconds, waiting {WaitTimeSpan.TotalSeconds} seconds");
                waited = true;
            }
            
            RequestCount++;
            LastRequestDateTime = DateTime.Now;
            return waited;
        }

        private static void WaitInternal(string logMessage)
        {
            _logger.Info(logMessage);
            Thread.Sleep(WaitTimeSpan);
            RequestCount = 0;
        }

        public static void Wait()
        {
            WaitInternal($"Explicit wait requested, waiting {WaitTimeSpan.TotalSeconds} seconds");
        }

        /// <summary>
        /// Call this method after a request is made so that it can determine if the moinmoin wiki has given a warning 
        /// about surge protection. If the warning is detected, a wait on the current thread will occur.
        /// </summary>
        /// <param name="pageSource">The web page source or text that could contain a warning message</param>
        /// <returns>True if a surge protection warning is detected and this method enforces a wait on the thread if enabled.</returns>
        public static bool CheckForWarning(in string pageSource)
        {
            bool warningDetected = string.Concat(pageSource.Take(1024)).ToLowerInvariant().Contains(WarningTextToDetect.ToLowerInvariant());

            if (Enabled && warningDetected)
            {
                WaitInternal($"The warning, '{WarningTextToDetect}', was detected in the page text, waiting {WaitTimeSpan.TotalSeconds} seconds");
                return warningDetected;
            }

            return warningDetected;
        }
    }
}
