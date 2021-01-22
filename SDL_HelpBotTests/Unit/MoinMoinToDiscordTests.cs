using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SDL_HelpBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Humanizer;
using System.Reflection.Metadata.Ecma335;
using SDL_HelpBotLibrary.Parsers;

namespace SDL_HelpBotTests.Unit
{
    [TestFixture]
    public class MoinMoinToDiscordTests
    {

        [SetUp]
        public void SetUp()
        {

        }

        [TestCase("https://wiki.libsdl.org/")]
        [TestCase(null)]
        public void InternalLinkGeneration(string baseUrl)
        {
            string text =
                " .[[SDL_Log]]" + Environment.NewLine +
                " .[[SDL_LogDebug]]" + Environment.NewLine +
                " .[[SDL_LogError]]" + Environment.NewLine +
                " .[[SDL_LogInfo]]" + Environment.NewLine +
                " .[[SDL_LogMessage]]" + Environment.NewLine +
                " .[[SDL_LogMessageV]]" + Environment.NewLine +
                " .[[SDL_LogVerbose]]" + Environment.NewLine +
                " .[[SDL_LogWarn]]" + Environment.NewLine;

            string expectedWithBaseUrlResult =
                $" [SDL_Log]({baseUrl}SDL_Log)" + Environment.NewLine +
                $" [SDL_LogDebug]({baseUrl}SDL_LogDebug)" + Environment.NewLine +
                $" [SDL_LogError]({baseUrl}SDL_LogError)" + Environment.NewLine +
                $" [SDL_LogInfo]({baseUrl}SDL_LogInfo)" + Environment.NewLine +
                $" [SDL_LogMessage]({baseUrl}SDL_LogMessage)" + Environment.NewLine +
                $" [SDL_LogMessageV]({baseUrl}SDL_LogMessageV)" + Environment.NewLine +
                $" [SDL_LogVerbose]({baseUrl}SDL_LogVerbose)" + Environment.NewLine +
                $" [SDL_LogWarn]({baseUrl}SDL_LogWarn)" + Environment.NewLine;

            string expectedWithoutBaseUrlResult =
                $" SDL_Log" + Environment.NewLine +
                $" SDL_LogDebug" + Environment.NewLine +
                $" SDL_LogError" + Environment.NewLine +
                $" SDL_LogInfo" + Environment.NewLine +
                $" SDL_LogMessage" + Environment.NewLine +
                $" SDL_LogMessageV" + Environment.NewLine +
                $" SDL_LogVerbose" + Environment.NewLine +
                $" SDL_LogWarn" + Environment.NewLine;

            Assert.AreEqual
            (
                baseUrl == null ? expectedWithoutBaseUrlResult : expectedWithBaseUrlResult,
                text.GenerateDiscordLinks(baseUrl)
            );
        }

        [Test]
        public void ExternalLinkGeneration()
        {
            string text = "[[http://hg.libsdl.org/SDL/file/default/include/SDL_log.h|SDL_log.h]]";
            string expectedResult = $"[SDL_log.h](http://hg.libsdl.org/SDL/file/default/include/SDL_log.h)";

            Assert.AreEqual(expectedResult, text.GenerateDiscordLinks());
        }

        [Test]
        public void CodeBlockGeneration()
        {
            string text =
                "== Syntax ==" + Environment.NewLine +
                "{{{#!highlight cpp" + Environment.NewLine +
                "void SDL_LogCritical(int         category," + Environment.NewLine +
                "                     const char* fmt," + Environment.NewLine +
                "                     ...)" + Environment.NewLine +
                "}}}" + Environment.NewLine;

            string expectedResult =
                "== Syntax ==" + Environment.NewLine +
                "```cpp" + Environment.NewLine +
                "void SDL_LogCritical(int         category," + Environment.NewLine +
                "                     const char* fmt," + Environment.NewLine +
                "                     ...)" + Environment.NewLine +
                "```" + Environment.NewLine;

            Assert.AreEqual(expectedResult, text.GenerateDiscordCodeBlocks());
        }

        [Test]
        public void DoubleCodeBlockGeneration()
        {
            //string text =
            //    "== Syntax ==" + Environment.NewLine +
            //    "{{{#!highlight cpp" + Environment.NewLine +
            //    "void SDL_LogCritical(int         category," + Environment.NewLine +
            //    "                     const char* fmt," + Environment.NewLine +
            //    "                     ...)" + Environment.NewLine +
            //    "}}}" + Environment.NewLine + Environment.NewLine +

            //    "== Return Value ==" + Environment.NewLine +
            //    "Returns the window that was created or NULL on failure; call SDL_GetError() for more information." + Environment.NewLine + Environment.NewLine +

            //    "== Code Examples ==" + Environment.NewLine +
            //    "{{{#!highlight cpp" + Environment.NewLine +
            //    "void SDL_LogCritical(int         category," + Environment.NewLine +
            //    "                     const char* fmt," + Environment.NewLine +
            //    "                     ...)" + Environment.NewLine +
            //    "}}}" + Environment.NewLine;

            //string expectedResult =
            //    "== Syntax ==" + Environment.NewLine +
            //    "```cpp" + Environment.NewLine +
            //    "void SDL_LogCritical(int         category," + Environment.NewLine +
            //    "                     const char* fmt," + Environment.NewLine +
            //    "                     ...)" + Environment.NewLine +
            //    "```" + Environment.NewLine + Environment.NewLine +

            //    "== Return Value ==" + Environment.NewLine +
            //    "Returns the window that was created or NULL on failure; call SDL_GetError() for more information." + Environment.NewLine + Environment.NewLine +

            //    "== Code Examples ==" + Environment.NewLine +
            //    "```cpp" + Environment.NewLine +
            //    "void SDL_LogCritical(int         category," + Environment.NewLine +
            //    "                     const char* fmt," + Environment.NewLine +
            //    "                     ...)" + Environment.NewLine +
            //    "```" + Environment.NewLine;

            string text = "#pragma section-numbers off\r\n#pragma disable-camelcase\r\n\r\n= SDL_CreateWindow =\r\nUse this function to create a window with the specified position, dimensions, and flags.\r\n\r\n<<TableOfContents()>>\r\n\r\n== Syntax ==\r\n{{{#!highlight cpp\r\nSDL_Window* SDL_CreateWindow(const char* title,\r\n                             int         x,\r\n                             int         y,\r\n                             int         w,\r\n                             int         h,\r\n                             Uint32      flags)\r\n}}}\r\n\r\n== Function Parameters ==\r\n||'''title'''||the title of the window, in UTF-8 encoding||\r\n||'''x'''||the x position of the window, SDL_WINDOWPOS_CENTERED, or SDL_WINDOWPOS_UNDEFINED||\r\n||'''y'''||the y position of the window, SDL_WINDOWPOS_CENTERED, or SDL_WINDOWPOS_UNDEFINED||\r\n||'''w'''||the width of the window, in screen coordinates||\r\n||'''h'''||the height of the window, in screen coordinates||\r\n||'''flags'''||0, or one or more [[SDL_WindowFlags]] OR'd together; see [[#flags|Remarks]] for details||\r\n\r\n== Return Value ==\r\nReturns the window that was created or NULL on failure; call [[SDL_GetError]]() for more information.\r\n\r\n== Code Examples ==\r\n{{{#!highlight cpp\r\n// Example program:\r\n// Using SDL2 to create an application window\r\n\r\n#include \"SDL.h\"\r\n#include <stdio.h>\r\n\r\nint main(int argc, char* argv[]) {\r\n\r\n    SDL_Window *window;                    // Declare a pointer\r\n\r\n    SDL_Init(SDL_INIT_VIDEO);              // Initialize SDL2\r\n\r\n    // Create an application window with the following settings:\r\n    window = SDL_CreateWindow(\r\n        \"An SDL2 window\",                  // window title\r\n        SDL_WINDOWPOS_UNDEFINED,           // initial x position\r\n        SDL_WINDOWPOS_UNDEFINED,           // initial y position\r\n        640,                               // width, in pixels\r\n        480,                               // height, in pixels\r\n        SDL_WINDOW_OPENGL                  // flags - see below\r\n    );\r\n\r\n    // Check that the window was successfully created\r\n    if (window == NULL) {\r\n        // In the case that the window could not be made...\r\n        printf(\"Could not create window: %s\n\", SDL_GetError());\r\n        return 1;\r\n    }\r\n\r\n    // The window is open: could enter program loop here (see SDL_PollEvent())\r\n\r\n    SDL_Delay(3000);  // Pause execution for 3000 milliseconds, for example\r\n\r\n    // Close and destroy the window\r\n    SDL_DestroyWindow(window);\r\n\r\n    // Clean up\r\n    SDL_Quit();\r\n    return 0;\r\n}\r\n\r\n}}}\r\n\r\n== Remarks ==\r\n<<Anchor(flags)>> '''flags''' may be any of the following OR'd together:\r\n||SDL_WINDOW_FULLSCREEN||fullscreen window||\r\n||SDL_WINDOW_FULLSCREEN_DESKTOP||fullscreen window at the current desktop resolution||\r\n||SDL_WINDOW_OPENGL||window usable with OpenGL context||\r\n||SDL_WINDOW_VULKAN||window usable with a Vulkan instance||\r\n||SDL_WINDOW_HIDDEN||window is not visible||\r\n||SDL_WINDOW_BORDERLESS||no window decoration||\r\n||SDL_WINDOW_RESIZABLE||window can be resized||\r\n||SDL_WINDOW_MINIMIZED||window is minimized||\r\n||SDL_WINDOW_MAXIMIZED||window is maximized||\r\n||SDL_WINDOW_INPUT_GRABBED||window has grabbed input focus||\r\n||SDL_WINDOW_ALLOW_HIGHDPI||window should be created in high-DPI mode if supported (>= SDL 2.0.1)||\r\n\r\nSDL_WINDOW_SHOWN is ignored by SDL_!CreateWindow(). The SDL_Window is implicitly shown if SDL_WINDOW_HIDDEN is not set. SDL_WINDOW_SHOWN may be queried later using [[SDL_GetWindowFlags]]().\r\n\r\nOn Apple's OS X you '''must''' set the NSHighResolutionCapable Info.plist property to YES, otherwise you will not receive a High DPI OpenGL canvas.\r\n\r\nIf the window is created with the SDL_WINDOW_ALLOW_HIGHDPI flag, its size in pixels may differ from its size in screen coordinates on platforms with high-DPI support (e.g. iOS and Mac OS X). Use [[SDL_GetWindowSize]]() to query the client area's size in screen coordinates, and [[SDL_GL_GetDrawableSize]]() or [[SDL_GetRendererOutputSize]]() to query the drawable size in pixels.\r\n\r\nIf the window is set fullscreen, the width and height parameters '''w''' and '''h''' will not be used. However, invalid size parameters (e.g. too large) may still fail. Window size is actually limited to 16384 x 16384 for all platforms at window creation.\r\n\r\n== Version ==\r\nThis function is available since SDL 2.0.0.\r\n\r\n== Related Functions ==\r\n .[[SDL_CreateWindowFrom]]\r\n .[[SDL_DestroyWindow]]\r\n\r\n----\r\n[[CategoryAPI]], [[CategoryVideo]]";
            string expectedResult = "#pragma section-numbers off\r\n#pragma disable-camelcase\r\n\r\n= SDL_CreateWindow =\r\nUse this function to create a window with the specified position, dimensions, and flags.\r\n\r\n<<TableOfContents()>>\r\n\r\n== Syntax ==\r\n```cpp\r\nSDL_Window* SDL_CreateWindow(const char* title,\r\n                             int         x,\r\n                             int         y,\r\n                             int         w,\r\n                             int         h,\r\n                             Uint32      flags)\r\n```\r\n\r\n== Function Parameters ==\r\n||'''title'''||the title of the window, in UTF-8 encoding||\r\n||'''x'''||the x position of the window, SDL_WINDOWPOS_CENTERED, or SDL_WINDOWPOS_UNDEFINED||\r\n||'''y'''||the y position of the window, SDL_WINDOWPOS_CENTERED, or SDL_WINDOWPOS_UNDEFINED||\r\n||'''w'''||the width of the window, in screen coordinates||\r\n||'''h'''||the height of the window, in screen coordinates||\r\n||'''flags'''||0, or one or more [[SDL_WindowFlags]] OR'd together; see [[#flags|Remarks]] for details||\r\n\r\n== Return Value ==\r\nReturns the window that was created or NULL on failure; call [[SDL_GetError]]() for more information.\r\n\r\n== Code Examples ==\r\n```cpp\r\n// Example program:\r\n// Using SDL2 to create an application window\r\n\r\n#include \"SDL.h\"\r\n#include <stdio.h>\r\n\r\nint main(int argc, char* argv[]) {\r\n\r\n    SDL_Window *window;                    // Declare a pointer\r\n\r\n    SDL_Init(SDL_INIT_VIDEO);              // Initialize SDL2\r\n\r\n    // Create an application window with the following settings:\r\n    window = SDL_CreateWindow(\r\n        \"An SDL2 window\",                  // window title\r\n        SDL_WINDOWPOS_UNDEFINED,           // initial x position\r\n        SDL_WINDOWPOS_UNDEFINED,           // initial y position\r\n        640,                               // width, in pixels\r\n        480,                               // height, in pixels\r\n        SDL_WINDOW_OPENGL                  // flags - see below\r\n    );\r\n\r\n    // Check that the window was successfully created\r\n    if (window == NULL) {\r\n        // In the case that the window could not be made...\r\n        printf(\"Could not create window: %s\n\", SDL_GetError());\r\n        return 1;\r\n    }\r\n\r\n    // The window is open: could enter program loop here (see SDL_PollEvent())\r\n\r\n    SDL_Delay(3000);  // Pause execution for 3000 milliseconds, for example\r\n\r\n    // Close and destroy the window\r\n    SDL_DestroyWindow(window);\r\n\r\n    // Clean up\r\n    SDL_Quit();\r\n    return 0;\r\n}\r\n\r\n```\r\n\r\n== Remarks ==\r\n<<Anchor(flags)>> '''flags''' may be any of the following OR'd together:\r\n||SDL_WINDOW_FULLSCREEN||fullscreen window||\r\n||SDL_WINDOW_FULLSCREEN_DESKTOP||fullscreen window at the current desktop resolution||\r\n||SDL_WINDOW_OPENGL||window usable with OpenGL context||\r\n||SDL_WINDOW_VULKAN||window usable with a Vulkan instance||\r\n||SDL_WINDOW_HIDDEN||window is not visible||\r\n||SDL_WINDOW_BORDERLESS||no window decoration||\r\n||SDL_WINDOW_RESIZABLE||window can be resized||\r\n||SDL_WINDOW_MINIMIZED||window is minimized||\r\n||SDL_WINDOW_MAXIMIZED||window is maximized||\r\n||SDL_WINDOW_INPUT_GRABBED||window has grabbed input focus||\r\n||SDL_WINDOW_ALLOW_HIGHDPI||window should be created in high-DPI mode if supported (>= SDL 2.0.1)||\r\n\r\nSDL_WINDOW_SHOWN is ignored by SDL_!CreateWindow(). The SDL_Window is implicitly shown if SDL_WINDOW_HIDDEN is not set. SDL_WINDOW_SHOWN may be queried later using [[SDL_GetWindowFlags]]().\r\n\r\nOn Apple's OS X you '''must''' set the NSHighResolutionCapable Info.plist property to YES, otherwise you will not receive a High DPI OpenGL canvas.\r\n\r\nIf the window is created with the SDL_WINDOW_ALLOW_HIGHDPI flag, its size in pixels may differ from its size in screen coordinates on platforms with high-DPI support (e.g. iOS and Mac OS X). Use [[SDL_GetWindowSize]]() to query the client area's size in screen coordinates, and [[SDL_GL_GetDrawableSize]]() or [[SDL_GetRendererOutputSize]]() to query the drawable size in pixels.\r\n\r\nIf the window is set fullscreen, the width and height parameters '''w''' and '''h''' will not be used. However, invalid size parameters (e.g. too large) may still fail. Window size is actually limited to 16384 x 16384 for all platforms at window creation.\r\n\r\n== Version ==\r\nThis function is available since SDL 2.0.0.\r\n\r\n== Related Functions ==\r\n .[[SDL_CreateWindowFrom]]\r\n .[[SDL_DestroyWindow]]\r\n\r\n----\r\n[[CategoryAPI]], [[CategoryVideo]]";

            Assert.AreEqual(expectedResult, text.GenerateDiscordCodeBlocks());
        }

        [Test]
        public void TableGeneration()
        {
            string text =
                "== Function Parameters ==" + Environment.NewLine +
                "||'''category'''||the category of the message; see [[#category|Remarks]] for details||" + Environment.NewLine +
                "||'''fmt'''||a printf() style message format string||" + Environment.NewLine +
                "||'''...'''||additional parameters matching % tokens in the '''fmt''' string, if any||" + Environment.NewLine;

            string expectedResult =
                "== Function Parameters ==" + Environment.NewLine +
                "**`category`** - the category of the message; see [[#category|Remarks]] for details" + Environment.NewLine +
                "**`fmt`** - a printf() style message format string" + Environment.NewLine +
                "**`...`** - additional parameters matching % tokens in the '''fmt''' string, if any" + Environment.NewLine;

            var result = text.GenerateDiscordTables();

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void RemoveMacros()
        {
            string codeSection =
                "== Syntax ==" + Environment.NewLine +
                "{{{#!highlight cpp" + Environment.NewLine +
                "void SDL_LogCritical(int         category," + Environment.NewLine +
                "                     const char* fmt," + Environment.NewLine +
                "                     <<test>>...)" + Environment.NewLine +
                "}}}" + Environment.NewLine;

            string remarksSection =
                "== Remarks ==" + Environment.NewLine +
                "<<Anchor(category)>>" + Environment.NewLine +
                "The '''category''' can be one of:" + Environment.NewLine +
                "<<Include(SDL_LOG_CATEGORY, , , from=\" == Values == \", to=\" == Code Examples == \")>>" + Environment.NewLine;

            string text = codeSection + Environment.NewLine + remarksSection;

            string expectedResult =
                codeSection + Environment.NewLine +
                "== Remarks ==" + Environment.NewLine + Environment.NewLine +
                "The '''category''' can be one of:" + Environment.NewLine + Environment.NewLine;

            var result = text.RemoveMoinMoinMacros();

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void SplitMoinMoinSections()
        {
            string[] sections =
            {
                Environment.NewLine +
                "= Title Area =" + Environment.NewLine +
                "This is the description of the page." + Environment.NewLine + Environment.NewLine,

                "== Function Parameters ==" + Environment.NewLine +
                "||'''category'''||the category of the message; see [[#category|Remarks]] for details||" + Environment.NewLine +
                "||'''fmt'''||a printf() style message format string||" + Environment.NewLine +
                "||'''...'''||additional parameters matching % tokens in the '''fmt''' string, if any||" + Environment.NewLine + Environment.NewLine,

                "----" + Environment.NewLine +
                "[[CategoryAPI]], [[CategoryLog]]"
            };

            string text = string.Join(Environment.NewLine, sections);

            var expectedResult = new Dictionary<string, string>()
            {
                { "Title Area", "This is the description of the page." },
                { "Function Parameters",
                    "||'''category'''||the category of the message; see [[#category|Remarks]] for details||" + Environment.NewLine +
                    "||'''fmt'''||a printf() style message format string||" + Environment.NewLine +
                    "||'''...'''||additional parameters matching % tokens in the '''fmt''' string, if any||" },
                { "Categories", "[[CategoryAPI]], [[CategoryLog]]" }
            };

            var result = text.SplitMoinMoinSections(includeCategories: true);

            Assert.That(result.SequenceEqual(expectedResult));
        }
    }
}
